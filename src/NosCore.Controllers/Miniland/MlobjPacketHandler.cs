using ChickenAPI.Packets.ClientPackets.Miniland;
using ChickenAPI.Packets.Enumerations;
using ChickenAPI.Packets.ServerPackets.Miniland;
using ChickenAPI.Packets.ServerPackets.UI;
using NosCore.Core.I18N;
using NosCore.Data.Enumerations.Character;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Providers.MinilandProvider;
using System;
using System.Collections.Generic;
using System.Text;

namespace NosCore.PacketHandlers.Miniland
{
    public class MlEditPacketHandler : PacketHandler<MLEditPacket>, IWorldPacketHandler
    {
        private readonly IMinilandProvider _minilandProvider;

        public MlEditPacketHandler(IMinilandProvider minilandProvider)
        {
            _minilandProvider = minilandProvider;
        }

        public override void Execute(MLEditPacket mlEditPacket, ClientSession clientSession)
        {
            var miniland = _minilandProvider.GetMiniland(clientSession.Character.CharacterId);
            switch (mlEditPacket.Type)
            {
                case 1:
                    clientSession.SendPacket(new MlintroPacket { Intro = mlEditPacket.MinilandInfo.Replace(' ', '^') });
                    miniland.MinilandMessage = mlEditPacket.MinilandInfo;
                    clientSession.SendPacket(new InfoPacket
                    {
                        Message = Language.Instance.GetMessageFromKey(LanguageKey.MINILAND_INFO_CHANGED, clientSession.Account.Language)
                    });
                    break;

                case 2:
                    switch (mlEditPacket.Parameter)
                    {
                        case MinilandState.Private:
                            clientSession.SendPacket(new MsgPacket
                            {
                                Message = Language.Instance.GetMessageFromKey(LanguageKey.MINILAND_PRIVATE, clientSession.Account.Language)
                            });
                            miniland.State = MinilandState.Private;
                            _minilandProvider.GetMiniland(clientSession.Character.CharacterId).State = MinilandState.Private;
                            break;

                        case MinilandState.Lock:
                            clientSession.SendPacket(new MsgPacket
                            {
                                Message = Language.Instance.GetMessageFromKey(LanguageKey.MINILAND_LOCK, clientSession.Account.Language)
                            });
                            miniland.State = MinilandState.Lock;
                            _minilandProvider.GetMiniland(clientSession.Character.CharacterId).State = MinilandState.Lock;
                            break;

                        case MinilandState.Open:
                            clientSession.SendPacket(new MsgPacket
                            {
                                Message = Language.Instance.GetMessageFromKey(LanguageKey.MINILAND_PUBLIC, clientSession.Account.Language)
                            });
                            break;

                        default:
                            return;
                    }
                    break;
            }
        }
    }
}
