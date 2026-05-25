using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClientVodenko.Models
{
    public class PLCDto
    {
        public string Ip { get; set; }
        public int Rack { get; set; }
        public int Slot { get; set; }
        public CpuType Cpu { get; set; }

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
