using System;
using System.Collections.Generic;
using System.Text;

namespace TGRaidBot
{
    public class EventData
    {
        public int Raid { get; set; }

        public string Gym { get; set; }

        public string Pokemon { get; set; }

        public string Monster { get; set; }

        public int? Tier { get; set; }
        
        public double? Lat { get; set; }

        public double? Lng { get; set; }

        public int? Start { get; set; }

        public int? End { get; set; }

        public bool Created { get; set; }

        public int? Choice { get; set; }

        public string Time { get; set; }

        public string Submitter { get; set; }


    }
}
