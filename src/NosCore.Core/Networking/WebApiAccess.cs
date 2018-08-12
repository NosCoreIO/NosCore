using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using NosCore.Configuration;
using NosCore.Data.WebApi;

namespace NosCore.Core.Networking
{
    public class WebApiAccess
    {
        private static WebApiAccess _instance;

        private static Uri BaseAddress { get; set; }

        private static string Token { get; set; }

        public Dictionary<string, object> MockValues { get; set; } = new Dictionary<string, object>();

        public static WebApiAccess Instance
        {
            get
            {
                if (BaseAddress == null)
                {
                    throw new NullReferenceException("baseAddress can't be null");
                }

                return _instance ?? (_instance = new WebApiAccess());
            }
        }

        public static StringContent Content { get; private set; }

        public static void RegisterBaseAdress(string address = null, string token = null)
        {
            if (address == null)
            {
                BaseAddress = new Uri("http://localhost");
                return;
            }

            BaseAddress = new Uri(address);
            Content = new StringContent(JsonConvert.SerializeObject(new WebApiToken {ServerToken = token}),
                Encoding.Default, "application/json");
        }

        private void AssignToken(HttpResponseMessage response, ref HttpClient client)
        {
            if (Token == null)
            {
                response = client.PostAsync("api/token/connectserver", Content).Result;

                if (!response.IsSuccessStatusCode)
                {
                    throw new HttpRequestException(response.Headers.ToString());
                }

                Token = response.Content.ReadAsStringAsync().Result;
            }

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token);
        }

        public T Delete<T>(string route, ServerConfiguration webApi = null, object id = null)
        {
            if (MockValues.ContainsKey(route))
            {
                return (T) MockValues[route];
            }

            var client = new HttpClient();
            var response = new HttpResponseMessage();
            client.BaseAddress = webApi == null ? BaseAddress : new Uri(webApi.ToString());
            AssignToken(response, ref client);
            response = client.DeleteAsync(route + "?id=" + id ?? "").Result;
            if (response.IsSuccessStatusCode)
            {
                return JsonConvert.DeserializeObject<T>(response.Content.ReadAsStringAsync().Result);
            }

            throw new HttpRequestException(response.Headers.ToString());
        }

        public T Get<T>(string route, ServerConfiguration webApi = null, object id = null)
        {
            if (MockValues.ContainsKey(route))
            {
                return (T) MockValues[route];
            }

            var client = new HttpClient();
            var response = new HttpResponseMessage();
            client.BaseAddress = webApi == null ? BaseAddress : new Uri(webApi.ToString());
            AssignToken(response, ref client);
            response = client.GetAsync(route + "?id=" + id ?? "").Result;
            if (response.IsSuccessStatusCode)
            {
                return JsonConvert.DeserializeObject<T>(response.Content.ReadAsStringAsync().Result);
            }

            throw new HttpRequestException(response.Headers.ToString());
        }

        public T Post<T>(string route, object data, ServerConfiguration webApi = null)
        {
            if (MockValues.ContainsKey(route))
            {
                return (T) MockValues[route];
            }

            var client = new HttpClient();
            var response = new HttpResponseMessage();
            client.BaseAddress = webApi == null ? BaseAddress : new Uri(webApi.ToString());
            AssignToken(response, ref client);
            var content = new StringContent(JsonConvert.SerializeObject(data), Encoding.Default, "application/json");
            var postResponse = client.PostAsync(route, content).Result;
            if (postResponse.IsSuccessStatusCode)
            {
                return JsonConvert.DeserializeObject<T>(postResponse.Content.ReadAsStringAsync().Result);
            }

            throw new HttpRequestException(postResponse.Headers.ToString());
        }

        public T Put<T>(string route, object data, ServerConfiguration webApi = null)
        {
            if (MockValues.ContainsKey(route))
            {
                return (T) MockValues[route];
            }

            var client = new HttpClient();
            var response = new HttpResponseMessage();
            client.BaseAddress = webApi == null ? BaseAddress : new Uri(webApi.ToString());
            AssignToken(response, ref client);
            var content = new StringContent(JsonConvert.SerializeObject(data), Encoding.Default, "application/json");
            var postResponse = client.PutAsync(route, content).Result;
            if (postResponse.IsSuccessStatusCode)
            {
                return JsonConvert.DeserializeObject<T>(postResponse.Content.ReadAsStringAsync().Result);
            }

            throw new HttpRequestException(postResponse.Headers.ToString());
        }

        public T Patch<T>(string route, object data, ServerConfiguration webApi = null)
        {
            if (MockValues.ContainsKey(route))
            {
                return (T) MockValues[route];
            }

            var client = new HttpClient();
            var response = new HttpResponseMessage();
            client.BaseAddress = webApi == null ? BaseAddress : new Uri(webApi.ToString());
            AssignToken(response, ref client);
            var content = new StringContent(JsonConvert.SerializeObject(data), Encoding.Default, "application/json");
            var postResponse = client.PatchAsync(route, content).Result;
            if (postResponse.IsSuccessStatusCode)
            {
                return JsonConvert.DeserializeObject<T>(postResponse.Content.ReadAsStringAsync().Result);
            }

            throw new HttpRequestException(postResponse.Headers.ToString());
        }
    }
}