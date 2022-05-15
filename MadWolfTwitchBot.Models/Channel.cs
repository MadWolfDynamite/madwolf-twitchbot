using MadWolfTools.Common.AttributeExtension;

namespace MadWolfTwitchBot.Models
{
    public class Channel
    {
        public long Id { get; set; }

        public string Username { get; set; }
        [DbColumn("display_name")]
        public string DisplayName { get; set; }
    }
}
