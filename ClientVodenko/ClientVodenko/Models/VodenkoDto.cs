using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClientVodenko.Models
{
    public class VodenkoDto
    {
        public bool Setpoint_Invalid { get; set; }
        public bool Manual_Valve_Invalid { get; set; }
        public bool Tank_Overfill { get; set; }
        public float Actual_Level { get; set; }
        public float Valve_Position { get; set; }
    }
}
