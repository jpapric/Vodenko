using Server_vodenko.Application.DTOs;
using Server_vodenko.Application.Interfaces;
using Server_vodenko.Domain;

namespace Server_vodenko.Application.Services
{
    public class VodenkoService : IVodenkoService
    {
        private IVodenkoRepository _repository;
        public VodenkoService(IVodenkoRepository repository)
        {
            _repository = repository;
        }

        public PlcDto GetPlc()
        {
            Plc plc = _repository.GetPlc();

            PlcDto plcDto = ApplicationFactory.GetPlcDtofromDomain(plc);

            return plcDto;
        }

        public async Task<List<AlarmsDto>> GetAlarmsAsync(int minutes)
        {
            var alarms = await _repository.GetAlarmsAsync(minutes); 
            var alarmsDtos = alarms.Select(ApplicationFactory.GetAlarmsDtofromDomain).ToList();
            return alarmsDtos;
        }

        public async Task<L2ToPlcDto> GetControlRowAsync()
        {
            var controlRow = await _repository.GetControlRowAsync();
            var controlRowDto = ApplicationFactory.GetL2ToPlcDtofromDomain(controlRow);
            return controlRowDto;
        }

        public async Task<List<VodenkoDto>> GetTrendsAsync(int minutes)
        {
            var trends = await _repository.GetTrendsAsync(minutes); 
            var trendsDtos = trends.Select(ApplicationFactory.GetVodenkoDtofromDomain).ToList();
            return trendsDtos;
        }

        public async Task SetResetPulseAsync()
        {
            await _repository.SetResetPulseAsync();
        }

        public async Task UpdateControlAsync(L2ToPlcDto dto)
        {
            await _repository.UpdateControlAsync(dto);
        }

        public async Task SaveTrendAsync(VodenkoDto vodenko)
        {
            await _repository.SaveTrendAsync(vodenko);
        }

        public async Task SaveAlarmAsync(AlarmsDto alarms)
        {
            await _repository.SaveAlarmAsync(alarms);
        }
    }
}
