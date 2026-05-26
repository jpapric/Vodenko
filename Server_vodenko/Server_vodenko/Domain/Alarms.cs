namespace Server_vodenko.Domain
{
    public class Alarms
    {
        public bool Setpoint_Invalid { get; set; }
        public bool Manual_Valve_Invalid { get; set; }
        public bool Tank_Overfill { get; set; }
        public DateTime Time_Saved { get; set; }

        public Alarms(bool setpoint_Invalid, bool manual_Valve_Invalid, bool tank_Overfill, DateTime time_Saved)
        {
            Setpoint_Invalid = setpoint_Invalid;
            Manual_Valve_Invalid = manual_Valve_Invalid;
            Tank_Overfill = tank_Overfill;
            Time_Saved = time_Saved;
        }
    }
}
