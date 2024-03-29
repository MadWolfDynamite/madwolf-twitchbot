﻿using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MadWolfTwitchBot.BotCommands;
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
    public class MainWindowViewModel : ObservableObject
    {
        private TwitchClient _client;
        private readonly BotConfiguration _config = new BotConfiguration();

        private readonly SynchronizationContext _uiContext;

        private string _windowTitle;
        public string Title
        {
            get => _windowTitle;
            set => SetProperty(ref _windowTitle, value);
        }

        private bool _disconnected = true;
        public bool IsDisconnected 
        {
            get => _disconnected;
            set => SetProperty(ref _disconnected, value);
        }

        public string BotName
        {
            get => _config.UserName;
            set
            {
                var oldValue = _config.UserName;
                SetProperty(ref oldValue, value);

                _config.UserName = oldValue;
            }
        }

        public string TwitchChannel
        {
            get => _config.Channel;
            set
            {
                var oldValue = _config.Channel;
                SetProperty(ref oldValue, value);

                _config.Channel = oldValue;
            }
        }

        private readonly Dictionary<string, string> _commands;
        public ObservableCollection<BotCommand> ChatCommands { get; set; } = new ObservableCollection<BotCommand>();

        public ObservableCollection<Model.ChatMessage> Messages { get; set; } = new ObservableCollection<Model.ChatMessage>();

        public ICommand ConnectCommand { get; private set; }
        public ICommand DisconnectCommand { get; private set; }

        public ICommand AddCommand { get; private set; }
        public ICommand RemoveCommand { get; private set; }

        public MainWindowViewModel()
        {
          
            Title = "MadWolf Twitch Bot Client";

            _config.SetConfig("D:/TestConfig.json");
            _uiContext = SynchronizationContext.Current;

            _commands = BotCommandService.LoadCommands("D:/Commands.json");
            foreach (var command in _commands)
            {
                var data = new BotCommand
                {
                    Command = command.Key,
                    Message = command.Value
                };

                ChatCommands.Add(data);
            }

            ConnectCommand = new RelayCommand(Connect, CanConnect);
            DisconnectCommand = new RelayCommand(Disconnect, CanDisconnect);

            AddCommand = new RelayCommand(AddChatCommand, CanAddChatCommand);

            //Connect();
        }

        private bool CanConnect()
        {
            return IsDisconnected && !String.IsNullOrWhiteSpace(_config.UserName) && !String.IsNullOrWhiteSpace(_config.Channel) && !String.IsNullOrWhiteSpace(_config.Token);
        }
        private void Connect()
        {
            var msg = new Model.ChatMessage
            {
                Message = $"Connecting to {TwitchChannel}...",
                HexColour = "#FF000000"
            };
            Messages.Add(msg);

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

            if (!_client.Connect())
                System.Windows.MessageBox.Show("Connection Failed");
        }

        private bool CanDisconnect()
        {
            return !IsDisconnected;
        }
        private void Disconnect()
        {
            _client.Disconnect();
        }

        private void OnBotConnected(object sender, OnConnectedArgs e)
        {
            Title = $"[CONNECTED] {Title}";
            IsDisconnected = false;

            var msg = new Model.ChatMessage
            {
                Message = "Connected!",
                HexColour = "#FF000000"
            };
            _uiContext.Send(x => Messages.Add(msg), null);
        }
        private void OnBotDisconnected(object sender, TwitchLib.Communication.Events.OnDisconnectedEventArgs e)
        {
            Title = Title.Replace("[CONNECTED] ", "");
            IsDisconnected = true;

            var msg = new Model.ChatMessage
            {
                Message = $"Disconnected from {TwitchChannel}",
                HexColour = "#FF000000"
            };
            _uiContext.Send(x => Messages.Add(msg), null);
        }

        private void OnChannelJoined(object sender, OnJoinedChannelArgs e)
        {
            _client.SendMessage(e.Channel, "/me " + BotCommandService.GetConnectionMessage());
        }

        private void OnReceivedMessage(object sender, OnMessageReceivedArgs e) 
        {
            var msg = new Model.ChatMessage
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
                    _client.SendMessage(_client.JoinedChannels.FirstOrDefault(), "/me Uses Final Heaven");
                    Thread.Sleep(4200);
                    _client.SendMessage(_client.JoinedChannels.FirstOrDefault(), $"Critical direct hit! {msg.DisplayName} takes 731858 damage.");
                    break;
                case "!uptime":
                    break;
                case "!hello":
                case "!hi":
                case "!yo":
                    _client.SendReply(_client.JoinedChannels.FirstOrDefault(), e.ChatMessage.Id, $"/me Nods at {msg.DisplayName}");
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
            ChatCommands.Add(new BotCommand());
        }
    }
}
