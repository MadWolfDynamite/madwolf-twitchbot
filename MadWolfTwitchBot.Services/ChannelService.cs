using MadWolfTwitchBot.Domain;
using MadWolfTwitchBot.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MadWolfTwitchBot.Services
{
    public static class ChannelService
    {
        private static readonly ChannelRepository m_repository = new ChannelRepository(@"D:\DevStuff\bottest.db");

        public static async Task<IEnumerable<Channel>> GetAllTwitchChannels()
        {
            return await m_repository.ListAll<Channel>();
        }
    }
}
