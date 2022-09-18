using MadWolfTwitchBot.Domain;
using MadWolfTwitchBot.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MadWolfTwitchBot.Services
{
    public static class PromoService
    {
        private static readonly PromoRepository m_repository = new (@"D:\DevStuff\bottest.db");

        public static async Task<IEnumerable<BotPromo>> GetAllCommands()
        {
            return await m_repository.ListAll<BotPromo>();
        }

        public static async Task<IEnumerable<BotPromo>> GetMessagesForBot(long id)
        {
            return await m_repository.GetByBotId<BotPromo>(id);
        }

        public static async Task<BotPromo> CreateOrUpdateBotPromo(
            long id,
            long botId,
            string message)
        {
            var data = await m_repository.GetById<BotPromo>(id);
            var isNew = data == null;

            if (isNew)
                data = new BotPromo();

            data.BotId = botId;
            data.ResponseMessage = message;

            var result = isNew
                ? await m_repository.CreateNewBotPromo(data.Id, data.BotId, data.ResponseMessage)
                : await m_repository.UpdateBotPromo(data.Id, data.BotId, data.ResponseMessage);

            return result;
        }

        public static async Task<bool> DeleteBotPromo(long id)
        {
            return await m_repository.DeleteById(id);
        }
    }
}
