using Server_vodenko.Application.DTOs;
using Server_vodenko.Domain;

namespace Server_vodenko.Application.Interfaces
{
    public interface IVodenkoRepository
    {
        Plc GetPlc();
        Task<List<Vodenko>> GetTrendsAsync(int minutes);
        Task<List<Alarms>> GetAlarmsAsync(int minutes);
        Task<L2ToPlc> GetControlRowAsync();
        Task UpdateControlAsync(L2ToPlcDto dto);
        Task SetResetPulseAsync();
        Task SaveTrendAsync(VodenkoDto trends);
        Task SaveAlarmAsync(AlarmsDto alarms);
    }
}
