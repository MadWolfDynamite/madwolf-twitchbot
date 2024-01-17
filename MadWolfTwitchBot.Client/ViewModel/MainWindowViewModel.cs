using CommunityToolkit.Mvvm.ComponentModel;
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
using System.Net;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using TwitchLib.Api;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Extensions;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Events;
using TwitchLib.Communication.Models;

namespace MadWolfTwitchBot.Client.ViewModel
{
    public class MainWindowViewModel : ObservableObject
    {
        #region Private Members

        private readonly SynchronizationContext m_uiContext;

        private TwitchClient m_client;
        private bool m_isDisconnectClientInitiated = false;

        private DispatcherTimer m_promoTimer;
        private DispatcherTimer m_oauthErrorTimer;
        private DispatcherTimer m_oauthRefreshTimer;
        

        private string m_windowTitle;
        private BasicStatusMessage m_status;

        private bool m_disconnected = true;

        private OAuthTokenStatus? m_tokenStatus;

        private BasicBot m_selectedBot;
        private BasicChannel m_selectedChannel;

        private string m_channel;

        private int m_prevMessage;
        private readonly IList<BasicPromo> m_promoList = new List<BasicPromo>();
        #endregion

        #region Properties
        public string Title
        {
            get => m_windowTitle;
            set => SetProperty(ref m_windowTitle, value);
        }

        public BasicStatusMessage AppState
        {
            get => m_status;
            set => SetProperty(ref m_status, value);
        }
        
        public bool IsDisconnected 
        {
            get => m_disconnected; 
            set => SetProperty(ref m_disconnected, value);
        }

        public OAuthTokenStatus? TokenStatus
        {
            get => m_tokenStatus; 
            set => SetProperty(ref m_tokenStatus, value);
        }

        public ObservableCollection<BasicBot> AvailableBots { get; private set; } = new ObservableCollection<BasicBot>();

        public BasicBot SelectedBot
        {
            get => m_selectedBot;
            set
            {
                SetProperty(ref m_selectedBot, value);

                if (m_selectedBot != null)
                {
                    m_uiContext.Send(async delegate {
                        await RefreshChannelData(m_selectedBot);

                        await GetLocalBotCommands(m_selectedBot.Id);
                        await GetBotPromoMessages(m_selectedBot.Id);

                        await UpdateBotTokenStatus();
                    }, null);   
                }
                else { TokenStatus = null; }
            }
        }

        public ObservableCollection<BasicChannel> TwitchChannels { get; private set; } = new ObservableCollection<BasicChannel>();

        public BasicChannel SelectedChannel
        {
            get => m_selectedChannel; 
            set => SetProperty(ref m_selectedChannel, value);
        }

        public string ChannelSearch
        {
            get => m_channel;
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
            SetupTimers();
            WolfAPIService.SetApiEndpoint(ApiSettings.Endpoint);

            Title = "MadWolf Twitch Bot Client";
            AppState = new BasicStatusMessage { ColourHex = "#FF000000" };

            m_uiContext = SynchronizationContext.Current;

            m_uiContext.Post(async delegate {
                await GetDbData();
            }, null);

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
        }

        #endregion

        private void SetupTimers()
        {
            m_promoTimer = new() { Interval = TimeSpan.FromMinutes(45) };
            m_promoTimer.Tick += SendPromoMessage;

            m_oauthErrorTimer = new() { Interval = TimeSpan.FromSeconds(5) };
            m_oauthErrorTimer.Tick += RetryFailedTokenStatus;

            m_oauthRefreshTimer = new() { Interval = TimeSpan.FromMinutes(TokenSettings.RefreshMinutes) };
            m_oauthRefreshTimer.Tick += CheckTokenStatus;
        }

        private void SetupTwitchClient()
        {
            var clientOptions = new ClientOptions
            {
                MessagesAllowedInPeriod = 750,
                ThrottlingPeriod = TimeSpan.FromSeconds(30)
            };

            m_client = new TwitchClient(new WebSocketClient(clientOptions));
            m_client.Initialize(new ConnectionCredentials(SelectedBot.Username, SelectedBot.OAuthToken));

            m_client.OnConnected += OnBotConnected;
            m_client.OnDisconnected += OnBotDisconnected;

            m_client.OnJoinedChannel += OnChannelJoined;
            m_client.OnLeftChannel += OnChannelLeft;

            m_client.OnChatCommandReceived += OnReceivedCommand;
            m_client.OnMessageReceived += OnReceivedMessage;
        }

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

                await UpdateBotTokenStatus();
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
        private async Task<OAuthTokenStatus> GetBotTokenStatus(bool isRetry)
        {
            AppState.Message = isRetry ? "Retrying OAuth Token Verification..." : "Verifying OAuth Token...";
            AppState.ColourHex = "#FF808080";

            if (String.IsNullOrEmpty(SelectedBot.OAuthToken))
                return OAuthTokenStatus.None;

            try
            {
                var tokenAge = DateTime.UtcNow - (SelectedBot.TokenTimestamp ?? DateTime.MinValue);
                if (tokenAge.TotalMinutes >= TokenSettings.RefreshMinutes)
                {
                    var validationResult = await WolfAPIService.ValidateOAuthToken(SelectedBot.OAuthToken);
                    return validationResult ? OAuthTokenStatus.Valid : OAuthTokenStatus.NotValid;
                }
            }
            catch (Exception ex)
            {
                AppState.Message = $"{ex.Message}";
                AppState.ColourHex = "#FF800000";

                m_oauthErrorTimer.Start();

                return OAuthTokenStatus.Error;
            }
                
            return OAuthTokenStatus.Valid;
        }

        private async Task UpdateBotTokenStatus(bool isRetry = false)
        {
            TokenStatus = await GetBotTokenStatus(isRetry);

            AppState.Message = TokenStatus == OAuthTokenStatus.Error
                ? AppState.Message
                : string.Empty;

            if (TokenStatus == OAuthTokenStatus.Valid && m_client == null)
                SetupTwitchClient();
        }

        private async void RetryFailedTokenStatus(object sender, EventArgs e) 
        {
            var timer = sender as DispatcherTimer;

            await UpdateBotTokenStatus(true);

            if (TokenStatus != OAuthTokenStatus.Error)
                timer.Stop();
        }

        private async void CheckTokenStatus(object sender, EventArgs e)
        {
            await UpdateBotTokenStatus();

            if (TokenStatus == OAuthTokenStatus.NotValid)
                await RefreshOAuthToken();
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
                    SelectedChannel?.Id);

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

        private async void SendPromoMessage(object sender, EventArgs e)
        {
            CheckTokenStatus(sender, e);

            var minIndex = 0;
            var maxIndex = m_promoList.Count;

            var rng = RandomNumberGenerator.GetInt32(minIndex, maxIndex);

            if (m_prevMessage == -1)
                m_prevMessage= rng;

            var channel = m_client.JoinedChannels[0];
            await WolfAPIService.AnnouncePromoMessage(ApiSettings.ClientId, SelectedBot.OAuthToken, channel.Channel, $"{m_promoList[m_prevMessage]}");

            int nextMessage;
            do
            {
                rng = RandomNumberGenerator.GetInt32(minIndex, maxIndex);
                nextMessage = rng;
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
            IsDisconnected = false;
            Messages.Clear();

            AppState.Message = $"Connecting to {SelectedChannel.DisplayName}...";
            AppState.ColourHex = "#FF808080";
            
            m_client.SetConnectionCredentials(new ConnectionCredentials(SelectedBot.Username, SelectedBot.OAuthToken));
                
            if (!m_client.Connect())
            {
                AppState.Message = $"Connection Failed";
                AppState.ColourHex = "#FF800000";

                IsDisconnected = true;
            }
        }

        private bool CanDisconnect()
        {
            return !IsDisconnected;
        }

        private void Disconnect()
        {
            if (m_client.JoinedChannels.Count > 0)
                m_client.LeaveChannel(m_client.JoinedChannels[0]);
        }

        #endregion

        #region TwitchLib Client Events

        private void OnBotConnected(object sender, OnConnectedArgs e)
        {
            if (m_isDisconnectClientInitiated) return;

            m_client.JoinChannel(SelectedChannel.Username);
        }

        private void OnBotDisconnected(object sender, OnDisconnectedEventArgs e)
        {
            if (m_isDisconnectClientInitiated)
            {
                m_uiContext.Send(delegate {
                    Title = Title.Replace($"[{SelectedChannel.DisplayName}] ", string.Empty);
                    IsDisconnected = true;

                    m_isDisconnectClientInitiated = false;
                }, null);

                m_prevMessage = -1;
            }
            else
            {
                AppState.Message = $"Attempting to reconnect to {SelectedChannel.DisplayName}...";
                AppState.ColourHex = "#FF808080";

                m_client.Reconnect();
            }
        }

        private void OnChannelLeft(object sender, OnLeftChannelArgs e)
        {
            m_isDisconnectClientInitiated = true;

            m_promoTimer.Stop();
            m_oauthRefreshTimer.Stop();

            m_client.Disconnect();
        }

        private void OnChannelJoined(object sender, OnJoinedChannelArgs e)
        {
            m_uiContext.Send(async delegate {
                Title = $"[{SelectedChannel.DisplayName}] {Title}";

                AppState.Message = string.Empty;

                var result = await BotService.CreateOrUpdateBot(
                    SelectedBot.Id,
                    SelectedBot.Username,
                    SelectedBot.DisplayName,
                    SelectedBot.OAuthToken,
                    SelectedBot.RefreshToken,
                    SelectedBot.TokenTimestamp,
                    SelectedChannel.Id);

                m_isDisconnectClientInitiated = false;
            }, null);

            m_promoTimer.Start();
            m_oauthRefreshTimer.Start();

            m_client.SendMessage(e.Channel, "/me " + BotCommandService.GetConnectionMessage());
        }

        private void OnReceivedMessage(object sender, OnMessageReceivedArgs e) 
        {
            var msg = new Model.ChatMessage
            {
                DisplayName = e.ChatMessage.DisplayName,
                Message = e.ChatMessage.Message,
                HexColour = e.ChatMessage.ColorHex
            };

            if (e.ChatMessage.Username.Equals(SelectedBot.Username) || msg.Message.StartsWith("!"))
                return;

            m_uiContext.Post(delegate { Messages.Add(msg); }, null);
        }

        private async void OnReceivedCommand(object sender, OnChatCommandReceivedArgs e)
        {
            var baseMessage = e.Command.ChatMessage;

            if (baseMessage.Username.Equals(SelectedBot.Username))
                return;

            var msg = new Model.ChatMessage
            {
                DisplayName = baseMessage.DisplayName,
                Message = baseMessage.Message,
                HexColour = baseMessage.ColorHex
            };

            var channel = m_client.JoinedChannels[0];

            var command = e.Command.CommandText;
            var args = e.Command.ArgumentsAsList;

            var selectedCommand = GlobalCommands.FirstOrDefault(c => command.Equals(c.Name));
            if (selectedCommand != null)
            {
                //TODO: Variable handling (e.g. {name} display's username)
                var message = ParseSystemVariables(selectedCommand.ResponseMessage, baseMessage.DisplayName);

                m_client.SendMessage(channel, message);
                return;
            }

            selectedCommand = LocalCommands.FirstOrDefault(c => command.Equals(c.Name));
            if (selectedCommand != null)
            {
                //TODO: Variable handling (e.g. {name} display's username)
                var message = ParseSystemVariables(selectedCommand.ResponseMessage, baseMessage.DisplayName);

                m_client.SendMessage(channel, message);
                return;
            }

            //Special Easter Egg Command
            if (command.Equals("lb") && SelectedBot.Username.Equals("windupkagura"))
            {
                m_client.SendMessage(channel, "Uses Final Heaven");
                await Task.Delay(4200);
                m_client.SendMessage(channel, $"Critical direct hit! {msg.DisplayName} takes 731858 damage.");

                return;
            }

            //Built in commands
            switch (command)
            {
                case "shoutout":
                case "so":
                case "shill":
                case "plug":
                    if (!(baseMessage.IsBroadcaster || baseMessage.IsModerator))
                        return;

                    if (args.Count < 1)
                    {
                        m_client.SendReply(channel, baseMessage.Id, "There's nobody to shout out...");
                        return;
                    }

                    if (args.Contains(SelectedChannel.Username))
                    {
                        m_client.SendMessage(channel, $"/me is visibly vexed at {baseMessage.DisplayName}");
                        return;
                    }

                    var userNames = args.Distinct();

                    if (userNames.Count() > 10)
                    {
                        m_client.SendMessage(channel, $"https://www.youtube.com/watch?v=qWAbrdaWwSk");
                        return;
                    }

                    foreach (var user in userNames)
                        m_client.SendMessage(channel, await CommandService.GenerateShoutoutMessage(ApiSettings.ClientId, SelectedBot.OAuthToken, user));

                    break;
                case "uptime":
                    if (!(baseMessage.IsBroadcaster || baseMessage.IsModerator))
                        return;
                    break;
                case "hello":
                case "hi":
                case "yo":
                    m_client.SendMessage(channel, $"/me Nods at {msg.DisplayName}");
                    return;
                case "o/":
                    m_client.SendReply(channel, baseMessage.Id, @"\o");
                    return;
                default:
                    m_client.SendMessage(channel, $"Could not find a !{command} command...");
                    return;
            }
        }

        #endregion

        private string ParseSystemVariables (string message, string username)
        {
            foreach (var variable in SystemBotVariables.SystemVariables)
            {
                switch (variable)
                {
                    case "{bot}":
                        message = message.Replace(variable, SelectedBot.DisplayName); break;
                    case "{name}":
                        message = message.Replace(variable, username); break;
                    case "{channel}":
                        message = message.Replace(variable, SelectedChannel.DisplayName); break;
                }
            }

            return message;
        }
    }
}
