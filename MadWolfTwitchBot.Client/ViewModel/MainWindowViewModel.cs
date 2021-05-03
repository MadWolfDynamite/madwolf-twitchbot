using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using MadWolfTwitchBot.Api;
using MadWolfTwitchBot.Client.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading;
using System.Windows.Input;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Models;

namespace MadWolfTwitchBot.Client.ViewModel
{
    public class MainWindowViewModel : ViewModelBase
    {
        private TwitchClient _client;
        private readonly BotConfigModel _config = new BotConfigModel();

        private readonly SynchronizationContext _uiContext;

        private string _windowTitle;
        public string Title
        {
            get => _windowTitle;
            set
            {
                if (_windowTitle != value)
                {
                    _windowTitle = value;
                    RaisePropertyChanged("Title");
                }
            }
        }

        private bool _connected = true;
        public bool IsDisconnected 
        {
            get => _connected;
            set
            {
                if (_connected != value)
                {
                    _connected = value;
                    RaisePropertyChanged("IsConnected");
                }
            }
        }

        public string BotName
        {
            get => _config.UserName;
            set
            {
                if (_config.UserName != value)
                {
                    _config.UserName = value;
                    RaisePropertyChanged("BotName");
                }
            }
        }

        public string TwitchChannel
        {
            get => _config.Channel;
            set
            {
                if (_config.Channel != value)
                {
                    _config.Channel = value;
                    RaisePropertyChanged("TwitchChannel");
                }
            }
        }

        private readonly Dictionary<string, string> _commands;
        public ObservableCollection<BotCommandModel> ChatCommands { get; set; } = new ObservableCollection<BotCommandModel>();

        public ObservableCollection<MessageModel> Messages { get; set; } = new ObservableCollection<MessageModel>();

        public ICommand ConnectCommand { get; private set; }
        public ICommand DisconnectCommand { get; private set; }

        public ICommand AddCommand { get; private set; }
        public ICommand RemoveCommand { get; private set; }

        public MainWindowViewModel()
        {
            if (IsInDesignMode)
                Title = "MadWolf Twitch Bot Client (Design Mode)";
            else
                Title = "MadWolf Twitch Bot Client";

            _config.SetConfig("D:/TestConfig.json");
            _uiContext = SynchronizationContext.Current;

            _commands = BotCommandService.LoadCommands("D:/Commands.json");
            foreach (var command in _commands)
            {
                var data = new BotCommandModel
                {
                    Command = command.Key,
                    Message = command.Value
                };

                ChatCommands.Add(data);
            }

            ConnectCommand = new RelayCommand(Connect, CanConnect);

            AddCommand = new RelayCommand(AddChatCommand, CanAddChatCommand);

            //Connect();
        }

        private bool CanConnect()
        {
            return IsDisconnected && !String.IsNullOrWhiteSpace(_config.UserName) && !String.IsNullOrWhiteSpace(_config.Channel) && !String.IsNullOrWhiteSpace(_config.Token);
        }
        private void Connect()
        {
            ConnectionCredentials credentials = new ConnectionCredentials(_config.UserName, _config.Token);
            var clientOptions = new ClientOptions
            {
                MessagesAllowedInPeriod = 750,
                ThrottlingPeriod = TimeSpan.FromSeconds(30)
            };

            WebSocketClient customClient = new WebSocketClient(clientOptions);
            _client = new TwitchClient(customClient);
            _client.Initialize(credentials, TwitchChannel);

            _client.OnConnected += OnBotConnected;
            _client.OnDisconnected += OnBotDisconnected;
            _client.OnJoinedChannel += OnChannelJoined;
            _client.OnMessageReceived += OnReceivedMessage;

            _client.Connect();
        }

        private void OnBotConnected(object sender, OnConnectedArgs e)
        {
            Title = $"[CONNECTED] {Title}";
            IsDisconnected = false;
        }
        private void OnBotDisconnected(object sender, TwitchLib.Communication.Events.OnDisconnectedEventArgs e)
        {
            Title = Title.Replace("[CONNECTED] ", "");
        }

        private void OnChannelJoined(object sender, OnJoinedChannelArgs e)
        {
            _client.SendMessage(e.Channel, BotCommandService.GetConnectionMessage());
        }

        private void OnReceivedMessage(object sender, OnMessageReceivedArgs e) 
        {
            var msg = new MessageModel
            {
                DisplayName = e.ChatMessage.DisplayName,
                Message = e.ChatMessage.Message,
                HexColour = e.ChatMessage.ColorHex
            };

            if (e.ChatMessage.Username.Equals(_config.UserName))
                return;

            var parsedMessage = msg.Message.Trim().ToLower();

            if (_commands.ContainsKey(parsedMessage))
            {
                var botMessage = _commands[parsedMessage].Replace("{name}", msg.DisplayName);
                _client.SendReply(_client.JoinedChannels.FirstOrDefault(), e.ChatMessage.Id, botMessage);

                return;
            }
            if (parsedMessage.Contains("!shoutout"))
            {
                //TODO: Shoutout command
                return;
            }

            switch (parsedMessage)
            {
                case "!heaven":
                    _client.SendMessage(_client.JoinedChannels.FirstOrDefault(), "Uses Final Heaven");
                    Thread.Sleep(4200);
                    _client.SendMessage(_client.JoinedChannels.FirstOrDefault(), $"Critical direct hit! {msg.DisplayName} takes 731858 damage.");
                    break;
                case "!uptime":
                    break;
                case "!hello":
                case "!hi":
                case "!yo":
                    _client.SendReply(_client.JoinedChannels.FirstOrDefault(), e.ChatMessage.Id, $"Nods at {msg.DisplayName}");
                    break;
                case "!o/":
                    _client.SendReply(_client.JoinedChannels.FirstOrDefault(), e.ChatMessage.Id, @"\o");
                    break;
                default:
                    _uiContext.Send(x => Messages.Add(msg), null);
                    break;
            }
        }

        private bool CanAddChatCommand()
        {
            return ChatCommands.Count == _commands.Count;
        }
        private void AddChatCommand()
        {
            ChatCommands.Add(new BotCommandModel());
        }
    }
}
