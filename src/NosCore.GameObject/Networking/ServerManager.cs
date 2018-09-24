using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using NosCore.Core;
using NosCore.Core.Networking;
using NosCore.Data;
using NosCore.Data.StaticEntities;
using NosCore.Data.WebApi;
using NosCore.DAL;
using NosCore.Shared.Enumerations.Map;
using NosCore.Shared.I18N;
using Mapster;

namespace NosCore.GameObject.Networking
{
    public sealed class ServerManager : BroadcastableBase
    {
        private static ServerManager _instance;

        private ServerManager()
        {
        }
        private static int _seed = Environment.TickCount;
        private static readonly ThreadLocal<Random> Random = new ThreadLocal<Random>(() => new Random(Interlocked.Increment(ref _seed)));
        public int RandomNumber(int min = 0, int max = 100)
        {
            return Random.Value.Next(min, max);
        }
        public static ServerManager Instance => _instance ?? (_instance = new ServerManager());

        private void LaunchEvents()
        {
            Observable.Interval(TimeSpan.FromMinutes(5)).Subscribe(x => { SaveAll(); });
        }
        
        public void SaveAll()
        {
            try
            {
                Logger.Log.Info(LogLanguage.Instance.GetMessageFromKey(LanguageKey.SAVING_ALL));
                Parallel.ForEach(Sessions.Values.Where(s => s.Character != null), session =>
                {
                    session.Character.Save();
                });
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }
        
        public void BroadcastPacket(PostedPacket postedPacket, int? channelId = null)
        {
            if (channelId == null)
            {
                foreach (var channel in WebApiAccess.Instance.Get<List<WorldServerInfo>>("api/channels"))
                {
                    WebApiAccess.Instance.Post<PostedPacket>("api/packet", postedPacket, channel.WebApi);
                }
            }
            else
            {
                var channel = WebApiAccess.Instance.Get<List<WorldServerInfo>>("api/channels", id: channelId.Value).FirstOrDefault();
                if (channel != null)
                {
                    WebApiAccess.Instance.Post<PostedPacket>("api/packet", postedPacket, channel.WebApi);
                }
            }
        }

        public void BroadcastPackets(List<PostedPacket> packets, int? channelId = null)
        {
            foreach (var packet in packets)
            {
                BroadcastPacket(packet, channelId);
            }
        }
    }
}