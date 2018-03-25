using Newtonsoft.Json;
using NosCore.Configuration;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace NosCore.Core.Networking
{
    public class WebApiAccess
    {
        private static WebApiAccess instance;

        private static Uri _baseAddress { get; set; }

        private static string _token { get; set; }

        public static WebApiAccess Instance
        {
            get
            {
                if (_baseAddress == null)
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
            _baseAddress = new Uri(address);
        }

        public T Get<T>(string route, ServerConfiguration webApi = null)
        {
            try
            {
                HttpClient client = new HttpClient();
                HttpResponseMessage response;
                client.BaseAddress = webApi == null ? _baseAddress : new Uri(webApi.ToString());
                if (_token == null)
                {
                    var values = new Dictionary<string, string>();
                    values.Add("ServerToken", "something");
                    var content = new FormUrlEncodedContent(values);

                    response = client.PostAsync("api/token/connectserver", content).Result;
                    if (response.IsSuccessStatusCode)
                    {
                        _token = response.Content.ReadAsStringAsync().Result;
                    }
                    else
                    {
                        throw new HttpRequestException(response.Headers.ToString());
                    }
                }
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _token);
                response = client.GetAsync(route).Result;
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
