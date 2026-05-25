using Server_vodenko.Application.DTOs;
using Server_vodenko.Domain;

namespace Server_vodenko.Application.Interfaces
{
    public interface IVodenkoService
    {
        PlcDto GetPlc();
        Task<List<VodenkoDto>> GetTrendsAsync(int minutes);
        Task<List<AlarmsDto>> GetAlarmsAsync(int minutes);
        Task<L2ToPlcDto> GetControlRowAsync();
        Task UpdateControlAsync(L2ToPlcDto dto);
        Task SetResetPulseAsync();
    }
}
