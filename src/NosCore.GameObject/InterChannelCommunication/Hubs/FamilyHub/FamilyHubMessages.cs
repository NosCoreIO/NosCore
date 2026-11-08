//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

namespace NosCore.GameObject.InterChannelCommunication.Hubs.FamilyHub;

public record PurchaseFamilyExtensionRequest(long FamilyId, long CharacterId, short ItemVNum) : IMasterRequest;
