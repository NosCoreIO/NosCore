using NosCore.Shared.Enumerations;

namespace NosCore.GameObject.Ecs.Components;

public record struct PlayerFlagsComponent(
    bool ExchangeBlocked,
    bool FriendRequestBlocked,
    bool WhisperBlocked,
    bool GroupRequestBlocked,
    bool HeroChatBlocked,
    bool FamilyRequestBlocked,
    bool EmoticonsBlocked,
    bool QuickGetUp,
    bool HpBlocked,
    bool MinilandInviteBlocked,
    bool MouseAimLock,
    AuthorityType Authority,
    bool UseSp,
    bool IsVehicled,
    bool Invisible,
    bool IsSitting);
