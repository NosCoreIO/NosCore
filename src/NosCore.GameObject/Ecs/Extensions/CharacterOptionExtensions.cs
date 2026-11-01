//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Dto;
using NosCore.Packets.Enumerations;

namespace NosCore.GameObject.Ecs.Extensions;

public static class CharacterOptionExtensions
{
    // The social options arrive as "allow" (true = accept requests) while the rest arrive as
    // "block"; both are stored as the blocking state.
    public static bool TryApplyOption(this PlayerComponentBundle player, GopPacketType option, bool isActive,
        out Game18NConstString? message)
    {
        message = null;
        switch (option)
        {
            case GopPacketType.ExchangeBlocked:
                player.ExchangeBlocked = !isActive;
                message = player.ExchangeBlocked ? Game18NConstString.TradeBlocked : Game18NConstString.TradeAllowed;
                return true;
            case GopPacketType.FriendRequestBlocked:
                player.FriendRequestBlocked = !isActive;
                message = player.FriendRequestBlocked ? Game18NConstString.FriendBlocked : Game18NConstString.FriendAllowed;
                return true;
            case GopPacketType.WhisperBlocked:
                player.WhisperBlocked = !isActive;
                message = player.WhisperBlocked ? Game18NConstString.WhisperBlocked : Game18NConstString.WhisperAllowed;
                return true;
            case GopPacketType.GroupRequestBlocked:
                player.GroupRequestBlocked = !isActive;
                message = player.GroupRequestBlocked ? Game18NConstString.GroupRequestBlocked : Game18NConstString.GroupRequestAllowed;
                return true;
            case GopPacketType.HeroChatBlocked:
                player.HeroChatBlocked = isActive;
                message = player.HeroChatBlocked ? Game18NConstString.SpeakerDisabled : Game18NConstString.SpeakerEnabled;
                return true;
            case GopPacketType.EmoticonsBlocked:
                player.EmoticonsBlocked = isActive;
                message = player.EmoticonsBlocked ? Game18NConstString.SmileysDisabled : Game18NConstString.SmileysEnabled;
                return true;
            case GopPacketType.HpBlocked:
                player.HpBlocked = isActive;
                message = player.HpBlocked ? Game18NConstString.HpStatusDisabled : Game18NConstString.HpStatusEnabled;
                return true;
            case GopPacketType.BuffBlocked:
                player.BuffBlocked = isActive;
                message = player.BuffBlocked ? Game18NConstString.BuffSequenceHidden : Game18NConstString.BuffSequenceShown;
                return true;
            case GopPacketType.QuickGetUp:
                player.QuickGetUp = isActive;
                message = player.QuickGetUp ? Game18NConstString.ClickToGetUp : Game18NConstString.FastGetUpDisabled;
                return true;
            case GopPacketType.MinilandInviteBlocked:
                player.MinilandInviteBlocked = isActive;
                message = player.MinilandInviteBlocked ? Game18NConstString.MinilandInviteBlocked : Game18NConstString.MinilandInvitePermitted;
                return true;
            case GopPacketType.MouseAimLock:
                player.MouseAimLock = isActive;
                return true;
            default:
                return false;
        }
    }

    // The stat packet carries one bit per option at position (option - 1). The mouse aim bit
    // is set while the lock is off.
    public static double GenerateStatOption(this PlayerComponentBundle player)
    {
        var mask = Bit(player.ExchangeBlocked, GopPacketType.ExchangeBlocked)
            | Bit(player.FriendRequestBlocked, GopPacketType.FriendRequestBlocked)
            | Bit(player.WhisperBlocked, GopPacketType.WhisperBlocked)
            | Bit(player.GroupRequestBlocked, GopPacketType.GroupRequestBlocked)
            | Bit(!player.MouseAimLock, GopPacketType.MouseAimLock)
            | Bit(player.HeroChatBlocked, GopPacketType.HeroChatBlocked)
            | Bit(player.QuickGetUp, GopPacketType.QuickGetUp)
            | Bit(player.EmoticonsBlocked, GopPacketType.EmoticonsBlocked)
            | Bit(player.HpBlocked, GopPacketType.HpBlocked)
            | Bit(player.BuffBlocked, GopPacketType.BuffBlocked)
            | Bit(player.MinilandInviteBlocked, GopPacketType.MinilandInviteBlocked);
        return mask;
    }

    public static void LoadOptions(this PlayerComponentBundle player, CharacterDto characterDto)
    {
        player.ExchangeBlocked = characterDto.ExchangeBlocked;
        player.FriendRequestBlocked = characterDto.FriendRequestBlocked;
        player.WhisperBlocked = characterDto.WhisperBlocked;
        player.GroupRequestBlocked = characterDto.GroupRequestBlocked;
        player.HeroChatBlocked = characterDto.HeroChatBlocked;
        player.EmoticonsBlocked = characterDto.EmoticonsBlocked;
        player.HpBlocked = characterDto.HpBlocked;
        player.BuffBlocked = characterDto.BuffBlocked;
        player.QuickGetUp = characterDto.QuickGetUp;
        player.MinilandInviteBlocked = characterDto.MinilandInviteBlocked;
        player.MouseAimLock = characterDto.MouseAimLock;
    }

    public static void SaveOptions(this PlayerComponentBundle player, CharacterDto characterDto)
    {
        characterDto.ExchangeBlocked = player.ExchangeBlocked;
        characterDto.FriendRequestBlocked = player.FriendRequestBlocked;
        characterDto.WhisperBlocked = player.WhisperBlocked;
        characterDto.GroupRequestBlocked = player.GroupRequestBlocked;
        characterDto.HeroChatBlocked = player.HeroChatBlocked;
        characterDto.EmoticonsBlocked = player.EmoticonsBlocked;
        characterDto.HpBlocked = player.HpBlocked;
        characterDto.BuffBlocked = player.BuffBlocked;
        characterDto.QuickGetUp = player.QuickGetUp;
        characterDto.MinilandInviteBlocked = player.MinilandInviteBlocked;
        characterDto.MouseAimLock = player.MouseAimLock;
    }

    private static int Bit(bool set, GopPacketType option) => set ? 1 << ((int)option - 1) : 0;
}
