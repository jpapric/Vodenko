using Server_vodenko.Domain;
using Server_vodenko.Application.DTOs;

namespace Server_vodenko.Application
{
    public class ApplicationFactory
    {
        public static PlcDto GetPlcDtofromDomain(Plc plc)
        {
            return new PlcDto
            {
                Cpu = plc.Cpu,
                Ip = plc.Ip,
                Rack = plc.Rack,
                Slot = plc.Slot
            };
        }
    }
}
