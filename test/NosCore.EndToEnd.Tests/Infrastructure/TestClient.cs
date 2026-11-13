//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace NosCore.EndToEnd.Tests.Infrastructure
{
    // A game client without the game: it logs in, opens the world session and exchanges packet
    // lines with the servers over real sockets, the way the client does.
    public sealed class TestClient : IAsyncDisposable
    {
        public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(10);

        private readonly string _account;
        private readonly string _password;
        private readonly string _host;
        private readonly int _loginPort;
        private readonly List<string> _received = new();
        private readonly SemaphoreSlim _arrived = new(0);
        private readonly CancellationTokenSource _closing = new();
        private TcpClient? _world;
        private Task? _reading;
        private int _session;
        private int _counter = Random.Shared.Next(1000, 30000);
        private int _cursor;
        private bool _disconnected;

        public TestClient(string host, int loginPort, string account, string password = "test")
        {
            _host = host;
            _loginPort = loginPort;
            _account = account;
            _password = password;
        }

        public long CharacterId { get; private set; }

        public int Speed { get; private set; }

        // Every packet received so far, oldest first.
        public IReadOnlyList<string> Received
        {
            get
            {
                lock (_received)
                {
                    return _received.ToList();
                }
            }
        }

        // The login server's answer to this account: the world to connect to, or the refusal.
        public async Task<LoginReply> LoginAsync()
        {
            var line = string.Join(' ', "NoS0575", Random.Shared.Next(100000, 9999999).ToString(CultureInfo.InvariantCulture),
                _account, Convert.ToHexString(SHA512.HashData(ClientCipher.Text.GetBytes(_password))),
                Guid.NewGuid().ToString(), "006548C6", "0\v0.9.3.3125", "0", "458E0876581FA9A6EFE00A28AA8E75F2");
            using var login = new TcpClient();
            await login.ConnectAsync(_host, _loginPort);
            var stream = login.GetStream();
            await stream.WriteAsync(ClientCipher.EncodeLogin(line));

            var bytes = new List<byte>();
            var buffer = new byte[8192];
            using var timeout = new CancellationTokenSource(DefaultTimeout);
            while (!ClientCipher.IsCompleteLoginReply(bytes.ToArray()))
            {
                var read = await stream.ReadAsync(buffer, timeout.Token);
                if (read == 0)
                {
                    break;
                }

                bytes.AddRange(buffer.AsSpan(0, read).ToArray());
            }

            return LoginReply.Parse(bytes.Count == 0 ? string.Empty : ClientCipher.DecodeLoginReply(bytes.ToArray()));
        }

        // Logs in and opens the world session; the character list is in Received afterwards.
        public async Task ConnectAsync()
        {
            var reply = await LoginAsync();
            if (reply.World == null)
            {
                throw new InvalidOperationException($"Login refused: {reply.Raw}");
            }

            _session = reply.Session;
            _world = new TcpClient { NoDelay = true };
            await _world.ConnectAsync(reply.World.Value.Host, reply.World.Value.Port);
            await _world.GetStream().WriteAsync(ClientCipher.EncodeSession(_counter++, _session));
            _reading = Task.Run(ReadLoopAsync);
            await Task.Delay(200);
            await SendAsync($"{_account} ORG 0");
            await SendAsync(_password);
            await ExpectAsync("clist_end");
        }

        // Selects the character in a slot and starts the game; returns what the server sent for it.
        public async Task<IReadOnlyList<string>> EnterWorldAsync(byte slot)
        {
            if (_world == null)
            {
                await ConnectAsync();
            }

            var mark = Mark();
            await SendAsync($"select {slot}");
            await ExpectAsync("OK");
            await SendAsync("game_start");
            await SendAsync("lbs 0");
            var info = await ExpectAsync("c_info");
            CharacterId = long.Parse(info.Split(' ')[6], CultureInfo.InvariantCulture);
            await ExpectAsync("c_map");
            var condition = await ExpectAsync("cond");
            Speed = int.Parse(condition.Split(' ')[5], CultureInfo.InvariantCulture);
            return Since(mark);
        }

        // One step, as the client sends it: the cell, its checksum and the speed the server gave.
        public Task WalkAsync(short x, short y) => SendAsync($"walk {x} {y} {(x + y) % 3 % 2} {Speed}");

        public async Task SendAsync(string line)
        {
            var world = _world ?? throw new InvalidOperationException("Not connected to a world server");
            var counter = _counter;
            _counter = _counter >= 65534 ? 0 : _counter + 1;
            await world.GetStream().WriteAsync(ClientCipher.EncodeWorld($"{counter} {line}", _session));
        }

        // A position in what has been received, to read from later with Since.
        public int Mark()
        {
            lock (_received)
            {
                return _received.Count;
            }
        }

        public IReadOnlyList<string> Since(int mark)
        {
            lock (_received)
            {
                return _received.Skip(mark).ToList();
            }
        }

        // The next packet with this header after the last one an Expect returned. Fails the test
        // when none arrives in time.
        public Task<string> ExpectAsync(string header, TimeSpan? timeout = null) =>
            ExpectAsync(packet => HeaderOf(packet) == header, header, timeout);

        public async Task<string> ExpectAsync(Func<string, bool> matches, string description, TimeSpan? timeout = null)
        {
            var packet = await WaitAsync(matches, timeout ?? DefaultTimeout);
            if (packet != null)
            {
                return packet;
            }

            var tail = string.Join(Environment.NewLine, Received.TakeLast(15).Select(p => "  " + Shorten(p)));
            throw new TimeoutException(
                $"No '{description}' packet within {(timeout ?? DefaultTimeout).TotalSeconds:0.#} s" +
                $"{(_disconnected ? " (the server closed the connection)" : string.Empty)}. Last packets:{Environment.NewLine}{tail}");
        }

        // The next matching packet, or null when none arrives in time. Use it to assert that
        // something is not sent.
        public async Task<string?> WaitAsync(Func<string, bool> matches, TimeSpan timeout)
        {
            var waited = Stopwatch.StartNew();
            while (true)
            {
                lock (_received)
                {
                    for (; _cursor < _received.Count; _cursor++)
                    {
                        if (matches(_received[_cursor]))
                        {
                            return _received[_cursor++];
                        }
                    }
                }

                var left = timeout - waited.Elapsed;
                if (left <= TimeSpan.Zero || !await _arrived.WaitAsync(left))
                {
                    return null;
                }
            }
        }

        public static string HeaderOf(string packet)
        {
            var space = packet.IndexOf(' ');
            return space < 0 ? packet : packet[..space];
        }

        private static string Shorten(string packet) => packet.Length <= 120 ? packet : packet[..120] + "...";

        private async Task ReadLoopAsync()
        {
            var world = _world ?? throw new InvalidOperationException("Not connected to a world server");
            var pending = new List<byte>();
            var buffer = new byte[16384];
            try
            {
                while (!_closing.IsCancellationRequested)
                {
                    var read = await world.GetStream().ReadAsync(buffer, _closing.Token);
                    if (read == 0)
                    {
                        break;
                    }

                    pending.AddRange(buffer.AsSpan(0, read).ToArray());
                    var packets = ClientCipher.DecodeWorld(pending);
                    if (packets.Count == 0)
                    {
                        continue;
                    }

                    lock (_received)
                    {
                        _received.AddRange(packets);
                    }

                    _arrived.Release();
                }
            }
            catch (Exception ex) when (ex is OperationCanceledException or System.IO.IOException or ObjectDisposedException)
            {
            }

            _disconnected = true;
            _arrived.Release();
        }

        public async ValueTask DisposeAsync()
        {
            _closing.Cancel();
            _world?.Dispose();
            if (_reading != null)
            {
                await _reading;
            }

            _closing.Dispose();
            _arrived.Dispose();
        }
    }

    public sealed record LoginReply(string Raw, int Session, (string Host, int Port)? World)
    {
        // NsTeST <region> <account> ... <session> <ip:port:colour:world.channel.name> ... -1:-1:-1:10000.10000.1
        public static LoginReply Parse(string raw)
        {
            var parts = raw.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0 || parts[0] != "NsTeST")
            {
                return new LoginReply(raw, 0, null);
            }

            var index = Array.FindIndex(parts, part => part.Count(c => c == ':') == 3 && !part.StartsWith("-1", StringComparison.Ordinal));
            if (index < 1)
            {
                return new LoginReply(raw, 0, null);
            }

            var server = parts[index].Split(':');
            return new LoginReply(raw, int.Parse(parts[index - 1], CultureInfo.InvariantCulture),
                (server[0], int.Parse(server[1], CultureInfo.InvariantCulture)));
        }

        public string? Refusal => World == null && Raw.StartsWith("failc", StringComparison.Ordinal) ? Raw : null;
    }
}
