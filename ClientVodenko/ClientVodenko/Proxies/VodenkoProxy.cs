using ClientVodenko.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Client.Models;

namespace ClientVodenko.Proxies
{
    public class VodenkoProxy
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseRoute = "api/vodenko/";

        public VodenkoProxy()
        {
            string baseUrl = ConfigurationManager.AppSettings["ApiBaseUrl"];
            if (!baseUrl.EndsWith("/")) baseUrl += "/";

            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(baseUrl)
            };
        }

        public async Task<PlcDto> GetPlcAsync()
        {
            var response = await _httpClient.GetAsync($"GetPlc");
            response.EnsureSuccessStatusCode();

            string json = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<PlcDto>(json, new Newtonsoft.Json.Converters.StringEnumConverter());
        }

        public async Task<List<AlarmsDto>> GetAlarmsAsync(int minutes)
        {
            var response = await _httpClient.GetAsync($"GetAlarms/{minutes}");
            response.EnsureSuccessStatusCode();

            string json = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<List<AlarmsDto>>(json);
        }

        public async Task<List<VodenkoDto>> GetTrendsAsync(int minutes)
        {
            var response = await _httpClient.GetAsync($"GetTrends/{minutes}");
            response.EnsureSuccessStatusCode();

            string json = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<List<VodenkoDto>>(json);
        }

        public async Task<L2ToPlcDto> GetControlRowAsync()
        {
            var response = await _httpClient.GetAsync($"GetControlRow");
            response.EnsureSuccessStatusCode();

            string json = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<L2ToPlcDto>(json);
        }

        public async Task<VodenkoDto> GetVodenkoDataFromPlcAsync()
        {
            var response = await _httpClient.GetAsync($"GetVodenkoDataFromPlc");
            if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
            {
                return null;
            }
            response.EnsureSuccessStatusCode();

            string json = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<VodenkoDto>(json);
        }

        public async Task SetResetPulseAsync()
        {
            var response = await _httpClient.PostAsync($"SetResetPulse", null);
            response.EnsureSuccessStatusCode();
        }

        public async Task UpdateControlRowAsync(L2ToPlcDto dto)
        {
            string json = JsonConvert.SerializeObject(dto);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"UpdateControlRow", content);
            response.EnsureSuccessStatusCode();
        }

        public async Task WriteBoolToPlcAsync(string variable, bool state)
        {
            var response = await _httpClient.PostAsync($"WriteBoolToPlc?variable={Uri.EscapeDataString(variable)}&state={state}", null);
            response.EnsureSuccessStatusCode();
        }

        public async Task WriteRealToPlcAsync(string variable, float value)
        {
            string valueStr = value.ToString(System.Globalization.CultureInfo.InvariantCulture);
            var response = await _httpClient.PostAsync($"WriteRealToPlc?variable={Uri.EscapeDataString(variable)}&value={valueStr}", null);
            response.EnsureSuccessStatusCode();
        }
    }
}