namespace Server_vodenko.Domain
{
    public class L2ToPlc
    {
        public float? Level_Setpoint { get; set; }
        public float? Manual_Valve_Value { get; set; }
        public bool? Automatic_Manual { get; set; }
        public bool? Start_Pump { get; set; }
        public bool? Reset { get; set; }

        public L2ToPlc(float? level_Setpoint, float? manual_Valve_Value, bool? automatic_Manual, bool? start_Pump, bool? reset)
        {
            Level_Setpoint = level_Setpoint;
            Manual_Valve_Value = manual_Valve_Value;
            Automatic_Manual = automatic_Manual;
            Start_Pump = start_Pump;
            Reset = reset;
        }
    }
}
