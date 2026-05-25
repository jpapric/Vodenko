using Microsoft.AspNetCore.Mvc;
using Server_vodenko.Application.Interfaces;


namespace Server_vodenko.Infrastructure.Controllers
{
    [ApiController]
    [Route("api/vodenko")]
    public class VodenkoController : ControllerBase
    {
        private readonly IVodenkoService _service;
        
        public VodenkoController(IVodenkoService service)
        {
            _service = service;

        }
    }
}
