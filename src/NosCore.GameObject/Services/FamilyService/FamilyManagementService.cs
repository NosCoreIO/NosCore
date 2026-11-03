//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using FamilyAuthority = NosCore.Data.Enumerations.Family.FamilyAuthority;
using NosCore.GameObject.InterChannelCommunication.Hubs.PubSub;
using NosCore.GameObject.InterChannelCommunication.Messages;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.FamilyService
{
    public class FamilyManagementService(IFamilyService familyService, IPubSubHub pubSubHub) : IFamilyManagementService
    {
        public async Task ChangeAuthorityAsync(ClientSession session, string? characterName, FamilyAuthority authority)
        {
            if (session.Character.Family == null || string.IsNullOrWhiteSpace(characterName))
            {
                return;
            }

            var target = await familyService.FindCharacterAsync(characterName);
            if (target == null || await ChangeAuthorityAsync(session, target.CharacterId, authority) == FamilyAuthorityChange.NotInFamily)
            {
                await session.SendPacketAsync(new Infoi2Packet
                {
                    Message = Game18NConstString.CanNotFind,
                    ArgumentType = 1,
                    Game18NArguments = { characterName }
                });
            }
        }

        public async Task<FamilyAuthorityChange> ChangeAuthorityAsync(ClientSession session, long targetId, FamilyAuthority authority)
        {
            var family = session.Character.Family;
            if (family == null)
            {
                return FamilyAuthorityChange.NotInFamily;
            }

            var result = await familyService.ChangeAuthorityAsync(family, session.Character.CharacterId, targetId, authority);
            Game18NConstString? message = result switch
            {
                FamilyAuthorityChange.NotAuthorized => Game18NConstString.NotAuthorized,
                FamilyAuthorityChange.OnlyAssistantCanBecomeHead => Game18NConstString.OnlyFamilyDeputyCanBecomeHEad,
                FamilyAuthorityChange.TooManyAssistants => Game18NConstString.CanNotAppointMoreDeputy,
                FamilyAuthorityChange.Changed => authority switch
                {
                    FamilyAuthority.Head => Game18NConstString.FamilyHeadChanged,
                    FamilyAuthority.Assistant => Game18NConstString.NewFamilyDeputyAppointed,
                    FamilyAuthority.Manager => Game18NConstString.NewFamilyKeeperAppointed,
                    _ => null
                },
                _ => null
            };
            if (message != null)
            {
                await session.SendPacketAsync(new InfoiPacket { Message = message.Value });
            }

            if (result == FamilyAuthorityChange.Changed)
            {
                var changed = new FamilyChangedData { FamilyId = family.FamilyId, CharacterIds = { targetId } };
                if (authority == FamilyAuthority.Head)
                {
                    changed.CharacterIds.Add(session.Character.CharacterId);
                }

                await pubSubHub.SendMessageAsync(changed);
            }

            return result;
        }
    }
}
