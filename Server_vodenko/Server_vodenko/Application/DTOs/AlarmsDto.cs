namespace Server_vodenko.Application.DTOs
{
    public class AlarmsDto
    {
        public bool Setpoint_Invalid { get; set; }
        public bool Manual_Valve_Invalid { get; set; }
        public bool Tank_Overfill { get; set; }
        public DateTime Time_Saved { get; set; }
    }
}
