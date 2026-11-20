//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BattleService;
using NosCore.Packets.ClientPackets.Battle;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Battle;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.PathFinder.Interfaces;
using NosCore.Shared.Enumerations;

namespace NosCore.PacketHandlers.Battle
{
    // A skill cast at a map cell. The cell has to be within the skill's range of the caster;
    // who is hit there is the battle service's call.
    public class UseAoeSkillPacketHandler(
        IBattleService battleService,
        ISkillCastService skillCastService,
        IHeuristic distanceCalculator)
        : PacketHandler<UseAoeSkillPacket>, IWorldPacketHandler
    {
        // Has the client put the name of the item whose vnum follows into the line.
        private const byte ItemNameArgument = 2;

        public override async Task ExecuteAsync(UseAoeSkillPacket packet, ClientSession clientSession)
        {
            var character = clientSession.Character;
            if (!character.CanFight || character.IsVehicled || character.MapInstance == null)
            {
                await CancelAsync(clientSession, packet.CastId, CancelPacketType.CancelAutoAttack);
                return;
            }

            if (character.IsSitting)
            {
                await character.RestAsync();
            }

            var skill = skillCastService.FindLearned(character, packet.CastId);
            var distance = (int)distanceCalculator.GetDistance((character.PositionX, character.PositionY), (packet.MapX, packet.MapY));
            if (skill?.Skill == null || !SkillReach.IsTargetInReach(skill.Skill.Range, distance))
            {
                await CancelAsync(clientSession, packet.CastId, CancelPacketType.CancelAutoAttack);
                return;
            }

            switch (skillCastService.Check(character, skill, out var mpCost))
            {
                case SkillCastRefusal.NotEnoughMp:
                    await NotEnoughMpAsync(character);
                    await CancelAsync(clientSession, packet.CastId, CancelPacketType.CancelAutoAttack);
                    return;
                case SkillCastRefusal.MissingItem:
                    await MissingItemAsync(character, skill.Skill.ItemVNum);
                    await CancelAsync(clientSession, packet.CastId, CancelPacketType.CancelAutoAttack);
                    return;
                case SkillCastRefusal.OnCooldown:
                    await CancelAsync(clientSession, packet.CastId, CancelPacketType.CancelCast);
                    return;
            }

            if (await battleService.HitArea(character, packet.MapX, packet.MapY, new HitArguments { SkillId = packet.CastId }))
            {
                await skillCastService.SpendAsync(character, skill, mpCost);
            }
        }

        internal static Task NotEnoughMpAsync(PlayerComponentBundle character)
        {
            return character.SendPacketAsync(character.GenerateSayi(SayColorType.Yellow, Game18NConstString.NotEnoughMpForSpell));
        }

        internal static Task MissingItemAsync(PlayerComponentBundle character, short itemVNum)
        {
            return character.SendPacketAsync(character.GenerateSayi(SayColorType.Yellow, Game18NConstString.MissingItemForSkill,
                ItemNameArgument, itemVNum.ToString()));
        }

        internal static Task CancelAsync(ClientSession session, long castId, CancelPacketType type)
        {
            return session.SendPacketAsync(new CancelPacket { Type = type, TargetId = castId, Unknow = -1 });
        }
    }
}
