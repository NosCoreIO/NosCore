using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using NosCore.Configuration;

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

        public static FormUrlEncodedContent Content { get; private set; }

        public static void RegisterBaseAdress(string address = null, string token = null)
        {
            if (address == null)
            {
                BaseAddress = new Uri("http://localhost");
	            return;
            }

            BaseAddress = new Uri(address);
            var values = new Dictionary<string, string>
            {
                {"ServerToken", token}
            };
            Content = new FormUrlEncodedContent(values);
        }

        public T Get<T>(string route, ServerConfiguration webApi = null)
        {
            if (MockValues.ContainsKey(route))
            {
                return (T)MockValues[route];
            }

            var client = new HttpClient();
            HttpResponseMessage response;
            client.BaseAddress = webApi == null ? BaseAddress : new Uri(webApi.ToString());
            if (Token == null)
            {
                response = client.PostAsync("api/token/connectserver", Content).Result;
                if (response.IsSuccessStatusCode)
                {
                    Token = response.Content.ReadAsStringAsync().Result;
                }
                else
                {
                    throw new HttpRequestException(response.Headers.ToString());
                }
            }

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token);
            response = client.GetAsync(route).Result;
            if (response.IsSuccessStatusCode)
            {
                return JsonConvert.DeserializeObject<T>(response.Content.ReadAsStringAsync().Result);
            }

            throw new HttpRequestException(response.Headers.ToString());
        }
    }
}