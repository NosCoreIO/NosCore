//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NodaTime;
using NosCore.Data.Enumerations;
using NosCore.Data.WebApi;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.InterChannelCommunication.Hubs.MailHub;
using NosCore.GameObject.InterChannelCommunication.Hubs.PubSub;
using NosCore.GameObject.InterChannelCommunication.Messages;
using NosCore.GameObject.Services.MailService;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.RemoteCharacterService;

// Acts on a character by name, whichever channel they are connected to. Each call returns
// false when no connected character has that name.
public interface IRemoteCharacterService
{
    Task<bool> UpdateStatAsync(string characterName, UpdateStatActionType actionType, long value);

    Task<bool> KickAsync(string? characterName);

    Task<bool> SendGiftAsync(ICharacterEntity sender, string? characterName, short vnum, short amount, sbyte rare,
        byte upgrade);
}

public sealed class RemoteCharacterService(IPubSubHub pubSubHub, IMailHub mailHub, IClock clock)
    : IRemoteCharacterService
{
    public async Task<bool> UpdateStatAsync(string characterName, UpdateStatActionType actionType, long value)
    {
        if (await FindAsync(characterName) == null)
        {
            return false;
        }

        await pubSubHub.SendMessageAsync(new StatData
        {
            ActionType = actionType,
            Character = new Character { Name = characterName },
            Data = value
        });
        return true;
    }

    public async Task<bool> KickAsync(string? characterName)
    {
        var receiver = await FindAsync(characterName);
        if (receiver == null)
        {
            return false;
        }

        await pubSubHub.UnsubscribeAsync(receiver.ConnectedCharacter!.Id);
        return true;
    }

    public async Task<bool> SendGiftAsync(ICharacterEntity sender, string? characterName, short vnum, short amount,
        sbyte rare, byte upgrade)
    {
        var receiver = await FindAsync(characterName);
        if (receiver == null)
        {
            return false;
        }

        await mailHub.SendMailAsync(GiftHelper.GenerateMailRequest(clock, sender, receiver.ConnectedCharacter!.Id, null,
            vnum, amount, rare, upgrade, false, null, null));
        return true;
    }

    private async Task<Subscriber?> FindAsync(string? characterName)
    {
        if (characterName == null)
        {
            return null;
        }

        var subscribers = await pubSubHub.GetSubscribersAsync();
        return subscribers.FirstOrDefault(x => x.ConnectedCharacter != null && x.ConnectedCharacter.Name == characterName);
    }
}
