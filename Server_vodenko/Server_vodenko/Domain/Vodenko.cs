namespace Server_vodenko.Domain
{
    public class Vodenko
    {
        public float Actual_Level { get; set; }
        public float Valve_Position { get; set; }
        public DateTime Time_Saved { get; set; }

        public Vodenko(float actual_Level, float valve_Position, DateTime time_Saved)
        {
            Actual_Level = actual_Level;
            Valve_Position = valve_Position;
            Time_Saved = time_Saved;
        }
    }
}
