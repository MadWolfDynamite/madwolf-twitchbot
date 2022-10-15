﻿using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using MadWolfTwitchBot.BotCommands;
using MadWolfTwitchBot.Client.Constants;
using MadWolfTwitchBot.Client.Model;
using MadWolfTwitchBot.Client.View;
using MadWolfTwitchBot.Client.View.Modals;
using MadWolfTwitchBot.Services;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Models;

namespace MadWolfTwitchBot.Client.ViewModel
{
    public class MainWindowViewModel : ObservableObject
    {
        #region Private Members
        private TwitchClient _client;
        private DispatcherTimer _timer;
        private readonly SynchronizationContext _uiContext;

        private string m_windowTitle;
        private BasicStatusMessage m_status;

        private bool m_disconnected = true;

        private OAuthTokenStatus? m_tokenStatus;

        private BasicBot m_selectedBot;
        private BasicChannel m_selectedChannel;

        private string m_channel;

        private readonly Random rng = new();
        private int m_prevMessage;
        private readonly IList<BasicPromo> m_promoList = new List<BasicPromo>();
        #endregion

        #region Properties
        public string Title
        {
            get { return m_windowTitle; }
            set { SetProperty(ref m_windowTitle, value); }
        }

        public BasicStatusMessage AppState
        {
            get { return m_status; }
            set { SetProperty(ref m_status, value); }
        }
        
        public bool IsDisconnected 
        {
            get { return m_disconnected; }
            set { SetProperty(ref m_disconnected, value); }
        }

        public OAuthTokenStatus? TokenStatus
        {
            get { return m_tokenStatus; }
            set { SetProperty(ref m_tokenStatus, value); }
        }

        public ObservableCollection<BasicBot> AvailableBots { get; private set; } = new ObservableCollection<BasicBot>();

        public BasicBot SelectedBot
        {
            get { return m_selectedBot; }
            set
            {
                SetProperty(ref m_selectedBot, value);

                if (m_selectedBot != null)
                {
                    RefreshChannelData(m_selectedBot);

                    GetLocalBotCommands(m_selectedBot.Id);
                    GetBotPromoMessages(m_selectedBot.Id);
                    UpdateBotTokenStatus(); 
                }
                else { TokenStatus = null; }
            }
        }

        public ObservableCollection<BasicChannel> TwitchChannels { get; private set; } = new ObservableCollection<BasicChannel>();

        public BasicChannel SelectedChannel
        {
            get { return m_selectedChannel; }
            set { SetProperty(ref m_selectedChannel, value); }
        }

        public string ChannelSearch
        {
            get { return m_channel; }
            set
            {
                SetProperty(ref m_channel, value);
                SelectedChannel = TwitchChannels.FirstOrDefault(c => c.DisplayName.Equals(m_channel));
            }
        }

        public ObservableCollection<BasicCommand> GlobalCommands { get; set; } = new ObservableCollection<BasicCommand>();
        public ObservableCollection<BasicCommand> LocalCommands { get; set; } = new ObservableCollection<BasicCommand>();

        public ObservableCollection<Model.ChatMessage> Messages { get; set; } = new ObservableCollection<Model.ChatMessage>();
        #endregion

        #region Commands
        public ICommand AddBotCommand { get; }
        public ICommand EditBotCommand { get; }
        public ICommand DeleteBotCommand { get; }

        public ICommand ValidateChannelCommand { get; }
        public ICommand EditChannelCommand { get; }
        public ICommand DeleteChannelCommand { get; }

        public ICommand AddCommandCommand { get; }
        public ICommand EditCommandCommand { get; }
        public ICommand DeleteCommandCommand { get; }

        public ICommand GetTokenCommand { get; }
        public ICommand RefreshTokenCommand { get; }

        public ICommand ConnectCommand { get;}
        public ICommand DisconnectCommand { get; }

        public ICommand PromoMessageCommand { get; }
        #endregion

        #region Ctor
        public MainWindowViewModel()
        {
            WolfAPIService.SetApiEndpoint(ApiSettings.Endpoint);
            Title = "MadWolf Twitch Bot Client";
            AppState = new BasicStatusMessage { ColourHex = "#FF000000" };

            _timer = new()
            {
                Interval = TimeSpan.FromSeconds(380)
            };
            _timer.Tick += SendPromoMessage;

            _uiContext = SynchronizationContext.Current;

            AddBotCommand = new AsyncRelayCommand(AddNewBot);
            EditBotCommand = new AsyncRelayCommand(EditBotDetails, CanEditBotDetails);
            DeleteBotCommand = new AsyncRelayCommand(DeleteExistingBot, CanEditBotDetails);

            ValidateChannelCommand = new AsyncRelayCommand(ValidateChannel, CanValidateChannel);
            EditChannelCommand = new AsyncRelayCommand(EditChannel, CanEditChannel);
            DeleteChannelCommand = new AsyncRelayCommand(DeleteExistingChannel, CanEditChannel);

            AddCommandCommand = new AsyncRelayCommand(AddNewCommand);
            EditCommandCommand = new AsyncRelayCommand<BasicCommand>(EditCommand);
            DeleteCommandCommand = new AsyncRelayCommand(DeleteCommand, CanDeleteCommand);

            GetTokenCommand = new AsyncRelayCommand(GetOAuthToken, CanGetOAuthToken);
            RefreshTokenCommand = new AsyncRelayCommand(RefreshOAuthToken, CanRefreshToken);

            ConnectCommand = new RelayCommand(Connect, CanConnect);
            DisconnectCommand = new RelayCommand(Disconnect, CanDisconnect);

            PromoMessageCommand = new AsyncRelayCommand(ShowPromoMessages, CanShowPromoMessages);

            GetDbData();
        }
        #endregion

        #region Data Retrieval Methods
        private async Task GetDbData()
        {
            var botData = await BotService.GetAllConfiguredBots();
            foreach (var bot in botData)
                AvailableBots.Add(new BasicBot(bot));

            SetProperty(ref m_selectedBot, AvailableBots.FirstOrDefault(), nameof(SelectedBot));

            var channelData = await ChannelService.GetAllTwitchChannels();
            foreach (var channel in channelData)
                TwitchChannels.Add(new BasicChannel(channel));

            await GetGlobalBotCommands();

            if (SelectedBot != null)
            {
                SelectedChannel = TwitchChannels.FirstOrDefault(c => c.Id == SelectedBot.ChannelId);
                m_channel = SelectedChannel.DisplayName;

                await GetLocalBotCommands(SelectedBot.Id);
                await GetBotPromoMessages(SelectedBot.Id);

                TokenStatus = await GetBotTokenStatus();
            }
        }

        private async Task RefreshBotData(BasicBot selected = null)
        {
            AvailableBots.Clear();

            var botData = await BotService.GetAllConfiguredBots();
            foreach (var bot in botData)
                AvailableBots.Add(new BasicBot(bot));

            SelectedBot = selected != null 
                ? AvailableBots.FirstOrDefault(b => b.Username.Equals(selected.Username))
                : null;

            if (SelectedBot != null) 
            {
                await GetLocalBotCommands(SelectedBot.Id);
                await GetBotPromoMessages(SelectedBot.Id);
            }
                
        }
        
        private async Task RefreshChannelData(BasicBot selected = null)
        {
            TwitchChannels.Clear();

            var channelData = await ChannelService.GetAllTwitchChannels();
            foreach (var channel in channelData)
                TwitchChannels.Add(new BasicChannel(channel));

            SelectedChannel = selected != null
                ? TwitchChannels.FirstOrDefault(c => c.Id == selected.ChannelId)
                : TwitchChannels.LastOrDefault();
        }

        private async Task GetChannelHistory(long Id)
        {
            var historyData = await HistoryService.GetChannelHistoryForBot(Id);
            foreach (var history in historyData)
            {
                var channel = new BasicChannel()
                {
                    Id = history.ChannelId,

                    Username = history.ChannelUsername,
                    DisplayName = history.ChannelName
                };

                var existingChannel = TwitchChannels.FirstOrDefault(c => c.Id == channel.Id);
                if (existingChannel == null)
                    TwitchChannels.Add(channel);
            }

            if (!TwitchChannels.Any())
            {
                var channelData = await ChannelService.GetAllTwitchChannels();
                foreach (var channel in channelData)
                    TwitchChannels.Add(new BasicChannel(channel));
            }

            SelectedChannel = TwitchChannels.FirstOrDefault(c => c.Id == SelectedBot.ChannelId);
        }

        private async Task GetGlobalBotCommands()
        {
            GlobalCommands.Clear();

            var commandData = await CommandService.GetAllBotCommands();
            foreach (var command in commandData.Where(c => c.BotId == null))
                GlobalCommands.Add(new BasicCommand(command));
        }

        private async Task GetLocalBotCommands(long id)
        {
            LocalCommands.Clear();

            var commandData = await CommandService.GetCommandsForBot(id);
            foreach (var command in commandData)
                LocalCommands.Add(new BasicCommand(command));
        }

        private async Task GetBotPromoMessages(long id)
        {
            m_prevMessage = -1;
            m_promoList.Clear();

            var promoData = await PromoService.GetMessagesForBot(id);
            foreach (var promo in promoData)
                m_promoList.Add(new BasicPromo(promo));
        }
        #endregion

        #region OAuth Token Methods
        private async Task<OAuthTokenStatus> GetBotTokenStatus()
        {
            if (String.IsNullOrEmpty(SelectedBot.OAuthToken))
                return OAuthTokenStatus.None;

            var tokenAge = DateTime.UtcNow - (SelectedBot.TokenTimestamp ?? DateTime.MinValue);
            if (tokenAge.TotalMinutes >= TokenSettings.RefreshMinutes) 
            { 
                var validationResult = await WolfAPIService.ValidateOAuthToken(SelectedBot.OAuthToken);
                return validationResult ? OAuthTokenStatus.Valid : OAuthTokenStatus.NotValid;
            }
                
            return OAuthTokenStatus.Valid;
        }
        private async Task UpdateBotTokenStatus()
        {
            TokenStatus = await GetBotTokenStatus();
        }

        private bool CanGetOAuthToken()
        {
            if (SelectedBot == null)
                return false;

            return TokenStatus == OAuthTokenStatus.None;
        }
        private async Task GetOAuthToken()
        {
            var tokenRequestWindow = new OAuthTokenRetievalWindow
            {
                DataContext = new OAuthTokenRetrievalViewModel(SelectedBot.Username)
            };

            if (tokenRequestWindow.ShowDialog() == true)
            {
                var data = tokenRequestWindow.DataContext as OAuthTokenRetrievalViewModel;

                var updatedDetails = await BotService.CreateOrUpdateBot(
                    SelectedBot.Id, 
                    SelectedBot.Username, 
                    SelectedBot.DisplayName, 
                    data.OAuthToken, 
                    data.RefreshToken, 
                    data.TokenTimestamp, 
                    SelectedChannel.Id);

                if (updatedDetails != null)
                    await RefreshBotData(new BasicBot(updatedDetails));
            }
        }
        
        private bool CanRefreshToken()
        {
            if (SelectedBot == null)
                return false;

            return TokenStatus == OAuthTokenStatus.NotValid;
        }
        private async Task RefreshOAuthToken()
        {
            var newToken = await WolfAPIService.RefreshTwitchTokenAsync(ApiSettings.ClientId, ApiSettings.ClientSecret, SelectedBot.RefreshToken);
            if (newToken != null)
            {
                var updatedDetails = await BotService.CreateOrUpdateBot(
                    SelectedBot.Id, 
                    SelectedBot.Username, 
                    SelectedBot.DisplayName, 
                    newToken.Access_Token, 
                    newToken.Refresh_Token, 
                    DateTime.UtcNow, 
                    SelectedChannel.Id);

                if (updatedDetails != null)
                    await RefreshBotData(new BasicBot(updatedDetails));
            }
        }
        #endregion

        #region Bot Methods
        private async Task AddNewBot()
        {
            var windowModal = new BotDetailsWindow
            {
                DataContext = new BotDetailsViewModel()
            };

            if (windowModal.ShowDialog() == true)
            {
                var data = windowModal.DataContext as BotDetailsViewModel;
                var result = await BotService.CreateOrUpdateBot(
                    0,
                    data.Username.ToLower(),
                    data.DisplayName,
                    data.OAuthToken,
                    data.RefreshToken,
                    data.TokenTimestamp,
                    SelectedChannel.Id);

                if (result == null)
                {
                    MessageBox.Show("Oops...");
                    return;
                }

                await RefreshBotData(new BasicBot(result));
            }
        }

        private bool CanEditBotDetails()
        {
            return SelectedBot != null;
        }
        private async Task EditBotDetails()
        {
            var windowModal = new BotDetailsWindow
            {
                DataContext = new BotDetailsViewModel(SelectedBot)
            };

            if (windowModal.ShowDialog() == true)
            {
                var data = windowModal.DataContext as BotDetailsViewModel;
                var result = await BotService.CreateOrUpdateBot(
                    SelectedBot.Id, 
                    data.Username, 
                    data.DisplayName, 
                    data.OAuthToken, 
                    data.RefreshToken, 
                    data.TokenTimestamp, 
                    SelectedChannel.Id);

                if (result == null)
                {
                    AppState.Message = $"Failed to Update Bot";
                    AppState.ColourHex = "#FF800000";

                    return;
                }

                await RefreshBotData(new BasicBot(result));
            }
        }

        private async Task DeleteExistingBot()
        {
            var botToDelete = SelectedBot.DisplayName;

            var dialog = MessageBox.Show($"Remove {botToDelete} from saved list?", $"Remove Bot - {Title}", MessageBoxButton.YesNo);
            if (dialog == MessageBoxResult.Yes)
            {
                if (await BotService.DeleteBot(SelectedBot.Id))
                {
                    await RefreshBotData();

                    AppState.Message = $"{botToDelete} successfully deleted";
                    AppState.ColourHex = "#FF800000";
                }
            }
        }
        #endregion

        #region Channel Methods
        private bool CanValidateChannel()
        {
            return TokenStatus == OAuthTokenStatus.Valid && SelectedChannel == null;
        }
        private async Task ValidateChannel()
        {
            var data = await WolfAPIService.GetChannelDetails(ApiSettings.ClientId, SelectedBot.OAuthToken, ChannelSearch.ToLower());
            if (data == null)
                return;

            var channelData = await ChannelService.GetAllTwitchChannels();
            var existingChannel = channelData.FirstOrDefault(c => c.Username.Equals(data.Username));

            if (existingChannel == null)
            {
                var result = await ChannelService.CreateOrUpdateChannel(0, data.Username, data.DisplayName);
                if (result != null)
                {
                    TwitchChannels.Add(new BasicChannel(result));
                    SelectedChannel = TwitchChannels.FirstOrDefault(c => c.Id == result.Id);
                }
            }
            else
            {
                var check = TwitchChannels.FirstOrDefault(c => c.Id == existingChannel.Id);
                if (check == null)
                {
                    TwitchChannels.Add(new BasicChannel(existingChannel));
                    SelectedChannel = TwitchChannels.FirstOrDefault(c => c.Id == existingChannel.Id);
                }
            }
        }

        private bool CanEditChannel()
        {
            return SelectedChannel != null;
        }
        private async Task EditChannel()
        {
            var token = TokenStatus == OAuthTokenStatus.Valid ? SelectedBot.OAuthToken : string.Empty;

            var windowModal = new ChannelDetailsWindow
            {
                DataContext = new ChannelDetailsViewModel(SelectedChannel, token)
            };

            if (windowModal.ShowDialog() == true)
            {
                var data = windowModal.DataContext as ChannelDetailsViewModel;
                var result = await ChannelService.CreateOrUpdateChannel(SelectedChannel.Id, data.Username, data.DisplayName);

                if (result == null)
                {
                    MessageBox.Show("Oops...");
                    return;
                }

                await RefreshChannelData(SelectedBot);
            }
        }

        private async Task DeleteExistingChannel()
        {
            var channelToDelete = SelectedChannel.DisplayName;

            var dialog = MessageBox.Show($"Remove {channelToDelete} from saved list?", $"Remove Channel - {Title}", MessageBoxButton.YesNo);
            if (dialog == MessageBoxResult.Yes)
            {
                if (await ChannelService.DeleteChannel(SelectedChannel.Id))
                {
                    await RefreshChannelData();

                    AppState.Message = $"{channelToDelete} successfully deleted";
                    AppState.ColourHex = "#FF800000";
                }
            }
        }
        #endregion

        #region Command Methods
        private async Task AddNewCommand()
        {
            var windowModal = new CommandDetailsWindow
            {
                DataContext = new CommandDetailsViewModel()
            };

            if (windowModal.ShowDialog() == true)
            {
                var data = windowModal.DataContext as CommandDetailsViewModel;
                var result = await CommandService.CreateOrUpdateCommand(
                    0, 
                    data.Name, 
                    data.ResponseMessage, 
                    data.IsLocalCommand ? data.SelectedBot.Id : null);

                if (result == null)
                {
                    MessageBox.Show("Oops...");
                    return;
                }

                await GetGlobalBotCommands();
                await GetLocalBotCommands(SelectedBot.Id);
            }
        }

        private async Task EditCommand(BasicCommand command)
        {
            var commandBot = AvailableBots.FirstOrDefault(b => b.Id == command.BotId);

            var windowModal = new CommandDetailsWindow
            {
                DataContext = new CommandDetailsViewModel(
                    command.Name, 
                    command.ResponseMessage, 
                    commandBot != null ? commandBot.Id : 0)
            };

            if (windowModal.ShowDialog() == true)
            {
                var data = windowModal.DataContext as CommandDetailsViewModel;
                var result = await CommandService.CreateOrUpdateCommand(
                    command.Id, 
                    data.Name, 
                    data.ResponseMessage, 
                    data.IsLocalCommand ? data.SelectedBot.Id : null);

                if (result == null)
                {
                    MessageBox.Show("Oops...");
                    return;
                }

                await GetGlobalBotCommands();
                await GetLocalBotCommands(SelectedBot.Id);
            }
        }

        private bool CanDeleteCommand()
        {
            return GlobalCommands.Count + LocalCommands.Count > 0;
        }
        private async Task DeleteCommand()
        {
            var commandData = await CommandService.GetAllBotCommands();
            var commands = new List<BasicCommand>();

            foreach (var command in commandData.Where(c => c.BotId == null || c.BotId == SelectedBot.Id))
                commands.Add(new BasicCommand(command));

            var windowModal = new DeleteCommandWindow
            {
                DataContext = new DeleteCommandViewModel(commands)
            };

            if (windowModal.ShowDialog() == true)
            {
                var data = windowModal.DataContext as DeleteCommandViewModel;
                var commandsToDelete = data.ChatCommands.Where(c => c.IsSelected);

                var successfulDelete = 0;
                var failedDelete = 0;

                foreach (var command in commandsToDelete) 
                {
                    var result = await CommandService.DeleteCommand(command.Data.Id);

                    if (result)
                        successfulDelete++;
                    else
                        failedDelete++;
                }

                await GetGlobalBotCommands();
                await GetLocalBotCommands(SelectedBot.Id);
            }
        }
        #endregion

        #region Promotion Message Methods

        private bool CanShowPromoMessages()
        {
            return SelectedBot != null;
        }
        private async Task ShowPromoMessages()
        {
            await GetBotPromoMessages(SelectedBot.Id);

            var window = new BotPromoWindow()
            {
                DataContext = new BotPromoViewModel(SelectedBot.Id, m_promoList)
            };

            window.Show();
        }
        #endregion

        #region Promotion Message Automation

        private void SendPromoMessage(object sender, EventArgs e)
        {
            if (m_prevMessage == -1)
                m_prevMessage = rng.Next(m_promoList.Count);

            var channel = _client.JoinedChannels[0];
            _client.SendMessage(channel, $"/announce {m_promoList[m_prevMessage]}");

            int nextMessage;
            do
            {
                nextMessage = rng.Next(m_promoList.Count);
            } while (nextMessage == m_prevMessage);

            m_prevMessage = nextMessage;
        }

        #endregion

        #region TwitchLib Client Methods
        private bool CanConnect()
        {
            if (SelectedBot == null || SelectedChannel == null)
                return false;

            if (SelectedBot.Username.Equals(SelectedChannel.Username))
                return false;

            return IsDisconnected && TokenStatus == OAuthTokenStatus.Valid;
        }
        private void Connect()
        {
            Messages.Clear();

            AppState.Message = $"Connecting to {SelectedChannel.DisplayName}...";
            AppState.ColourHex = "#FF808080";

            ConnectionCredentials credentials = new(SelectedBot.Username, SelectedBot.OAuthToken);
            var clientOptions = new ClientOptions
            {
                MessagesAllowedInPeriod = 750,
                ThrottlingPeriod = TimeSpan.FromSeconds(30)
            };

            WebSocketClient customClient = new(clientOptions);
            _client = new TwitchClient(customClient);
            _client.Initialize(credentials, SelectedChannel.Username);

            _client.OnConnected += OnBotConnected;
            _client.OnDisconnected += OnBotDisconnected;
            _client.OnJoinedChannel += OnChannelJoined;
            _client.OnMessageReceived += OnReceivedMessage;

            if (!_client.Connect())
            {
                AppState.Message = $"Connection Failed";
                AppState.ColourHex = "#FF800000";
            }
        }

        private bool CanDisconnect()
        {
            return !IsDisconnected;
        }
        private void Disconnect()
        {
            _client.Disconnect();
        }
        #endregion

        #region TwitchLib Client Events
        private void OnBotConnected(object sender, OnConnectedArgs e)
        {
            _uiContext.Send(x => {
                Title = $"{SelectedChannel.DisplayName} - {Title}";
                IsDisconnected = false;

                AppState.Message = $"";
            }, null);

            var result = BotService.CreateOrUpdateBot(
                SelectedBot.Id, 
                SelectedBot.Username, 
                SelectedBot.DisplayName, 
                SelectedBot.OAuthToken, 
                SelectedBot.RefreshToken, 
                SelectedBot.TokenTimestamp, 
                SelectedChannel.Id);

            _timer.Start();
        }
        private void OnBotDisconnected(object sender, TwitchLib.Communication.Events.OnDisconnectedEventArgs e)
        {
            Title = Title.Replace($"{SelectedChannel} - ", "");
            IsDisconnected = true;

            AppState.Message = $"Disconnected from {SelectedChannel.DisplayName}";
            AppState.ColourHex = "#FF008000";

            _timer.Stop();
            m_prevMessage = -1;
        }

        private void OnChannelJoined(object sender, OnJoinedChannelArgs e)
        {
            _client.SendMessage(e.Channel, "/me " + BotCommandService.GetConnectionMessage());
        }

        private async void OnReceivedMessage(object sender, OnMessageReceivedArgs e) 
        {
            var msg = new Model.ChatMessage
            {
                DisplayName = e.ChatMessage.DisplayName,
                Message = e.ChatMessage.Message,
                HexColour = e.ChatMessage.ColorHex
            };

            if (e.ChatMessage.Username.Equals(SelectedBot.Username))
                return;

            var channel = _client.JoinedChannels[0];
            var args = msg.Message.Trim().Split(' ');

            if (!args[0].StartsWith("!"))
                return;

            var command = args[0].ToLower().Replace("!", "");

            var selectedCommand = GlobalCommands.FirstOrDefault(c => command.Equals(c.Name));
            if (selectedCommand != null)
            {
                //TODO: Variable handling (e.g. {name} display's username)
                var message = selectedCommand.ResponseMessage;

                _client.SendMessage(channel, message);
                return;
            }

            selectedCommand = LocalCommands.FirstOrDefault(c => command.Equals(c.Name));
            if (selectedCommand != null)
            {
                //TODO: Variable handling (e.g. {name} display's username)
                var message = selectedCommand.ResponseMessage;

                _client.SendMessage(channel, message);
                return;
            }

            //Special Easter Egg Command
            if (command.Equals("lb") && SelectedBot.Username.Equals("windupkagura"))
            {
                _client.SendMessage(channel, "Uses Final Heaven");
                await Task.Delay(4200);
                _client.SendMessage(channel, $"Critical direct hit! {msg.DisplayName} takes 731858 damage.");

                return;
            }

            //Built in commands
            switch (command)
            {
                case "shoutout":
                case "so":
                case "shill":
                case "plug":
                    if (!(e.ChatMessage.IsBroadcaster || e.ChatMessage.IsModerator))
                        return;

                    if (args.Length < 2)
                    {
                        _client.SendReply(channel, e.ChatMessage.Id, "There's nobody to shout out...");
                        return;
                    }

                    if (args.Contains(SelectedChannel.Username))
                    {
                        _client.SendMessage(channel, $"/me is visibly vexed at {e.ChatMessage.DisplayName}");
                        return;
                    }

                    for (int i = 1; i < args.Length; i++)
                        _client.SendMessage(channel, await CommandService.GenerateShoutoutMessage(ApiSettings.ClientId, SelectedBot.OAuthToken, args[i]));

                    break;
                case "uptime":
                    if (!(e.ChatMessage.IsBroadcaster || e.ChatMessage.IsModerator))
                        return;
                    break;
                case "hello":
                case "hi":
                case "yo":
                    _client.SendMessage(channel, $"/me Nods at {msg.DisplayName}");
                    break;
                case "o/":
                    _client.SendReply(channel, e.ChatMessage.Id, @"\o");
                    break;
                default:
                    _uiContext.Send(x => Messages.Add(msg), null);
                    break;
            }
        }
        #endregion
    }
}
