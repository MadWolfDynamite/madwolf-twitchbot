using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TwitchLib.Client;
using TwitchLib.Client.Events;

namespace MadWolfTwitchBot.BotCommands
{
    public static class BotCommandService
    {
        public static Dictionary<string, string> LoadCommands(string path)
        {
            string json = File.ReadAllText(path);
            var commands = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);

            return commands;
        }
        public static void SaveCommands(IDictionary<string, string> commands, string path)
        {
            string json = JsonConvert.SerializeObject(commands, Formatting.Indented);

            using var writer = new StreamWriter(path);
            writer.Write(json);
        }

        public static string GetConnectionMessage()
        {
            var rng = new Random();
            var messagePool = new string[] { "Thuderclaps her way in", "Meditates to Open Chakra", "Performs Form Shift" };

            var index = rng.Next(0, messagePool.Length);
            return messagePool[index];
        }
    }
}
