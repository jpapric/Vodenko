namespace Server_vodenko.Application.DTOs
{
    public class PlcDto
    {
        public string Ip { get; set; }
        public int Rack { get; set; }
        public int Slot { get; set; }
        public string Cpu { get; set; }

        public enum CpuType
        {
            S7200 = 0,
            S7300 = 10,
            S7400 = 20,
            S71200 = 30,
            S71500 = 40
        }
    }
}
