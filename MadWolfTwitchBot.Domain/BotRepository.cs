using MadWolfTwitchBot.Models;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MadWolfTwitchBot.Domain
{
    public class BotRepository : BaseRepository
    {
        public BotRepository(string dbPath) : base(dbPath)
        {
            m_tableName = "bot";
        }

        public async Task<Bot> CreateNewBot(long id, string username, string displayName, string token, string refresh, DateTime? timestamp, long? channel)
        {
            var botData = new Bot()
            {
                Id = id,

                Username = username,
                DisplayName = displayName,

                OAuthToken = token,
                RefreshToken = refresh,
                TokenTimestamp = timestamp,

                ChannelId = channel
            };

            var query = @$"INSERT INTO {m_tableName} (username, display_name, oauth_token, refresh_token, token_timestamp, channel_id)
VALUES (@Username, @DisplayName, @Token, @Refresh, @Timestamp, @Channel)";

            return await Save(botData, query);
        }

        public async Task<Bot> UpdateBot(long id, string username, string displayName, string token, string refresh, DateTime? timestamp, long? channel)
        {
            var botData = new Bot()
            {
                Id = id,

                Username = username,
                DisplayName = displayName,

                OAuthToken = token,
                RefreshToken = refresh,
                TokenTimestamp = timestamp,

                ChannelId = channel
            };

            var query = @$"UPDATE 
    {m_tableName}
SET
    username        = @Username,
    display_name    = @DisplayName,
    oauth_token     = @Token,
    refresh_token   = @Refresh,
    token_timestamp = @Timestamp,
    channel_id      = @Channel
WHERE
    id = @Id";

            return await Save(botData, query);
        }

        private async Task<Bot> Save(Bot data, string query)
        {
            var isSuccessful = true;

            await m_dbConnection.OpenAsync();

            using (var transaction = await m_dbConnection.BeginTransactionAsync())
            {
                try
                {
                    using var cmd = new SqliteCommand(query, m_dbConnection, (SqliteTransaction)transaction);
                    cmd.Parameters.AddWithValue("@Id", data.Id);

                    cmd.Parameters.AddWithValue("@Username", data.Username);
                    cmd.Parameters.AddWithValue("@DisplayName", data.DisplayName);

                    cmd.Parameters.AddWithValue("@Token", data.OAuthToken != null ? data.OAuthToken : DBNull.Value);
                    cmd.Parameters.AddWithValue("@Refresh", data.RefreshToken != null ? data.RefreshToken : DBNull.Value);
                    cmd.Parameters.AddWithValue("@Timestamp", data.TokenTimestamp != null ? ((DateTime)data.TokenTimestamp).ToString("yyyy-MM-dd HH:mm:ss") : DBNull.Value);

                    cmd.Parameters.AddWithValue("@Channel", data.ChannelId != null ? data.ChannelId : DBNull.Value);

                    var updatedRows = cmd.ExecuteNonQuery();
                    if (updatedRows > 3)
                        throw new SqliteException("Too many rows affected", 50000);

                    transaction.Commit();
                }
                catch (Exception ex) 
                {
                    isSuccessful = false;
                    transaction.Rollback(); 
                }
            }

            await m_dbConnection.CloseAsync();

            var result = isSuccessful
                ? await ListAll<Bot>()
                : new List<Bot>();

            return result.FirstOrDefault(b => b.Username.Equals(data.Username));
        }
    }
}
