//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Generic;
using System.Text;

namespace NosCore.EndToEnd.Tests.Infrastructure
{
    // The client's half of the wire format: what a game client does to a line before it sends
    // it, and to the bytes it receives. The servers hold the other half.
    public static class ClientCipher
    {
        private const int WorldChunk = 0x7A;
        private const byte LoginEnd = 0xD8;
        private const byte LoginReplyEnd = 0x19;
        private const byte WorldEnd = 0xFF;
        private const byte SessionStart = 0x96;
        private const byte SessionEnd = 0x0E;

        public static readonly Encoding Text = Encoding.Latin1;

        public static byte[] EncodeLogin(string line)
        {
            var text = Text.GetBytes(line);
            var bytes = new byte[text.Length + 1];
            for (var i = 0; i < text.Length; i++)
            {
                bytes[i] = (byte)((text[i] ^ 0xC3) + 0xF);
            }

            bytes[^1] = LoginEnd;
            return bytes;
        }

        public static bool IsCompleteLoginReply(ReadOnlySpan<byte> bytes) => bytes.Length > 0 && bytes[^1] == LoginReplyEnd;

        public static string DecodeLoginReply(ReadOnlySpan<byte> bytes)
        {
            var text = new byte[bytes.Length - 1];
            for (var i = 0; i < text.Length; i++)
            {
                text[i] = (byte)(bytes[i] - 0xF);
            }

            return Text.GetString(text);
        }

        // The first thing a client sends to a world server: its packet counter and the session
        // the login server gave it, two characters to a byte.
        public static byte[] EncodeSession(int counter, int session)
        {
            var text = $"{counter} {session}";
            if (text.Length % 2 != 0)
            {
                text += " ";
            }

            var bytes = new List<byte> { SessionStart };
            for (var i = 0; i < text.Length; i += 2)
            {
                bytes.Add((byte)(((Nibble(text[i]) << 4) | Nibble(text[i + 1])) + 0xF));
            }

            bytes.Add(SessionEnd);
            return bytes.ToArray();
        }

        public static byte[] EncodeWorld(string line, int session)
        {
            var text = Text.GetBytes(line);
            var framed = new List<byte>(text.Length + text.Length / WorldChunk + 2);
            for (var start = 0; start < text.Length; start += WorldChunk)
            {
                var length = Math.Min(WorldChunk, text.Length - start);
                framed.Add((byte)length);
                for (var i = 0; i < length; i++)
                {
                    framed.Add((byte)(text[start + i] ^ 0xFF));
                }
            }

            framed.Add(WorldEnd);

            var key = (byte)((session & 0xFF) + 0x40);
            var mode = (session >> 6) & 3;
            var bytes = new byte[framed.Count];
            for (var i = 0; i < bytes.Length; i++)
            {
                var value = framed[i];
                bytes[i] = mode switch
                {
                    0 => (byte)(value + key),
                    1 => (byte)(value - key),
                    2 => (byte)((value ^ 0xC3) + key),
                    _ => (byte)((value ^ 0xC3) - key)
                };
            }

            return bytes;
        }

        // Takes the complete packets off the front of what has been received and leaves the rest.
        public static List<string> DecodeWorld(List<byte> received)
        {
            var packets = new List<string>();
            int end;
            while ((end = received.IndexOf(WorldEnd)) >= 0)
            {
                var text = new List<byte>(end);
                for (var position = 0; position < end;)
                {
                    var length = received[position];
                    for (var i = 1; i <= length && position + i < end; i++)
                    {
                        text.Add((byte)(received[position + i] ^ 0xFF));
                    }

                    position += 1 + length;
                }

                packets.Add(Text.GetString(text.ToArray()));
                received.RemoveRange(0, end + 1);
            }

            return packets;
        }

        private static int Nibble(char character) => character switch
        {
            ' ' => 1,
            '-' => 2,
            '.' => 3,
            >= '0' and <= '9' => character - '0' + 4,
            _ => throw new ArgumentOutOfRangeException(nameof(character), character, "Not a session character")
        };
    }
}
