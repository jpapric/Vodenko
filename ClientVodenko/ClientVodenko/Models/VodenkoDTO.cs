using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClientVodenko.Models
{
    public class VodenkoDTO
    {
        public float Actual_Level { get; set; }
        public float Valve_Position { get; set; }

        public DateTime Time_Saved { get; set; }

    }
}
