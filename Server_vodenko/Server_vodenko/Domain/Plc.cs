namespace Server_vodenko.Domain
{
    public class Plc
    {
        public string Ip { get; }
        public int Rack { get; }
        public int Slot { get; }

        public string Cpu { get; }

        public enum CpuType
        {
            S7200 = 0,
            S7300 = 10,
            S7400 = 20,
            S71200 = 30,
            S71500 = 40
        }

        public Plc() { }
        public Plc(string cpu, string ip, int rack, int slot)
        {
            Cpu = cpu;
            Ip = ip;
            Rack = rack;
            Slot = slot;
        }
    }
}
