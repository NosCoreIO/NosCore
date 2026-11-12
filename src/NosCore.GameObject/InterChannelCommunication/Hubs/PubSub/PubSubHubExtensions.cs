//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.WebApi;
using NosCore.GameObject.InterChannelCommunication.Hubs.ChannelHub;
using NosCore.Shared.Enumerations;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.GameObject.InterChannelCommunication.Hubs.PubSub;

public static class PubSubHubExtensions
{
    // Subscribers playing on a world server; the login and master channels subscribe too.
    public static async Task<List<Subscriber>> GetWorldSubscribersAsync(this IPubSubHub pubSubHub,
        IChannelHub channelHub)
    {
        var worldServerIds = (await channelHub.GetCommunicationChannels())
            .Where(c => c.Type == ServerType.WorldServer)
            .Select(c => c.Id)
            .ToHashSet();
        return (await pubSubHub.GetSubscribersAsync())
            .Where(s => worldServerIds.Contains(s.ChannelId))
            .ToList();
    }

    public static async Task<Subscriber?> GetWorldSubscriberAsync(this IPubSubHub pubSubHub, IChannelHub channelHub,
        long characterId) =>
        (await pubSubHub.GetWorldSubscribersAsync(channelHub))
        .FirstOrDefault(s => s.ConnectedCharacter?.Id == characterId);

    public static async Task<List<CharacterRelationStatus>> GetRelationStatusesAsync(this IPubSubHub pubSubHub,
        IChannelHub channelHub, IDao<CharacterDto, long> characterDao, List<CharacterRelationDto>? relations)
    {
        if (relations == null || relations.Count == 0)
        {
            return new List<CharacterRelationStatus>();
        }

        var online = (await pubSubHub.GetWorldSubscribersAsync(channelHub))
            .Select(s => s.ConnectedCharacter?.Id)
            .OfType<long>()
            .ToHashSet();
        var relatedIds = relations.Select(s => s.RelatedCharacterId).Distinct().ToList();
        var names = characterDao.Where(s => relatedIds.Contains(s.CharacterId))?
            .ToDictionary(s => s.CharacterId, s => (string?)s.Name) ?? new Dictionary<long, string?>();

        return relations.Select(rel => new CharacterRelationStatus
        {
            CharacterName = names.GetValueOrDefault(rel.RelatedCharacterId),
            CharacterId = rel.RelatedCharacterId,
            IsConnected = online.Contains(rel.RelatedCharacterId),
            RelationType = rel.RelationType,
            CharacterRelationId = rel.CharacterRelationId
        }).ToList();
    }
}
