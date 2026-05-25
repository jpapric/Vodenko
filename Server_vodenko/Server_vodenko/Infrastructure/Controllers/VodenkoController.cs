using Microsoft.AspNetCore.Mvc;
using Server_vodenko.Infrastructure.BackgroundServices;
using Server_vodenko.Application.DTOs;
using Server_vodenko.Application.Interfaces;
using Server_vodenko.Domain;
using Server_vodenko.Infrastructure.BackgroundServices;


namespace Server_vodenko.Infrastructure.Controllers
{
    [ApiController]
    [Route("api/vodenko")]
    public class VodenkoController : ControllerBase
    {
        private readonly IVodenkoService _service;
        private readonly PlcDataCache _cache;
        private readonly PlcConnection _plcConnection;
        public VodenkoController(IVodenkoService service, PlcDataCache cache, PlcConnection plcconnection)
        {
            _service = service;
            _cache = cache;
            _plcConnection = plcconnection;

        }

        [HttpGet]
        [Route("[action]")]
        public IActionResult GetPlc()
        {
            try
            {
                PlcDto plc = _service.GetPlc();
                return Ok(plc);
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }

        [HttpGet]
        [Route("[action]")]
        public IActionResult GetVodenkoDataFromPlc()
        {
            try
            {
                var data = _cache.Get();

                if (data == null)
                    return NoContent();

                return Ok(data);
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }

        [HttpPost]
        [Route("[action]")]
        public IActionResult WriteBoolToPlc(
           [FromQuery] string variable,
           [FromQuery] bool state)
        {
            try
            {
                _plcConnection.WriteBool(variable, state);
                return Ok();
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(503, ex.Message);
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }

        [HttpPost]
        [Route("[action]")]
        public IActionResult WriteRealToPlc(
            [FromQuery] string variable,
            [FromQuery] float value)
        {
            try
            {
                _plcConnection.WriteReal(variable, value);
                return Ok();
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(503, ex.Message);
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }

    }
}
