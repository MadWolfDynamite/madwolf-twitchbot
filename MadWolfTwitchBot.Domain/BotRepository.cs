﻿using MadWolfTwitchBot.Models;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
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

        public async Task<Bot> Save(long id, string username, string displayName, string token, DateTime timestamp, long channel)
        {
            var botData = new Bot()
            {
                Id = id,

                Username = username,
                DisplayName = displayName,

                OAuthToken = token,
                TokenTimestamp = timestamp,

                ChannelId = channel
            };

            return await Save(botData);
        }
        private async Task<Bot> Save(Bot data)
        {
            var query = @"UPDATE 
    bot
SET
    username = @Username,
    displayname = @DisplayName,
    oauth_token = @Token,
    token_timestamp = @Timestamp,
    channel_id = @Channel
WHERE
    id = @Id";

            await m_dbConnection.OpenAsync();

            using (var transaction = await m_dbConnection.BeginTransactionAsync())
            {
                try
                {
                    using var cmd = new SqliteCommand(query, m_dbConnection);
                    cmd.Parameters.AddWithValue("@Id", data.Id);

                    cmd.Parameters.AddWithValue("@Username", data.Username);
                    cmd.Parameters.AddWithValue("@DisplayName", data.DisplayName);

                    if (data.OAuthToken != null)
                        cmd.Parameters.AddWithValue("@Token", data.OAuthToken);
                    else
                        cmd.Parameters.AddWithValue("@Token", DBNull.Value);

                    if (data.TokenTimestamp != null)
                        cmd.Parameters.AddWithValue("@Timestamp", ((DateTime)data.TokenTimestamp).ToString("yyyy-MM-dd HH:mm:ss"));
                    else
                        cmd.Parameters.AddWithValue("@Timestamp", DBNull.Value);

                    if (data.ChannelId != null)
                        cmd.Parameters.AddWithValue("@Channel", data.ChannelId);
                    else
                        cmd.Parameters.AddWithValue("@Channel", DBNull.Value);

                    var updatedRows = cmd.ExecuteNonQuery();

                    if (updatedRows > 3)
                        throw new SqliteException("Too many rows affected", 50000);

                    transaction.Commit();
                }
                catch (Exception ex) { transaction.Rollback(); }
            }

            await m_dbConnection.CloseAsync();

            return new Bot();
        }
    }
}