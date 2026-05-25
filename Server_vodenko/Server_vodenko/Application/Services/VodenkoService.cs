using Server_vodenko.Application.Interfaces;

namespace Server_vodenko.Application.Services
{
    public class VodenkoService : IVodenkoService
    {
        private IVodenkoRepository _repository;
        public VodenkoService(IVodenkoRepository repository)
        {
            _repository = repository;
        }
    }
}
