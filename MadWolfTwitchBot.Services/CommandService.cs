using MadWolfTwitchBot.Domain;
using MadWolfTwitchBot.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MadWolfTwitchBot.Services
{
    public static class CommandService
    {
        private static readonly CommandRepository m_repository = new(@"D:\DevStuff\bottest.db");

        public static async Task<IEnumerable<Command>> GetAllBotCommands()
        {
            return await m_repository.ListAll<Command>();
        }

        public static async Task<IEnumerable<Command>> GetCommandsForBot(long id)
        {
            return await m_repository.GetByBotId<Command>(id);
        }

        public static async Task<Command> CreateOrUpdateCommand(
            long id,
            string name,
            string message,
            long? botId = null)
        {
            var data = await m_repository.GetById<Command>(id);
            var isNew = data == null;

            if (isNew)
                data = new Command();

            data.Name = name;
            data.ResponseMessage = message;

            data.BotId = botId;

            var result = isNew
                ? await m_repository.CreateNewCommand(data.Id, data.Name, data.ResponseMessage, data.BotId)
                : await m_repository.UpdateCommand(data.Id, data.Name, data.ResponseMessage, data.BotId);

            return result;
        }

        public static async Task<string> GenerateShoutoutMessage(string client, string token, string user)
        {
            var data = await WolfAPIService.GetShoutOutDetails(client, token, user);

            var streamTime = "";
            if (data.StreamDateTime.HasValue)
            {
                var streamDuration = DateTime.UtcNow - data.StreamDateTime.Value;

                streamTime = streamDuration.TotalHours > 0
                    ? $"{Math.Floor(streamDuration.TotalHours)} {(Math.Floor(streamDuration.TotalHours) == 1 ? "hour" : "hours")}"
                    : $"{Math.Floor(streamDuration.TotalMinutes)} {(Math.Floor(streamDuration.TotalMinutes) == 1 ? "minute" : "minutes")}";
            }

            var message = data.IsLive
                ? $"Check out {data.Link} ({data.Name}) who...has been streaming {data.Game} for {streamTime}!"
                : $"Check out {data.Link} ({data.Name}) who was previously streaming {data.Game}!";

            return $"/announce {message}";
        }
    }
}
