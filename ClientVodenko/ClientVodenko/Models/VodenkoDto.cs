using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClientVodenko.Models
{
    public class VodenkoDto
    {
        public bool Setpoint_invalid { get; set; }
        public bool Manual_valve_invalid { get; set; }
        public bool Tank_overfill { get; set; }
        public float Actual_level { get; set; }
        public float Valve_position { get; set; }
    }
}
