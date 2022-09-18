using MadWolfTwitchBot.Models;

namespace MadWolfTwitchBot.Client.Model
{
    public class BasicPromo
    {
        public long Id { get; set; }

        public long BotId { get; set; }

        public string ResponseMessage { get; set; }

        public BasicPromo() : this(0, 0, string.Empty) { }
        public BasicPromo(BotPromo promo) : this(promo.Id, promo.BotId, promo.ResponseMessage) { }

        public BasicPromo(long id, long botId, string responseMessage)
        {
            Id = id;
            BotId = botId;
            ResponseMessage = responseMessage;
        }

        public override string ToString()
        {
            return ResponseMessage;
        }
    }
}
