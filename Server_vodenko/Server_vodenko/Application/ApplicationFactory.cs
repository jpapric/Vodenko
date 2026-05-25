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

        public static AlarmsDto GetAlarmsDtofromDomain(Alarms alarm)
        {
            return new AlarmsDto
            {
                Setpoint_Invalid = alarm.Setpoint_Invalid,
                Manual_Valve_Invalid = alarm.Manual_Valve_Invalid,
                Tank_Overfill = alarm.Tank_Overfill,
                Time_Saved = alarm.Time_Saved
            };
        }

        public static VodenkoDto GetVodenkoDtofromDomain(Vodenko vodenko)
        {
            return new VodenkoDto
            {
                Actual_Level = vodenko.Actual_Level,
                Valve_Position = vodenko.Valve_Position,
                Time_Saved = vodenko.Time_Saved
            };
        }

        public static L2ToPlcDto GetL2ToPlcDtofromDomain(L2ToPlc l2ToPlc)
        {
            return new L2ToPlcDto
            {
                Level_Setpoint = l2ToPlc.Level_Setpoint,
                Manual_Valve_Value = l2ToPlc.Manual_Valve_Value,
                Automatic_Manual = l2ToPlc.Automatic_Manual,
                Start_Pump = l2ToPlc.Start_Pump,
                Reset_ = l2ToPlc.Reset_
            };
        }


    }
}
