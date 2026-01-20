//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Dto;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.GroupService;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.MapChangeService;
using NosCore.GameObject.Services.QuestService;
using NosCore.Networking;
using NosCore.Packets.Enumerations;
using NosCore.Packets.Interfaces;
using NosCore.Shared.Enumerations;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using MailData = NosCore.GameObject.InterChannelCommunication.Messages.MailData;

namespace NosCore.GameObject.ComponentEntities.Interfaces
{
    public interface ICharacterEntity : INamedEntity, IRequestableEntity
    {
        bool FriendRequestBlocked { get; }

        bool UseSp { get; }

        AuthorityType Authority { get; }

        short MapId { get; set; }

        bool IsVehicled { get; }

        byte? VehicleSpeed { get; }

        GenderType Gender { get; }

        HairStyleType HairStyle { get; }

        HairColorType HairColor { get; }

        CharacterClassType Class { get; }

        ReputationType ReputIcon { get; }

        DignityType DignityIcon { get; }

        bool Camouflage { get; }
        long JobLevelXp { get; set; }
        long HeroXp { get; set; }
        byte JobLevel { get; set; }
        new byte HeroLevel { get; set; }
        int SpPoint { get; set; }
        int SpAdditionPoint { get; set; }

        bool Invisible { get; }

        IChannel? Channel { get; }

        bool GroupRequestBlocked { get; }

        ConcurrentDictionary<long, long> GroupRequestCharacterIds { get; }

        List<QuicklistEntryDto> QuicklistEntries { get; }

        ConcurrentDictionary<short, CharacterSkill> Skills { get; }

        long Gold { get; set; }

        long BankGold { get; }

        IInventoryService InventoryService { get; }

        RegionType AccountLanguage { get; }

        List<StaticBonusDto> StaticBonusList { get; }

        List<TitleDto> Titles { get; }

        bool IsDisconnecting { get; }
        ScriptDto? Script { get; }
        Guid? CurrentScriptId { get; }
        ConcurrentDictionary<Guid, CharacterQuest> Quests { get; }
        short Compliment { get; }
        long Reput { get; set; }
        short Dignity { get; }

        Task GenerateMailAsync(IEnumerable<MailData> data);

        Task SendPacketAsync(IPacket packetDefinition);

        Task SendPacketsAsync(IEnumerable<IPacket> packetDefinitions);

        void AddBankGold(long bankGold);

        void RemoveBankGold(long bankGold);

        Task ChangeClassAsync(CharacterClassType classType);

        Task ChangeMapAsync(IMapChangeService mapChangeService, short mapId, short mapX, short mapY);
        string GetMessageFromKey(LanguageKey support);
    }
}
