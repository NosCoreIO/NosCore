using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NosCore.Configuration;
using NosCore.Core.Serializing;
using NosCore.Shared.I18N;

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
            var values = new Dictionary<string, string>//TODO use JSON instead
            {
                {"ServerToken", token}
            };
            Content = new FormUrlEncodedContent(values);//TODO use StringContent instead
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

        public T Post<T>(string route, object data, ServerConfiguration webApi = null)
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

            var param = new Dictionary<string, string>
            {
                { "postedData", JsonConvert.SerializeObject(data) }
            };
            var content = new FormUrlEncodedContent(param);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token);
            var postResponse = client.PostAsync(route, content).Result;
            if (postResponse.IsSuccessStatusCode)
            {
                return JsonConvert.DeserializeObject<T>(postResponse.Content.ReadAsStringAsync().Result);
            }

            throw new HttpRequestException(postResponse.Headers.ToString());
        
    }

        public T Delete<T>(string route, ServerConfiguration webApi = null)
        {
            if (MockValues.ContainsKey(route))
            {
                return (T)MockValues[route];
            }

            var client = new HttpClient();
            HttpResponseMessage response;
            client.BaseAddress = webApi == null ? BaseAddress : new Uri(webApi.ToString());

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token);
            response = client.DeleteAsync(route).Result;

            if (response.IsSuccessStatusCode)
            {
                return JsonConvert.DeserializeObject<T>(response.Content.ReadAsStringAsync().Result);
            }

            throw new HttpRequestException(response.Headers.ToString());
        }
    }
}