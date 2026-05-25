using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClientVodenko.Models
{
    public class AlarmsDTO
    {
        public bool Setpoint_Invaild { get; set; }
        public bool Manual_Valve_Invalid {get; set; }
        public bool Tank_Overfill { get; set; }
        public DateTime Time_Saved { get; set; }
    }
}
