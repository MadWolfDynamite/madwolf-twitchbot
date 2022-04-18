using MadWolfTwitchBot.Domain;
using MadWolfTwitchBot.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MadWolfTwitchBot.Services
{
    public static class BotService
    {
        private static readonly BotRepository m_repository = new(@"D:\DevStuff\bottest.db");

        public static async Task<IEnumerable<Bot>> GetAllConfiguredBots()
        {
            return await m_repository.ListAll<Bot>();
        }

        public static async Task<Bot> CreateOrUpdateBot(long id, string user, string display, string token, string refresh, DateTime? timestamp)
        {
            var data = await m_repository.GetById<Bot>(id);
            var isNew = data == null;

            if (isNew)
                data = new Bot();

            data.Username = user;
            data.DisplayName = display;

            data.OAuthToken = token;
            data.RefreshToken = refresh;
            data.TokenTimestamp = timestamp;

            var result = isNew ? null : await m_repository.Save(data.Id, data.Username, data.DisplayName, data.OAuthToken, data.RefreshToken, data.TokenTimestamp, data.ChannelId);
            return result;
        }
    }
}
