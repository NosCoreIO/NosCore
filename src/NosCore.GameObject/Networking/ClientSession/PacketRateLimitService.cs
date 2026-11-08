//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Options;
using NodaTime;
using NosCore.Core.Configuration;
using NosCore.GameObject.Infastructure;
using NosCore.Packets.ClientPackets.Battle;
using NosCore.Packets.ClientPackets.Chat;
using NosCore.Packets.ClientPackets.Families;
using NosCore.Packets.ClientPackets.Mates;
using NosCore.Packets.ClientPackets.Movement;
using NosCore.Packets.ClientPackets.UI;
using NosCore.Packets.Enumerations;
using NosCore.Packets.Interfaces;

namespace NosCore.GameObject.Networking.ClientSession;

public sealed class PacketRateLimitService(IOptions<WorldConfiguration> configuration, IClock clock)
    : IPacketRateLimitService, ISingletonService
{
    private const short FirstEmoticon = 973;
    private const short LastEmoticon = 999;

    private static readonly Duration Chat = Duration.FromMilliseconds(500);
    private static readonly Duration Shout = Duration.FromSeconds(3);
    private static readonly Duration Gesture = Duration.FromMilliseconds(500);
    private static readonly Duration Action = Duration.FromMilliseconds(100);
    private static readonly Duration Step = Duration.FromMilliseconds(50);

    // Minimum time between two packets of the same kind, for packets that reach other players.
    private static readonly Dictionary<Type, (string Key, Duration Interval)> HeaderLimits = new()
    {
        [typeof(ClientSayPacket)] = ("say", Chat),
        [typeof(WhisperPacket)] = ("whisper", Chat),
        [typeof(BtkPacket)] = ("btk", Chat),
        [typeof(GroupTalkPacket)] = ("group", Chat),
        [typeof(FamilyChatPacket)] = ("family", Chat),
        [typeof(HeroPacket)] = ("hero", Shout),
        [typeof(ClientDirPacket)] = ("dir", Action),
        [typeof(UseSkillPacket)] = ("u_s", Action),
        [typeof(UpetPacket)] = ("u_pet", Action),
        [typeof(WalkPacket)] = ("walk", Step),
    };

    public PacketRateDecision Check(ClientSession session, IPacket packet)
    {
        var now = clock.GetCurrentInstant();
        var bucket = CheckBucket(session.PacketRate, now);
        if (bucket != PacketRateDecision.Allow)
        {
            return bucket;
        }

        return IsWithinHeaderLimit(session, packet, now) ? PacketRateDecision.Allow : PacketRateDecision.Drop;
    }

    private PacketRateDecision CheckBucket(PacketRateState state, Instant now)
    {
        var limits = configuration.Value.PacketRateLimit;
        lock (state.Lock)
        {
            if (!state.Started)
            {
                state.Started = true;
                state.Tokens = limits.Burst;
                state.LastRefill = now;
                state.DropWindowStart = now;
            }

            var elapsed = (now - state.LastRefill).TotalSeconds;
            state.Tokens = Math.Min(limits.Burst, state.Tokens + elapsed * limits.PacketsPerSecond);
            state.LastRefill = now;

            if (state.Tokens >= 1)
            {
                state.Tokens -= 1;
                return PacketRateDecision.Allow;
            }

            if (now - state.DropWindowStart > Duration.FromSeconds(limits.DropWindowSeconds))
            {
                state.DropWindowStart = now;
                state.DroppedInWindow = 0;
            }

            state.DroppedInWindow++;
            return state.DroppedInWindow >= limits.DisconnectAfterDroppedPackets
                ? PacketRateDecision.Disconnect
                : PacketRateDecision.Drop;
        }
    }

    private static bool IsWithinHeaderLimit(ClientSession session, IPacket packet, Instant now)
    {
        if (!session.HasPlayerEntity || !TryGetLimit(packet, out var key, out var interval))
        {
            return true;
        }

        var lastSent = session.Character.LastSent;
        if (lastSent == null)
        {
            return true;
        }

        if (lastSent.TryGetValue(key, out var last) && now - last < interval)
        {
            return false;
        }

        lastSent[key] = now;
        return true;
    }

    private static bool TryGetLimit(IPacket packet, out string key, out Duration interval)
    {
        if (packet is GuriPacket { Type: GuriPacketType.TextInput, Data: >= FirstEmoticon and <= LastEmoticon })
        {
            (key, interval) = ("emoticon", Gesture);
            return true;
        }

        if (HeaderLimits.TryGetValue(packet.GetType(), out var limit))
        {
            (key, interval) = limit;
            return true;
        }

        (key, interval) = (string.Empty, Duration.Zero);
        return false;
    }
}
