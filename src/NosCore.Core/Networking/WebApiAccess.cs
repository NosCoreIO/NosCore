using Newtonsoft.Json;
using NosCore.Configuration;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;

namespace NosCore.Core.Networking
{
    public class WebApiAccess
    {
        private static WebApiAccess instance;

        private static Uri baseAddress { get; set; }

        public static WebApiAccess Instance
        {
            get
            {
                if (baseAddress == null)
                    throw new NullReferenceException("baseAddress can't be null");

                if (instance == null)
                {
                    instance = new WebApiAccess();
                }
                return instance;
            }
        }

        public static void RegisterBaseAdress(string address)
        {
            baseAddress = new Uri(address);
        }

        public T Get<T>(string route, ServerConfiguration webApi = null)
        {
            try
            {
                HttpClient client = new HttpClient();
                client.BaseAddress = webApi == null ? baseAddress : new Uri(webApi.ToString());
                HttpResponseMessage response = client.GetAsync(route).Result;
                if (response.IsSuccessStatusCode)
                {
                    return JsonConvert.DeserializeObject<T>(response.Content.ReadAsStringAsync().Result);
                }
                else
                {
                    throw new HttpRequestException(response.Headers.ToString());
                }
            }
            catch
            {
                throw;
            }
        }

    }
}
