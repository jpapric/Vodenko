using System;

namespace Server_vodenko.Application.DTOs
{
    public class VodenkoDto
    {
        public float Actual_Level { get; set; }
        public float Valve_Position { get; set; }
        public DateTime Time_Saved { get; set; }

        public bool Setpoint_Invaild { get; set; }
        public bool Manual_Valve_Invalid { get; set; }
        public bool Tank_Overfill { get; set; }
    }
}
