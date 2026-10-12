//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Json.Patch;
using NosCore.Data.WebApi;

namespace NosCore.GameObject.InterChannelCommunication.Hubs.MailHub;

public record GetMailsRequest(long Id, long CharacterId, bool SenderCopy) : IMasterRequest;

public record DeleteMailRequest(long Id, long CharacterId, bool SenderCopy) : IMasterRequest;

public record ViewMailRequest(long Id, JsonPatch MailData) : IMasterRequest;

public record SendMailRequest(MailRequest Mail) : IMasterRequest;
