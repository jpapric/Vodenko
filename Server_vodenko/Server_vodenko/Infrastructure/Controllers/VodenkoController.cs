using Microsoft.AspNetCore.Mvc;
using Server_vodenko.Application.DTOs;
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
        [Route("[action]/{minutes}")]
        public async Task<IActionResult> GetAlarms(int minutes)
        {
            try
            {
                List<AlarmsDto> alarms = await _service.GetAlarmsAsync(minutes);
                return Ok(alarms);
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }

        [HttpGet]
        [Route("[action]/{minutes}")]
        public async Task<IActionResult> GetTrends(int minutes)
        {
            try
            {
                List<VodenkoDto> trends = await _service.GetTrendsAsync(minutes);
                return Ok(trends);
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }

        [HttpGet]
        [Route("[action]")]
        public async Task<IActionResult> GetControlRow()
        {
            try
            {
                L2ToPlcDto controlRow = await _service.GetControlRowAsync();
                return Ok(controlRow);
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }

        [HttpPost]
        [Route("[action]")]
        public async Task<IActionResult> SetResetPulse()
        {
            try
            {
                await _service.SetResetPulseAsync();
                return Ok();
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }

        [HttpPost]
        [Route("[action]")]
        public async Task<IActionResult> UpdateControlRow([FromBody] L2ToPlcDto dto)
        {
            try
            {
                await _service.UpdateControlAsync(dto);
                return Ok();
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }
    }
}
