//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

namespace NosCore.GameObject.InterChannelCommunication;

public class MessagingConfiguration
{
    public MessagingTransport Transport { get; set; } = MessagingTransport.Postgresql;

    public int PollingIntervalMs { get; set; } = 20;

    public string? RabbitMqUri { get; set; }
}
