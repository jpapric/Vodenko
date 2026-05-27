using Client.Models;
using ClientVodenko.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

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



        public async Task<(VodenkoDto ProcessData, AlarmsDto ActiveAlarm)> GetVodenkoDataFromPlcAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("GetVodenkoDataFromPlc");

                if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
                {
                    return (null, null);
                }

                response.EnsureSuccessStatusCode();
                string json = await response.Content.ReadAsStringAsync();

                var jsonObject = Newtonsoft.Json.Linq.JObject.Parse(json);

                // POPRAVAK: Ključevi moraju početi MALIM slovima ("processData" i "activeAlarm")
                // Također ih odmah pretvaramo u točne DTO tipove!
                var processData = jsonObject["processData"]?.ToObject<VodenkoDto>();
                var activeAlarm = jsonObject["activeAlarm"]?.ToObject<AlarmsDto>();

                return (processData, activeAlarm);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PAD U PROXYJU: {ex.Message}");
                return (null, null);
            }
        }

        public async Task<AlarmsDto> GetLatestAlarmAsync(int minutes)
        {
            try
            {
                // Gađamo točnu rutu na API-ju i prosljeđujemo minute
                var response = await _httpClient.GetAsync($"GetAlarms?minutes={minutes}");

                if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
                {
                    return null; // Nema alarma u zadnjih X minuta
                }

                response.EnsureSuccessStatusCode();

                string json = await response.Content.ReadAsStringAsync();

                // 1. Deserijaliziramo u LISTU jer API (GetAlarms) vraća List<AlarmsDto>
                var alarmsList = JsonConvert.DeserializeObject<List<AlarmsDto>>(json);

                // 2. Ako je lista prazna, vrati null (nema alarma)
                if (alarmsList == null || !alarmsList.Any())
                {
                    return null;
                }

                // 3. Uzimamo ZADNJI (najnoviji) alarm iz liste
                // (Ovisno o tome kako ih baza sortira, ako idu od najstarijeg prema najnovijem koristi .Last(), 
                // a ako API šalje najnovije na početku liste, koristi .First())
                return alarmsList.Last();
            }
            catch
            {
                return null; // U slučaju greške vraćamo null da klijent ne pukne
            }
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