using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MadWolfTwitchBot.Models
{
    public class ShoutOut
    {
        public string Name { get; set; }
        public Uri Link { get; set; }

        public string Game { get; set; }

        public DateTime? StreamDateTime { get; set; }
        public bool IsLive { get; set; }
    }
}
