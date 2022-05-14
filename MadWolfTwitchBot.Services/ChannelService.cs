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
        private static readonly ChannelRepository m_repository = new(@"D:\DevStuff\bottest.db");

        public static async Task<IEnumerable<Channel>> GetAllTwitchChannels()
        {
            return await m_repository.ListAll<Channel>();
        }

        public static async Task<Channel> CreateOrUpdateChannel(long id, string username, string displayname)
        {
            var data = await m_repository.GetById<Channel>(id);
            var isNew = data == null;

            if (isNew)
                data = new Channel();

            data.Id = id;

            data.Username = username;
            data.DisplayName = displayname;

            var result = isNew
                ? await m_repository.CreateNewChannel(data.Id, data.Username, data.DisplayName)
                : await m_repository.SaveChannelDetails(data.Id, data.Username, data.DisplayName);

            return result;
        }

        public static async Task<bool> DeleteChannel(long id)
        {
            return await m_repository.DeleteById(id);
        }
    }
}
