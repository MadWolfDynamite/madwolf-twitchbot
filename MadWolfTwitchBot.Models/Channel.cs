using MadWolfTools.Common.AttributeExtension;

namespace MadWolfTwitchBot.Models
{
    public class Channel
    {
        public long Id { get; set; }

        public string UserName { get; set; }
        [DbColumn("display_name")]
        public string DisplayName { get; set; }
    }
}
