//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Linq;
using System.Threading.Tasks;
using NodaTime;
using NosCore.Data.Enumerations.Character;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Messaging.Handlers.UseItem;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.GameObject.Services.MinilandService;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;

namespace NosCore.GameObject.Services.MateService
{
    // No game source gives these figures; they are a best guess until the real game is measured.
    public sealed class MateLoyaltyService(
        ISessionRegistry sessionRegistry,
        IMinilandRegistry minilandRegistry,
        IRandomProvider random,
        IClock clock,
        IPartnerSkillCatalog partnerSkillCatalog) : IMateLoyaltyService, ISingletonService
    {
        public const int HitLossChance = 2;
        public const short MinHitLoss = 1;
        public const short MaxHitLoss = 5;
        public const short KnockOutLoss = 50;
        public const short MinilandGain = 100;
        public static readonly Duration MinilandGainInterval = Duration.FromSeconds(5);

        public Task HitAsync(PlayerComponentBundle owner, Mate mate) =>
            random.Next(0, 100) < HitLossChance
                ? LoseAsync(owner, mate, (short)random.Next(MinHitLoss, MaxHitLoss + 1))
                : Task.CompletedTask;

        public Task KnockedOutAsync(PlayerComponentBundle owner, Mate mate) => LoseAsync(owner, mate, KnockOutLoss);

        // An owner's mates win back loyalty while the owner is in their own Miniland.
        public async Task TickAsync(MapInstance mapInstance)
        {
            if (minilandRegistry.GetByMapInstanceId(mapInstance.MapInstanceId) is not { } miniland
                || !sessionRegistry.TryGetCharacter(s => s.CharacterId == miniland.OwnerId, out var owner)
                || owner.MapInstanceId != mapInstance.MapInstanceId)
            {
                return;
            }

            var now = clock.GetCurrentInstant();
            foreach (var mate in owner.Mates.Values.Where(m => m.Loyalty < MateItemHandler.MaxLoyalty && m.NextLoyaltyGainAt <= now))
            {
                mate.NextLoyaltyGainAt = clock.GetCurrentInstant().Plus(MinilandGainInterval);
                mate.Loyalty = (short)Math.Min(mate.Loyalty + MinilandGain, MateItemHandler.MaxLoyalty);
                await RefreshAsync(owner, mate);
            }
        }

        private async Task LoseAsync(PlayerComponentBundle owner, Mate mate, short amount)
        {
            if (mate.Loyalty <= 0)
            {
                return;
            }

            mate.Loyalty = (short)Math.Max(mate.Loyalty - amount, 0);
            await RefreshAsync(owner, mate);
            if (mate.Loyalty > 0)
            {
                return;
            }

            await owner.SendPacketAsync(Message(Game18NConstString.PetLoyaltyIsEmpty));
            await owner.SendPacketAsync(Message(Game18NConstString.FeedPetOrGoToMiniland));
        }

        private Task RefreshAsync(PlayerComponentBundle owner, Mate mate) =>
            owner.SendPacketAsync(mate.MateType == MateType.Pet
                ? mate.GenerateScp(owner.AccountLanguage)
                : mate.GenerateScn(owner.AccountLanguage, owner.InventoryService, partnerSkillCatalog));

        private static MsgiPacket Message(Game18NConstString message) => new()
        {
            Type = MessageType.Default,
            Message = message
        };
    }
}
