using NosCore.Configuration;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Networking.Group;
using NosCore.Packets.ClientPackets;
using NosCore.Packets.ServerPackets;
using NosCore.Shared.Enumerations;
using NosCore.Shared.Enumerations.Items;
using NosCore.Shared.I18N;
using System;

namespace NosCore.GameObject.Services.MapItemBuilder.Handlers
{
    public class GoldDropHandler : IHandler<MapItem, Tuple<MapItem, GetPacket>>
    {
        public bool Condition(MapItem item) => item.VNum == 1046;

        public void Execute(RequestData<Tuple<MapItem, GetPacket>> requestData)
        {
            // handle gold drop
            var maxGold = requestData.ClientSession.WorldConfiguration.MaxGoldAmount;
            if (requestData.ClientSession.Character.Gold + requestData.Data.Item1.Amount <= maxGold)
            {
                if (requestData.Data.Item2.PickerType == PickerType.Mate)
                {
                    requestData.ClientSession.SendPacket(requestData.ClientSession.Character.GenerateIcon(1, requestData.Data.Item1.VNum));
                }

                requestData.ClientSession.Character.Gold += requestData.Data.Item1.Amount;
                requestData.ClientSession.SendPacket(requestData.ClientSession.Character.GenerateSay(
                    $"{Language.Instance.GetMessageFromKey(LanguageKey.ITEM_ACQUIRED, requestData.ClientSession.Account.Language)}" +
                    $": {requestData.Data.Item1.ItemInstance.Item.Name} x {requestData.Data.Item1.Amount}",
                    SayColorType.Green));
            }
            else
            {
                requestData.ClientSession.Character.Gold = maxGold;
                requestData.ClientSession.SendPacket(new MsgPacket
                {
                    Message = Language.Instance.GetMessageFromKey(LanguageKey.MAX_GOLD,
                        requestData.ClientSession.Account.Language),
                    Type = 0
                });
            }

            requestData.ClientSession.SendPacket(requestData.ClientSession.Character.GenerateGold());
            requestData.ClientSession.Character.MapInstance.MapItems.TryRemove(requestData.Data.Item1.VisualId, out _);
            requestData.ClientSession.Character.MapInstance.Sessions.SendPacket(requestData.ClientSession.Character.GenerateGet(requestData.Data.Item1.VisualId));
        }
    }
}
