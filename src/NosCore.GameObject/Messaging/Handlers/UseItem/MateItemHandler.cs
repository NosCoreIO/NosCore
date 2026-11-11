//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.Algorithm.MateExperienceService;
using NosCore.Data.Enumerations.Character;
using NosCore.Data.Enumerations.Items;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.MateService;
using NosCore.Networking;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using MateModel = NosCore.GameObject.Services.MateService.Mate;

namespace NosCore.GameObject.Messaging.Handlers.UseItem
{
    [UsedImplicitly]
    public sealed class MateItemHandler(IMateService mateService, IMateExperienceService mateExperienceService)
    {
        // A fed mate must stay this many levels under its owner.
        public const int LevelUpGap = 5;

        private const int LevelUpEffect = 8;
        private const int LevelUpAura = 198;
        private const int PickUpEffect = 5;
        private const int PickUpAura = 5002;

        [UsedImplicitly]
        public async Task Handle(ItemUsedEvent evt)
        {
            var item = evt.InventoryItem.ItemInstance.Item;
            var isTeacher = item.ItemType == ItemType.Teacher;
            var isSkin = item.ItemType == ItemType.Special && item.Effect == ItemEffectType.ApplySkinPartner;
            if (!isTeacher && !isSkin)
            {
                return;
            }

            var owner = evt.ClientSession.Character;
            // The client names the mate the player has selected as the target of the item.
            if (evt.Packet.VisualType != VisualType.Npc || !owner.Mates.TryGetValue(evt.Packet.VisualId, out var mate))
            {
                return;
            }

            var used = isSkin
                ? await ApplySkinAsync(owner, mate, item.EffectValue, item.Morph)
                : (TeacherItemEffectType)item.Effect switch
                {
                    TeacherItemEffectType.PetLevelUp when mate.MateType == MateType.Pet => await LevelUpAsync(owner, mate),
                    TeacherItemEffectType.PartnerLevelUp when mate.MateType == MateType.Partner => await LevelUpAsync(owner, mate),
                    TeacherItemEffectType.PetPickUpTraining when mate.MateType == MateType.Pet => await TeachPickUpAsync(owner, mate),
                    TeacherItemEffectType.MateSummoningBook => await MakeSummonableAsync(owner, mate),
                    TeacherItemEffectType.ReleasePet when mate.MateType == MateType.Pet => await ReleaseAsync(owner, mate),
                    TeacherItemEffectType.ReleasePartner when mate.MateType == MateType.Partner => await ReleaseAsync(owner, mate),
                    _ => false
                };
            if (!used)
            {
                return;
            }

            var itemInstance = evt.InventoryItem;
            owner.InventoryService.RemoveItemAmountFromInventory(1, itemInstance.ItemInstanceId);
            await owner.SendPacketAsync(itemInstance.GeneratePocketChange(itemInstance.Type.ToPocketType(), itemInstance.Slot));
        }

        private async Task<bool> LevelUpAsync(PlayerComponentBundle owner, MateModel mate)
        {
            if (mate.Level + LevelUpGap > owner.Level)
            {
                await owner.SendPacketAsync(Message(mate.MateType == MateType.Pet
                    ? Game18NConstString.TooLowLevelToUpPet
                    : Game18NConstString.TooLowLevelToUpPartner));
                return false;
            }

            mate.Level++;
            mate.XpLoad = mate.MateType == MateType.Pet
                ? mateExperienceService.GetPetExperience(mate.Level)
                : mateExperienceService.GetPartnerExperience(mate.Level);
            mate.Hp = mate.MaxHp;
            mate.Mp = mate.MaxMp;
            if (mate.Entity is { } entity)
            {
                entity.Level = mate.Level;
                entity.Hp = mate.Hp;
                entity.Mp = mate.Mp;
                await owner.MapInstance.SendPacketAsync(entity.GenerateEff(LevelUpEffect));
                await owner.MapInstance.SendPacketAsync(entity.GenerateEff(LevelUpAura));
            }

            await RefreshAsync(owner, mate);
            return true;
        }

        private static async Task<bool> TeachPickUpAsync(PlayerComponentBundle owner, MateModel mate)
        {
            if (mate.CanPickUp)
            {
                return false;
            }

            mate.CanPickUp = true;
            if (mate.Entity is { } entity)
            {
                await owner.MapInstance.SendPacketAsync(entity.GenerateEff(PickUpEffect));
                await owner.MapInstance.SendPacketAsync(entity.GenerateEff(PickUpAura));
            }

            await RefreshAsync(owner, mate);
            await owner.SendPacketAsync(Message(Game18NConstString.PetCollectItems));
            return true;
        }

        private static async Task<bool> MakeSummonableAsync(PlayerComponentBundle owner, MateModel mate)
        {
            if (mate.IsSummonable)
            {
                await owner.SendPacketAsync(Message(Game18NConstString.SummoningBookAlreadyUsed));
                return false;
            }

            mate.IsSummonable = true;
            await RefreshAsync(owner, mate);
            await owner.SendPacketAsync(new MsgiPacket
            {
                Type = MessageType.Default,
                Message = Game18NConstString.UsedMateSummoningBook,
                ArgumentType = 8,
                Game18NArguments = { mate.VNum }
            });
            return true;
        }

        private async Task<bool> ReleaseAsync(PlayerComponentBundle owner, MateModel mate)
        {
            var isPet = mate.MateType == MateType.Pet;
            if (mate.IsTeamMember)
            {
                await owner.SendPacketAsync(Message(isPet
                    ? Game18NConstString.CanNotReleasePetInYourTeam
                    : Game18NConstString.CannotSeparateFromPartnerInTeam));
                return false;
            }

            if (mate.EquipmentPocket() is { } pocket && owner.InventoryService.Values.Any(s => s.Type == pocket))
            {
                await owner.SendPacketAsync(Message(Game18NConstString.ReleasePartnerWithoutEquipmentOnly));
                return false;
            }

            await mateService.ReleaseAsync(owner, mate);
            await owner.SendPacketsAsync(owner.GenerateMateRoster());
            return true;
        }

        private static async Task<bool> ApplySkinAsync(PlayerComponentBundle owner, MateModel mate, int partnerVNum, short skin)
        {
            if (mate.MateType != MateType.Partner || mate.VNum != partnerVNum || skin == 0)
            {
                await owner.SendPacketAsync(Message(Game18NConstString.CostumeNotFit));
                return false;
            }

            if (mate.Skin != 0)
            {
                return false;
            }

            mate.Skin = skin;
            if (mate.Entity is { } entity)
            {
                entity.Morph = mate.Morph();
                await owner.MapInstance.SendPacketAsync(entity.GenerateCMode());
            }

            await RefreshAsync(owner, mate);
            return true;
        }

        private static Task RefreshAsync(PlayerComponentBundle owner, MateModel mate) =>
            owner.SendPacketAsync(mate.MateType == MateType.Pet
                ? mate.GenerateScp(owner.AccountLanguage)
                : mate.GenerateScn(owner.AccountLanguage, owner.InventoryService));

        private static MsgiPacket Message(Game18NConstString message) => new()
        {
            Type = MessageType.Default,
            Message = message
        };
    }
}
