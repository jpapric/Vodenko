using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClientVodenko.Models
{
    public class L2ToPlcDto
    {
        public float? Level_Setpoint { get; set; }
        public float? Manual_Valve_Value { get; set; }
        public bool? Automatic_Manual { get; set; }
        public bool? Start_Pump { get; set; }
        public bool? Reset_ { get; set; }
    }
}
