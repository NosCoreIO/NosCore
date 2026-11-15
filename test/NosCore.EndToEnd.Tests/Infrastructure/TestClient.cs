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
using NosCore.Packets.ClientPackets;
using NosCore.Packets.ClientPackets.CharacterSelectionScreen;
using NosCore.Packets.ClientPackets.Login;
using NosCore.Packets.ClientPackets.Movement;
using NosCore.Packets.ClientPackets.Player;
using NosCore.Packets.Interfaces;
using NosCore.Packets.ServerPackets.CharacterSelectionScreen;
using NosCore.Packets.ServerPackets.Login;
using NosCore.Packets.ServerPackets.MiniMap;
using NosCore.Packets.ServerPackets.Player;
using NosCore.Shared.Enumerations;

namespace NosCore.EndToEnd.Tests.Infrastructure
{
    // A game client without the game: it logs in, opens the world session and exchanges packet
    // lines with the servers over real sockets, the way the client does.
    public sealed class TestClient : IAsyncDisposable
    {
        public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(10);

        // What the servers have to say about an expectation that fails, added to its message.
        // The argument is whether the server closed this client's connection.
        public static Func<bool, string>? ServerDiagnosis { get; set; }

        private readonly string _account;
        private readonly string _password;
        private readonly string _host;
        private readonly int _loginPort;
        private readonly List<Arrival> _received = new();
        private readonly SemaphoreSlim _arrived = new(0);
        private readonly CancellationTokenSource _closing = new();
        private TcpClient? _world;
        private Task? _reading;
        private int _session;
        private int _counter = Random.Shared.Next(1000, 30000);
        private int _cursor;
        private bool _disconnected;

        private readonly int _channel;

        public TestClient(string host, int loginPort, string account, string password = "test", int channel = 0)
        {
            _host = host;
            _loginPort = loginPort;
            _account = account;
            _password = password;
            _channel = channel;
        }

        public long CharacterId { get; private set; }

        public int Speed { get; private set; }

        // Every packet received so far, oldest first.
        public IReadOnlyList<IPacket> Received => Since(0);

        // The login server's answer to this account: the world to connect to, or the refusal.
        public async Task<LoginReply> LoginAsync()
        {
            var line = Wire.Serialize(new NoS0575Packet
            {
                SessionId = Random.Shared.Next(100000, 9999999),
                Username = _account,
                Password = Convert.ToHexString(SHA512.HashData(ClientCipher.Text.GetBytes(_password))),
                ClientId = Guid.NewGuid(),
                UnknownYet = "006548C6",
                RegionType = RegionType.EN,
                ClientVersion = new ClientVersionSubPacket { Major = 0, Minor = 9, Fix = 3, SubFix = 3125 },
                Md5String = "458E0876581FA9A6EFE00A28AA8E75F2",
            });
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

            return new LoginReply(bytes.Count == 0 ? null : Wire.Deserialize(ClientCipher.DecodeLoginReply(bytes.ToArray())));
        }

        // Logs in and opens the world session; the character list is in Received afterwards.
        public async Task ConnectAsync()
        {
            var reply = await LoginAsync();
            if (reply.Channels.Count <= _channel)
            {
                throw new InvalidOperationException($"No channel {_channel} in the login reply: {reply}");
            }

            _session = reply.Accepted?.SessionId ?? 0;
            _world = new TcpClient { NoDelay = true };
            await _world.ConnectAsync(reply.Channels[_channel].Host, reply.Channels[_channel].Port);
            await _world.GetStream().WriteAsync(ClientCipher.EncodeSession(_counter++, _session));
            _reading = Task.Run(ReadLoopAsync);
            await Task.Delay(200);
            // The session opens with the account and the password as bare lines, before any packet.
            await SendLineAsync($"{_account} ORG 0");
            await SendLineAsync(_password);
            await ExpectAsync<ClistEndPacket>();
        }

        // Selects the character in a slot and starts the game; returns what the server sent for it.
        public async Task<IReadOnlyList<IPacket>> EnterWorldAsync(byte slot)
        {
            if (_world == null)
            {
                await ConnectAsync();
            }

            var mark = Mark();
            await SendAsync(new SelectPacket { Slot = slot });
            await ExpectAsync<OkPacket>();
            await SendAsync(new GameStartPacket());
            await SendAsync(new LbsPacket { Type = 0 });
            CharacterId = (await ExpectAsync<CInfoPacket>()).CharacterId;
            await ExpectAsync<CMapPacket>();
            Speed = (await ExpectAsync<CondPacket>()).Speed;
            // The map's packets come first and game_start carries on after them; title is the last
            // packet it always sends, so nothing of the login is left to arrive once it is here.
            await ExpectAsync<TitlePacket>();
            return Since(mark);
        }

        // One step, as the client sends it: the cell, its checksum and the speed the server gave.
        public Task WalkAsync(short x, short y) => SendAsync(new WalkPacket
        {
            XCoordinate = x,
            YCoordinate = y,
            CheckSum = (byte)((x + y) % 3 % 2),
            Speed = (short)Speed,
        });

        public Task SendAsync(IPacket packet) => SendLineAsync(Wire.Serialize(packet));

        private async Task SendLineAsync(string line)
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

        public IReadOnlyList<IPacket> Since(int mark)
        {
            lock (_received)
            {
                return _received.Skip(mark).Select(arrival => arrival.Packet).ToList();
            }
        }

        public IEnumerable<T> Since<T>(int mark) where T : class, IPacket => Since(mark).OfType<T>();

        // The next packet of this type, matching when a condition is given, after the last one an
        // Expect returned. Fails the test when none arrives in time.
        public async Task<T> ExpectAsync<T>(Func<T, bool>? matches = null, string? description = null, TimeSpan? timeout = null)
            where T : class, IPacket =>
            (T)(await ExpectAsync(Is(matches), description ?? typeof(T).Name, timeout)).Packet;

        // A packet received at or after a mark, wherever it falls among the others. For when two
        // packets answer one action and their order is not the point.
        public async Task<T> ExpectSinceAsync<T>(int mark, Func<T, bool>? matches = null, string? description = null, TimeSpan? timeout = null)
            where T : class, IPacket =>
            (T)(await ExpectSinceAsync(mark, Is(matches), description ?? typeof(T).Name, timeout)).Packet;

        // Whether nothing matching has arrived since a mark, once the given time has passed. An
        // Expect moves past the packets before the one it returns, so an absence is asserted
        // from a mark taken before the action instead.
        public Task<bool> NoneSinceAsync<T>(int mark, Func<T, bool>? matches, TimeSpan after) where T : class, IPacket =>
            NoneSinceAsync(mark, Is(matches), after);

        // The next matching packet, or null when none arrives in time. Use it to assert that
        // something is not sent.
        public async Task<T?> WaitAsync<T>(Func<T, bool>? matches, TimeSpan timeout) where T : class, IPacket =>
            (await WaitAsync(Is(matches), timeout))?.Packet as T;

        private static Func<Arrival, bool> Is<T>(Func<T, bool>? matches) where T : class, IPacket =>
            arrival => arrival.Packet is T packet && (matches == null || matches(packet));

        private async Task<Arrival> ExpectAsync(Func<Arrival, bool> matches, string description, TimeSpan? timeout)
        {
            var arrival = await WaitAsync(matches, timeout ?? DefaultTimeout);
            return arrival ?? throw NotReceived(description, timeout ?? DefaultTimeout, "Last packets", Arrivals(0).TakeLast(15));
        }

        private async Task<Arrival> ExpectSinceAsync(int mark, Func<Arrival, bool> matches, string description, TimeSpan? timeout)
        {
            var limit = timeout ?? DefaultTimeout;
            var waited = Stopwatch.StartNew();
            while (true)
            {
                var found = Arrivals(mark).FirstOrDefault(matches);
                if (found != null)
                {
                    return found;
                }

                if (limit - waited.Elapsed <= TimeSpan.Zero)
                {
                    throw NotReceived(description, limit, "Received since", Arrivals(mark).TakeLast(15));
                }

                await Task.Delay(50);
            }
        }

        private async Task<bool> NoneSinceAsync(int mark, Func<Arrival, bool> matches, TimeSpan after)
        {
            await Task.Delay(after);
            return !Arrivals(mark).Any(matches);
        }

        private async Task<Arrival?> WaitAsync(Func<Arrival, bool> matches, TimeSpan timeout)
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

        private List<Arrival> Arrivals(int mark)
        {
            lock (_received)
            {
                return _received.Skip(mark).ToList();
            }
        }

        private TimeoutException NotReceived(string description, TimeSpan limit, string listed, IEnumerable<Arrival> arrivals) =>
            new($"No '{description}' packet within {limit.TotalSeconds:0.#} s" +
                $"{(_disconnected ? " (the server closed the connection)" : string.Empty)}. {listed}:{Environment.NewLine}" +
                string.Join(Environment.NewLine, arrivals.Select(arrival => "  " + arrival)) +
                ServerDiagnosis?.Invoke(_disconnected));

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

                    var arrivals = packets.Select(Arrival.Of).ToList();
                    lock (_received)
                    {
                        _received.AddRange(arrivals);
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

    // A packet as it came off the wire; the line is kept to describe the packet when an expectation fails.
    internal sealed record Arrival(IPacket Packet, string Line)
    {
        public static Arrival Of(string line) => new(Wire.Deserialize(line), line);

        public override string ToString()
        {
            var shown = Packet is UndecodedPacket undecoded ? $"{undecoded.Reason}: {Line}" : Line;
            return shown.Length <= 120 ? shown : shown[..120] + "...";
        }
    }

    public sealed record LoginReply(IPacket? Packet)
    {
        public NsTestPacket? Accepted => Packet as NsTestPacket;

        public FailcPacket? Refusal => Packet as FailcPacket;

        public IReadOnlyList<(string Host, int Port)> Channels =>
            Accepted?.SubPacket?.OfType<NsTeStSubPacket>()
                .Where(server => server is { Host: not null and not "-1", Port: > 0 })
                .Select(server => (server.Host ?? string.Empty, server.Port ?? 0)).ToList() ?? [];

        public (string Host, int Port)? World => Channels.Count > 0 ? Channels[0] : null;

        public override string ToString() => Packet == null ? "no reply" : Wire.Serialize(Packet);
    }
}
