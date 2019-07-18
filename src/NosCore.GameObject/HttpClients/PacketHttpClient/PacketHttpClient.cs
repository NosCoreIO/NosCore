using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using NosCore.Core;
using NosCore.Core.HttpClients;
using NosCore.Core.HttpClients.ChannelHttpClient;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.WebApi;

namespace NosCore.GameObject.HttpClients.PacketHttpClient
{
    public class PacketHttpClient : IPacketHttpClient
    {
        private readonly IChannelHttpClient _channelHttpClient;
        private readonly IHttpClientFactory _httpClientFactory;
        public PacketHttpClient(IHttpClientFactory httpClientFactory, IChannelHttpClient channelHttpClient)
        {
            _channelHttpClient = channelHttpClient;
            _httpClientFactory = httpClientFactory;
        }

        private void SendPacketToChannel(PostedPacket postedPacket, string channel)
        {
            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri(channel);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _channelHttpClient.GetOrRefreshToken());
            var content = new StringContent(JsonConvert.SerializeObject(postedPacket),
                Encoding.Default, "application/json");

            client.PostAsync($"api/packet", content);
        }

        public void BroadcastPacket(PostedPacket packet, int channelId)
        {
            var channel = _channelHttpClient.GetChannel(channelId);
            if (channel != null)
            {
                SendPacketToChannel(packet, channel.WebApi.ToString());
            }
        }

        public void BroadcastPacket(PostedPacket packet)
        {
            foreach (var channel in _channelHttpClient.GetChannels()
                ?.Where(c => c.Type == ServerType.WorldServer) ?? new List<ChannelInfo>())
            {
                SendPacketToChannel(packet, channel.WebApi.ToString());
            }
        }

        public void BroadcastPackets(List<PostedPacket> packets)
        {
            foreach (var packet in packets)
            {
                BroadcastPacket(packet);
            }
        }

        public void BroadcastPackets(List<PostedPacket> packets, int channelId)
        {
            foreach (var packet in packets)
            {
                BroadcastPacket(packet, channelId);
            }
        }
    }
}
