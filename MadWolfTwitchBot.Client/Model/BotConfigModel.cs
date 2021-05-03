using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MadWolfTwitchBot.Client.Model
{
    public class BotConfigModel
    {
        private string _name;
        public string UserName
        {
            get { return _name; }
            set { _name = value; }
        }

        private string _token;
        public string Token
        {
            get { return _token; }
            set { _token = value; }
        }

        private string _channel;
        public string Channel
        {
            get { return _channel; }
            set { _channel = value; }
        }

        public void SetConfig(string path)
        {
            string json = File.ReadAllText(path);
            var loadedData = JsonConvert.DeserializeObject<BotConfigModel>(json);

            _name = loadedData.UserName;
            _token = loadedData.Token;
            _channel = loadedData.Channel;
        }
    }
}
