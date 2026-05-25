using ClientVodenko.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace ClientVodenko.Proxies
{
    public class VodenkoProxy
    {
        private readonly HttpClient _httpClient;

        public VodenkoProxy()
        {
            string baseUrl = ConfigurationManager.AppSettings["ApiBaseUrl"];

            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(baseUrl)
            };
        }

      

        public async Task<PLCDto> GetPlc()
        {
            var response = await _httpClient.GetAsync("GetPlc");

            response.EnsureSuccessStatusCode();

            string json = await response.Content.ReadAsStringAsync();

            PLCDto result = JsonConvert.DeserializeObject<PLCDto>(json);

            return result;
        }

        public async Task UpdatePlc(PLCDto plc)
        {
            string json = JsonConvert.SerializeObject(plc);

            StringContent content = new StringContent(
                json,
                Encoding.UTF8,
                "application/json"
            );

            var response = await _httpClient.PostAsync("UpdatePlc", content);

            response.EnsureSuccessStatusCode();
        }


        public async Task<VodenkoDTO> GetVodenkoDataFromPlcAsync()
        {
            var response = await _httpClient.GetAsync("GetVodenkoDataFromPlc");

            response.EnsureSuccessStatusCode();

            string json = await response.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<VodenkoDTO>(json);
        }

        public async Task<List<VodenkoDTO>> GetTrendsAsync(int min)
        {
            var response = await _httpClient.GetAsync($"GetTrendsAsync/{min}");

            response.EnsureSuccessStatusCode();

            string json = await response.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<List<VodenkoDTO>>(json);
        }

        public async Task<List<AlarmsDTO>> GetAlarmsAsync(int min)
        {
            var response = await _httpClient.GetAsync($"GetAlarmsAsync/{min}");

            response.EnsureSuccessStatusCode();

            string json = await response.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<List<AlarmsDTO>>(json);
        }

        public async Task ResetAsync()
        {
            var response = await _httpClient.PostAsync("Reset", null);
            response.EnsureSuccessStatusCode();
        }





    }
}