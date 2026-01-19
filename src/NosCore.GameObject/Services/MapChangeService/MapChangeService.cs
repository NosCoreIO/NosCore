
using System;
using System.Linq;
using System.Threading.Tasks;
using NodaTime;
using NosCore.Algorithm.ExperienceService;
using NosCore.Algorithm.HeroExperienceService;
using NosCore.Algorithm.JobExperienceService;
using NosCore.Core.I18N;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Group;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Enumerations.Map;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.GameObject.Services.MapInstanceAccessService;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.GameObject.Services.MinilandService;
using NosCore.Networking;
using NosCore.Networking.SessionGroup.ChannelMatcher;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Map;
using NosCore.Shared.Enumerations;
using NosCore.Shared.I18N;
using Serilog;

namespace NosCore.GameObject.Services.MapChangeService
{
    public class MapChangeService(IExperienceService experienceService, IJobExperienceService jobExperienceService,
            IHeroExperienceService heroExperienceService, IMapInstanceAccessorService mapInstanceAccessorService,
            IClock clock,
            ILogLanguageLocalizer<LogLanguageKey> logLanguage, IMinilandService minilandProvider, ILogger logger,
            ILogLanguageLocalizer<LogLanguageKey> logLanguageLocalizer, IGameLanguageLocalizer gameLanguageLocalizer,
            ISessionRegistry sessionRegistry)
        : IMapChangeService
    {
        public async Task ChangeMapAsync(ClientSession session, short? mapId = null, short? mapX = null, short? mapY = null)
        {
            if (session.Character == null!)
            {
                return;
            }

            if (mapId != null)
            {
                var mapInstance = mapInstanceAccessorService.GetBaseMapById((short)mapId);

                if (mapInstance == null)
                {
                    logger.Error(
                        logLanguageLocalizer[LogLanguageKey.MAP_DONT_EXIST, session.Account.Language]);
                    return;
                }

                session.Character.MapInstance = mapInstance;
            }
            
            mapInstanceAccessorService.GetMapInstance(session.Character.MapInstanceId);
            await ChangeMapInstanceAsync(session, session.Character.MapInstanceId, mapX, mapY);
        }

        public async Task ChangeMapInstanceAsync(ClientSession session, Guid mapInstanceId, int? mapX = null, int? mapY = null)
        {
            if ((session.Character?.MapInstance == null) || session.Character.IsChangingMapInstance)
            {
                return;
            }

            try
            {
                session.Character.IsChangingMapInstance = true;

                if (session.Channel?.Id != null)
                {
                    session.Character.MapInstance.Sessions.Remove(session.Channel);
                }
                await session.SendPacketAsync(session.Character.MapInstance.GenerateCMap(false));
                session.Character.MapInstance.LastUnregister = clock.GetCurrentInstant();
                await LeaveMapAsync(session);
                if (session.Character.MapInstance.Sessions.Count == 0)
                {
                    session.Character.MapInstance.IsSleeping = true;
                }

                if (session.Character.IsSitting)
                {
                    session.Character.IsSitting = false;
                }
                
                session.Character.MapInstance = mapInstanceAccessorService.GetMapInstance(mapInstanceId)!;

                if (session.Character.MapInstance.MapInstanceType == MapInstanceType.BaseMapInstance)
                {
                    session.Character.MapId = session.Character.MapInstance.Map.MapId;
                    if ((mapX != null) && (mapY != null))
                    {
                        session.Character.MapX = (short)mapX;
                        session.Character.MapY = (short)mapY;
                    }
                }

                if ((mapX != null) && (mapY != null))
                {
                    session.Character.PositionX = (short)mapX;
                    session.Character.PositionY = (short)mapY;
                }

                await session.SendPacketAsync(session.Character.GenerateCInfo());
                await session.SendPacketAsync(session.Character.GenerateCMode());
                await session.SendPacketAsync(session.Character.GenerateEq());
                await session.SendPacketAsync(session.Character.GenerateEquipment());
                await session.SendPacketAsync(session.Character.GenerateLev(experienceService, jobExperienceService, heroExperienceService));
                await session.SendPacketAsync(session.Character.GenerateStat());
                await session.SendPacketAsync(session.Character.GenerateAt());
                await session.SendPacketAsync(session.Character.GenerateCond());
                await session.SendPacketAsync(session.Character.MapInstance.GenerateCMap(true));
                await session.SendPacketAsync(session.Character.GeneratePairy(
                    session.Character.InventoryService.LoadBySlotAndType((byte)EquipmentType.Fairy,
                        NoscorePocketType.Wear)?.ItemInstance as WearableInstance));
                await session.SendPacketsAsync(session.Character.MapInstance.GetMapItems(session.Character.AccountLanguage));
                await session.SendPacketsAsync(session.Character.MapInstance.MapDesignObjects.Values.Select(mp => mp.GenerateEffect()));

                var minilandPortals = minilandProvider
                    .GetMinilandPortals(session.Character.CharacterId)
                    .Where(s => s.SourceMapInstanceId == mapInstanceId)
                    .ToList();

                if (minilandPortals.Count > 0)
                {
                    await session.SendPacketsAsync(minilandPortals.Select(s => s.GenerateGp()));
                }

                await session.SendPacketAsync(session.Character.Group!.GeneratePinit());
                if (!session.Character.Group.IsEmpty)
                {
                    await session.SendPacketsAsync(session.Character.Group.GeneratePst());
                }

                if ((session.Character.Group.Type == GroupType.Group) && (session.Character.Group.Count > 1))
                {
                    await session.Character.MapInstance.SendPacketAsync(session.Character.Group.GeneratePidx(session.Character));
                }

                var mapSessions = sessionRegistry.GetCharacters(s =>
                    (s != session.Character) && (s.MapInstance.MapInstanceId == session.Character.MapInstanceId));

                await Task.WhenAll(mapSessions.Select(async s =>
                {
                    await session.SendPacketAsync(s.GenerateIn(s.Authority == AuthorityType.Moderator
                        ? $"[{gameLanguageLocalizer[LanguageKey.SUPPORT, s.AccountLanguage]}"
                        : string.Empty));
                    if (s.Shop == null)
                    {
                        return;
                    }

                    await session.SendPacketAsync(s.GeneratePFlag());
                    await session.SendPacketAsync(s.GenerateShop(session.Account.Language));
                }));
                await session.Character.MapInstance.SendPacketAsync(session.Character.GenerateTitInfo());
                session.Character.MapInstance.IsSleeping = false;
                if (session.Channel?.Id != null)
                {
                    if (!session.Character.Invisible)
                    {
                        await session.Character.MapInstance.SendPacketAsync(session.Character.GenerateIn(session.Character.Authority == AuthorityType.Moderator
                                ? $"[{gameLanguageLocalizer[LanguageKey.SUPPORT, session.Character.AccountLanguage]}]" : string.Empty),
                            new EveryoneBut(session.Character.Channel!.Id));
                    }

                    session.Character.MapInstance.Sessions.Add(session.Channel);
                }

                session.Character.MapInstance.Requests[typeof(IMapInstanceEntranceEventHandler)]?
                    .OnNext(new RequestData<MapInstance>(session, session.Character.MapInstance));

                session.Character.IsChangingMapInstance = false;
            }
            catch (Exception)
            {
                logger.Warning(logLanguage[LogLanguageKey.ERROR_CHANGE_MAP]);
                session.Character.IsChangingMapInstance = false;
            }
        }

        public async Task ChangeMapByCharacterIdAsync(long characterId, short mapId, short mapX, short mapY)
        {
            var sender = sessionRegistry.GetSenderByCharacterId(characterId);
            if (sender is ClientSession session)
            {
                await ChangeMapAsync(session, mapId, mapX, mapY);
            }
        }

        private async Task LeaveMapAsync(ClientSession session)
        {
            await session.SendPacketAsync(new MapOutPacket());
            await session.Character.MapInstance.SendPacketAsync(session.Character.GenerateOut(),
                new EveryoneBut(session.Channel!.Id));
        }

    }
}
