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

        public Task<List<AlarmsDto>> GetAlarmsAsync(int minutes)
        {
            throw new NotImplementedException();
        }

        public Task<L2ToPlcDto> GetControlRowAsync()
        {
            throw new NotImplementedException();
        }

        public PlcDto GetPlc()
        {
            Plc plc = _repository.GetPlc();

            PlcDto plcDto = ApplicationFactory.GetPlcDtofromDomain(plc);

            return plcDto;
        }

        public Task<List<VodenkoDto>> GetTrendsAsync(int minutes)
        {
            throw new NotImplementedException();
        }

        public Task SetResetPulseAsync(bool value)
        {
            throw new NotImplementedException();
        }

        public Task UpdateControlAsync(L2ToPlcDto dto)
        {
            throw new NotImplementedException();
        }
    }
}
