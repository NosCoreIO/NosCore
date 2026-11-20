//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.Character;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.MateService;
using NosCore.GameObject.Services.ScriptedInstanceService;
using NosCore.Networking;
using NosCore.Packets.ClientPackets.Npcs;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.Mates;
using NosCore.Packets.ServerPackets.Player;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;

namespace NosCore.GameObject.Messaging.Handlers.Nrun
{
    // A time-space talk's offer to take its companion along: the NPC leaves the room and stays with
    // the player as a partner.
    [UsedImplicitly]
    public sealed class TimeSpaceCompanionHandler(IScriptedInstanceService scriptedInstanceService,
        IDao<MateDto, long> mateDao, IMateService mateService, INpcMonsterCatalog npcMonsters,
        IPartnerSkillCatalog partnerSkillCatalog) : INrunEventHandler
    {
        private const short NewPartnerLoyalty = 3000;
        private const byte JoinedAction = 3;
        private const int JoinedEffect = 198;
        private const byte NpcNameArgument = 8;

        public NrunRunnerType Runner => NrunRunnerType.GetXCompanion;

        public bool RequiresNpc => false;

        public Task HandleAsync(ClientSession session, IAliveEntity? target, NrunPacket packet)
        {
            var player = session.Character;
            if (packet.Type is not { } vNum
                || target is not PlayerComponentBundle { CharacterId: var answering } || answering != player.CharacterId
                || scriptedInstanceService.GetRun(player.MapInstance.MapInstanceId) is not { } run)
            {
                return Task.CompletedTask;
            }

            return run.CompanionAnsweredAsync(player, vNum, companion => JoinAsync(player, companion));
        }

        private async Task<bool> JoinAsync(PlayerComponentBundle player, NpcComponentBundle companion)
        {
            if (player.Mates.Values.Any(s => s.MateType == MateType.Partner && s.VNum == companion.VNum))
            {
                await player.SendPacketAsync(Say(player, Game18NConstString.AlreadyInYourParty));
                return false;
            }

            if (npcMonsters.GetNpcMonster(companion.VNum) is not { } monster)
            {
                return false;
            }

            var row = await mateDao.TryInsertOrUpdateAsync(new MateDto
            {
                CharacterId = player.CharacterId,
                VNum = companion.VNum,
                MateType = MateType.Partner,
                Level = monster.Level,
                Loyalty = NewPartnerLoyalty,
                Hp = monster.MaxHp,
                Mp = monster.MaxMp,
                IsTeamMember = false
            });
            if (row == null)
            {
                return false;
            }

            var partner = mateService.Adopt(row, monster, player.Mates.Values);
            partner.Hp = partner.MaxHp;
            partner.Mp = partner.MaxMp;
            player.Mates[partner.MateTransportId] = partner;

            await player.SendPacketAsync(new CtlPacket
            {
                Type = VisualType.Npc, PetId = partner.MateTransportId, Action = JoinedAction, Unknown = 0
            });
            var joined = Say(player, Game18NConstString.SomeoneJoinGroup);
            joined.ArgumentType = NpcNameArgument;
            joined.Game18NArguments.Add(companion.VNum);
            await player.SendPacketAsync(joined);
            await player.MapInstance.SendPacketAsync(player.GenerateEff(JoinedEffect));
            await player.MapInstance.SendPacketAsync(new EffectPacket
            {
                EffectType = VisualType.Npc, VisualEntityId = partner.MateTransportId, Id = JoinedEffect
            });
            await player.SendPacketsAsync(player.GenerateMateRoster(partnerSkillCatalog));
            return true;
        }

        private static SayiPacket Say(PlayerComponentBundle player, Game18NConstString message) => new()
        {
            VisualType = VisualType.Player,
            VisualId = player.CharacterId,
            Type = SayColorType.Yellow,
            Message = message
        };
    }
}
