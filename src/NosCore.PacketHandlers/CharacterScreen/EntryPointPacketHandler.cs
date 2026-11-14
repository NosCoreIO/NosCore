//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Mapster;
using Microsoft.Extensions.Options;
using NosCore.Core.Configuration;
using NosCore.Core.Encryption;
using NosCore.Dao.Interfaces;
using NosCore.Data.CommandPackets;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.Character;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.WebApi;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.InterChannelCommunication.Hubs.AuthHub;
using NosCore.GameObject.InterChannelCommunication.Hubs.ChannelHub;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.Networking;
using NosCore.Networking.SessionRef;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.CharacterSelectionScreen;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using NosCore.Shared.I18N;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.CharacterScreen
{
    public class EntryPointPacketHandler(IDao<CharacterDto, long> characterDao,
            IDao<AccountDto, long> accountDao,
            IDao<MateDto, long> mateDao, ILogger<EntryPointPacketHandler> logger, IAuthHub authHttpClient,
            IChannelHub channelHub, IOptions<WorldConfiguration> configuration,
            ISessionRefHolder sessionRefHolder, ILogLanguageLocalizer<LogLanguageKey> logLanguage)
        : PacketHandler<EntryPointPacket>, IWorldPacketHandler
    {
        public static async Task VerifyConnectionAsync(ClientSession clientSession, ILogger _logger, IAuthHub authHttpClient,
            IDao<AccountDto, long> accountDao, IChannelHub channelHub, string accountName, Func<AccountDto, Task<bool>> authorize, ISessionRefHolder sessionRefHolder, ILogLanguageLocalizer<LogLanguageKey> logLanguage)
        {
            var account = await accountDao.FirstOrDefaultAsync(s => s.Name == accountName);

            if (account == null)
            {
                _logger.LogError(logLanguage[LogLanguageKey.INVALID_ACCOUNT], new
                {
                    accountName
                });
                await clientSession.DisconnectAsync();
                return;
            }

            if (!await authorize(account))
            {
                _logger.LogError(logLanguage[LogLanguageKey.INVALID_PASSWORD], new
                {
                    accountName
                });
                await clientSession.DisconnectAsync();
                return;
            }

            if (account.Authority < AuthorityType.User)
            {
                _logger.LogWarning(logLanguage[LogLanguageKey.WORLD_ENTRY_REFUSED_AUTHORITY], accountName, account.Authority);
                await clientSession.DisconnectAsync();
                return;
            }

            if (account.Authority < AuthorityType.GameMaster && await channelHub.IsInMaintenanceAsync())
            {
                _logger.LogWarning(logLanguage[LogLanguageKey.WORLD_ENTRY_REFUSED_MAINTENANCE], accountName);
                await clientSession.DisconnectAsync();
                return;
            }

            if (!await clientSession.ClaimAccountAsync(account))
            {
                _logger.LogError(logLanguage[LogLanguageKey.ALREADY_CONNECTED], new
                {
                    accountName
                });
                await clientSession.DisconnectAsync();
                return;
            }

            var sessionMapping = sessionRefHolder
                .FirstOrDefault(s => s.Value.SessionId == clientSession.SessionId);
            if (!sessionMapping.Equals(default(KeyValuePair<string, RegionTypeMapping>)))
            {
                sessionMapping.Value.RegionType = account.Language;
            }
            clientSession.InitializeAccount(account);
        }

        public override async Task ExecuteAsync(EntryPointPacket packet, ClientSession clientSession)
        {
            if (!clientSession.HasAccount) // we bypass this when Create new char
            {
                var password = packet.Password ?? throw new InvalidOperationException();
                await VerifyConnectionAsync(clientSession, logger, authHttpClient,
                    accountDao, channelHub, packet.Name, password == "thisisgfmode"
                        ? async _ =>
                        {
                            var grant = await authHttpClient.RedeemWorldEntryAsync(packet.Name, clientSession.SessionId);
                            clientSession.MfaValidated = grant?.MfaValidated ?? false;
                            return grant != null;
                        }
                        : account => Task.FromResult(account.Password?.Equals(new Sha512Hasher().Hash(password), StringComparison.OrdinalIgnoreCase) == true),
                    sessionRefHolder, logLanguage);
                if (!clientSession.HasAccount)
                {
                    return;
                }

                logger.LogInformation(logLanguage[LogLanguageKey.ACCOUNT_ARRIVED],
                    clientSession.Account.Name);
                if (!clientSession.MfaValidated && clientSession.Account.MfaSecret != null)
                {
                    await clientSession.SendPacketAsync(new GuriPacket
                    {
                        Type = GuriPacketType.TextInput,
                        Argument = 3,
                        EntityId = 0
                    });
                    return;
                }
            }

            var characters = characterDao.Where(s =>
                (s.AccountId == clientSession.Account.AccountId) && (s.State == CharacterState.Active) && s.ServerId == configuration.Value.ServerId) ?? [];

            // load characterlist packet for each character in Character
            await clientSession.SendPacketAsync(new ClistStartPacket { Type = (byte)(configuration.Value.AllClassAvailableOnCreate ? 1 : 0) });
            foreach (var character in characters)
            {
                var equipment = new WearableInstance?[16];
                /* IEnumerable<ItemInstanceDTO> inventory = _iteminstanceDAO.Where(s => s.CharacterId == character.CharacterId && s.Type == (byte)InventoryType.Wear);


                 foreach (ItemInstanceDTO equipmentEntry in inventory)
                 {
                     // explicit load of iteminstance
                     WearableInstance currentInstance = equipmentEntry as WearableInstance;
                     equipment[(short)currentInstance.Item.EquipmentSlot] = currentInstance;

                 }
                    */
                var petlist = new List<short?>();
                var mates = (mateDao.Where(s => s.CharacterId == character.CharacterId) ?? []).ToList();
                for (var i = 0; i < 26; i++)
                {
                    if (mates.Count > i)
                    {
                        petlist.Add(mates[i].Skin);
                        petlist.Add(mates[i].VNum);
                    }
                    else
                    {
                        petlist.Add(-1);
                    }
                }

                // 1 1 before long string of -1.-1 = act completion
                await clientSession.SendPacketAsync(new ClistPacket
                {
                    Slot = character.Slot,
                    Name = character.Name,
                    Unknown = 0,
                    Gender = character.Gender,
                    HairStyle = character.HairStyle,
                    HairColor = character.HairColor,
                    Unknown1 = 0,
                    Class = character.Class,
                    Level = character.Level,
                    HeroLevel = character.HeroLevel,
                    Equipments = new List<short?>
                    {
                        equipment[(byte) EquipmentType.Hat]?.ItemVNum,
                        equipment[(byte) EquipmentType.Armor]?.ItemVNum,
                        equipment[(byte) EquipmentType.WeaponSkin]?.ItemVNum ??
                        equipment[(byte) EquipmentType.MainWeapon]?.ItemVNum,
                        equipment[(byte) EquipmentType.SecondaryWeapon]?.ItemVNum,
                        equipment[(byte) EquipmentType.Mask]?.ItemVNum,
                        equipment[(byte) EquipmentType.Fairy]?.ItemVNum,
                        equipment[(byte) EquipmentType.CostumeSuit]?.ItemVNum,
                        equipment[(byte) EquipmentType.CostumeHat]?.ItemVNum
                    },
                    JobLevel = character.JobLevel,
                    QuestCompletion = 1,
                    QuestPart = 1,
                    Pets = petlist,
                    Design = equipment[(byte)EquipmentType.Hat]?.Item?.IsColored ?? false
                        ? equipment[(byte)EquipmentType.Hat]?.Design ?? 0 : 0,
                    Rename = character.ShouldRename
                });
            }

            await clientSession.SendPacketAsync(new ClistEndPacket());
        }
    }
}
