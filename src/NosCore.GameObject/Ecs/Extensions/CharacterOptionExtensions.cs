//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Dto;
using NosCore.Packets.Enumerations;
using System;
using System.Linq;

namespace NosCore.GameObject.Ecs.Extensions;

public static class CharacterOptionExtensions
{
    public static bool GetOption(this PlayerComponentBundle player, GopPacketType option) => option switch
    {
        GopPacketType.ExchangeBlocked => player.ExchangeBlocked,
        GopPacketType.FriendRequestBlocked => player.FriendRequestBlocked,
        GopPacketType.FamilyRequestBlocked => player.FamilyRequestBlocked,
        GopPacketType.WhisperBlocked => player.WhisperBlocked,
        GopPacketType.GroupRequestBlocked => player.GroupRequestBlocked,
        GopPacketType.PetAutoRelive => player.PetAutoRelive,
        GopPacketType.PartnerAutoRelive => player.PartnerAutoRelive,
        GopPacketType.GroupSharing => player.GroupSharing,
        GopPacketType.MouseAimLock => player.MouseAimLock,
        GopPacketType.HeroChatBlocked => player.HeroChatBlocked,
        GopPacketType.QuickGetUp => player.QuickGetUp,
        GopPacketType.EmoticonsBlocked => player.EmoticonsBlocked,
        GopPacketType.HpBlocked => player.HpBlocked,
        GopPacketType.BuffBlocked => player.BuffBlocked,
        GopPacketType.MinilandInviteBlocked => player.MinilandInviteBlocked,
        GopPacketType.HatInVisible => player.HatInvisible,
        GopPacketType.LockInterface => player.LockInterface,
        _ => throw new ArgumentOutOfRangeException(nameof(option), option, null)
    };

    public static void SetOption(this PlayerComponentBundle player, GopPacketType option, bool value)
    {
        switch (option)
        {
            case GopPacketType.ExchangeBlocked: player.ExchangeBlocked = value; break;
            case GopPacketType.FriendRequestBlocked: player.FriendRequestBlocked = value; break;
            case GopPacketType.FamilyRequestBlocked: player.FamilyRequestBlocked = value; break;
            case GopPacketType.WhisperBlocked: player.WhisperBlocked = value; break;
            case GopPacketType.GroupRequestBlocked: player.GroupRequestBlocked = value; break;
            case GopPacketType.PetAutoRelive: player.PetAutoRelive = value; break;
            case GopPacketType.PartnerAutoRelive: player.PartnerAutoRelive = value; break;
            case GopPacketType.GroupSharing: player.GroupSharing = value; break;
            case GopPacketType.MouseAimLock: player.MouseAimLock = value; break;
            case GopPacketType.HeroChatBlocked: player.HeroChatBlocked = value; break;
            case GopPacketType.QuickGetUp: player.QuickGetUp = value; break;
            case GopPacketType.EmoticonsBlocked: player.EmoticonsBlocked = value; break;
            case GopPacketType.HpBlocked: player.HpBlocked = value; break;
            case GopPacketType.BuffBlocked: player.BuffBlocked = value; break;
            case GopPacketType.MinilandInviteBlocked: player.MinilandInviteBlocked = value; break;
            case GopPacketType.HatInVisible: player.HatInvisible = value; break;
            case GopPacketType.LockInterface: player.LockInterface = value; break;
            default: throw new ArgumentOutOfRangeException(nameof(option), option, null);
        }
    }

    // One bit per option at (option - 1). The mouse aim bit is set while the lock is off.
    public static double GenerateStatOption(this PlayerComponentBundle player) =>
        Enum.GetValues<GopPacketType>()
            .Where(option => player.GetOption(option) != (option == GopPacketType.MouseAimLock))
            .Sum(option => 1 << ((int)option - 1));

    public static void LoadOptions(this PlayerComponentBundle player, CharacterDto characterDto)
    {
        player.ExchangeBlocked = characterDto.ExchangeBlocked;
        player.FriendRequestBlocked = characterDto.FriendRequestBlocked;
        player.FamilyRequestBlocked = characterDto.FamilyRequestBlocked;
        player.WhisperBlocked = characterDto.WhisperBlocked;
        player.GroupRequestBlocked = characterDto.GroupRequestBlocked;
        player.PetAutoRelive = characterDto.PetAutoRelive;
        player.PartnerAutoRelive = characterDto.PartnerAutoRelive;
        player.GroupSharing = characterDto.GroupSharing;
        player.MouseAimLock = characterDto.MouseAimLock;
        player.HeroChatBlocked = characterDto.HeroChatBlocked;
        player.QuickGetUp = characterDto.QuickGetUp;
        player.EmoticonsBlocked = characterDto.EmoticonsBlocked;
        player.HpBlocked = characterDto.HpBlocked;
        player.BuffBlocked = characterDto.BuffBlocked;
        player.MinilandInviteBlocked = characterDto.MinilandInviteBlocked;
        player.HatInvisible = characterDto.HatInvisible;
        player.LockInterface = characterDto.LockInterface;
    }

    public static void SaveOptions(this PlayerComponentBundle player, CharacterDto characterDto)
    {
        characterDto.ExchangeBlocked = player.ExchangeBlocked;
        characterDto.FriendRequestBlocked = player.FriendRequestBlocked;
        characterDto.FamilyRequestBlocked = player.FamilyRequestBlocked;
        characterDto.WhisperBlocked = player.WhisperBlocked;
        characterDto.GroupRequestBlocked = player.GroupRequestBlocked;
        characterDto.PetAutoRelive = player.PetAutoRelive;
        characterDto.PartnerAutoRelive = player.PartnerAutoRelive;
        characterDto.GroupSharing = player.GroupSharing;
        characterDto.MouseAimLock = player.MouseAimLock;
        characterDto.HeroChatBlocked = player.HeroChatBlocked;
        characterDto.QuickGetUp = player.QuickGetUp;
        characterDto.EmoticonsBlocked = player.EmoticonsBlocked;
        characterDto.HpBlocked = player.HpBlocked;
        characterDto.BuffBlocked = player.BuffBlocked;
        characterDto.MinilandInviteBlocked = player.MinilandInviteBlocked;
        characterDto.HatInvisible = player.HatInvisible;
        characterDto.LockInterface = player.LockInterface;
    }
}
