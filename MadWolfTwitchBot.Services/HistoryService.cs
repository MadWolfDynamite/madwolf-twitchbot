﻿using MadWolfTwitchBot.Domain;
using MadWolfTwitchBot.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MadWolfTwitchBot.Services
{
    public static class HistoryService
    {
        private static readonly HistoryRepository m_repository = new HistoryRepository(@"D:\DevStuff\bottest.db");

        public static async Task<IEnumerable<BotHistory>> GetChannelHistoryForBot(long id)
        {
            return await m_repository.GetBotHistory(id);
        }
    }
}
