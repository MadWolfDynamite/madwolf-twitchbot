using MadWolfTwitchBot.Domain;
using MadWolfTwitchBot.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MadWolfTwitchBot.Services
{
    public static class BotService
    {
        private static readonly BotRepository m_repository = new BotRepository(@"D:\DevStuff\bottest.db");

        public static async Task<IEnumerable<Bot>> GetAllConfiguredBots()
        {
            return await m_repository.ListAll<Bot>();
        }
    }
}
