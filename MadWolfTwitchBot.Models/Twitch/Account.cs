using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MadWolfTwitchBot.Models.Twitch
{
    public class Account
    {
        public string Id { get; set; }
        public string Login { get; set; }
        public string Display_Name { get; set; }

        public string Profile_Image_Url { get; set; }
        public int View_Count { get; set; }
    }
}
