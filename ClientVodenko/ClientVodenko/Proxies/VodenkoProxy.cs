using ClientVodenko.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.Net.Http;
using Newtonsoft.Json;

namespace ClientVodenko.Proxies
{
    public class VodenkoProxy
    {
        private HttpClient _httpClient;

        public VodenkoProxy()
        {
            string baseUrl = ConfigurationManager.AppSettings["ApiBaseUrl"];
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(baseUrl)
            };
        }

        public async Task<VodenkoDto> GetEafDataFromPlcAsync()
        {
            var response = await _httpClient.GetAsync("GetEafDataFromPlc");
            response.EnsureSuccessStatusCode();
            string json = await response.Content.ReadAsStringAsync();
            return Newtonsoft.Json.JsonConvert.DeserializeObject<VodenkoDto>(json);
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
            StringContent content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("UpdatePlc", content);
            response.EnsureSuccessStatusCode();
        }

        public async Task ResetAsync()
        {
            var response = await _httpClient.PostAsync("Reset", null);
            response.EnsureSuccessStatusCode();
        }

        public async Task SetSetpointAsync(float setpoint)
        {
            var content = new StringContent(
                setpoint.ToString(System.Globalization.CultureInfo.InvariantCulture),
                Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("SetCurrent", content);
            response.EnsureSuccessStatusCode();
        }

        public async Task SetValvePositionAsync(float valvePosition)
        {
            var content = new StringContent(
                valvePosition.ToString(System.Globalization.CultureInfo.InvariantCulture),
                Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("SetAngle", content);
            response.EnsureSuccessStatusCode();
        }

        public async Task<PLCDto> GetPlcAsync()
        {
            var response = await _httpClient.GetAsync("GetPlc");
            response.EnsureSuccessStatusCode();
            string json = await response.Content.ReadAsStringAsync();
            return Newtonsoft.Json.JsonConvert.DeserializeObject<PLCDto>(json);
        }

        public async Task UpdatePlcAsync(PLCDto plc)
        {
            string json = Newtonsoft.Json.JsonConvert.SerializeObject(plc);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("UpdatePlc", content);
            response.EnsureSuccessStatusCode();
        }

        public async Task<List<EventDto>> GetEventsAsync()
        {
            var response = await _httpClient.GetAsync("GetEvents");
            response.EnsureSuccessStatusCode();
            string json = await response.Content.ReadAsStringAsync();
            return Newtonsoft.Json.JsonConvert.DeserializeObject<List<EventDto>>(json);
        }

        public async Task EventDetectionAsync()
        {
            var response = await _httpClient.PostAsync("Event_detection", null);
            response.EnsureSuccessStatusCode();
        }
    }
}