using System;
using System.Collections.Generic;
using System.Text;

namespace MadWolfTwitchBot.Client.Model
{
    public class ChatMessage
    {
        private string _name;
        public string DisplayName
        {
            get => _name;
            set { _name = value; }
        }

        private string _message;
        public string Message
        {
            get => _message;
            set { _message = value; }
        }

        private string _colour;
        public string HexColour
        {
            get => _colour;
            set { _colour = value; }
        }
    }
}
