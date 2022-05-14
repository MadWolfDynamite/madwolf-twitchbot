using MadWolfTwitchBot.Models;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MadWolfTwitchBot.Domain
{
    public class ChannelRepository : BaseRepository
    {
        public ChannelRepository(string dbPath) : base(dbPath)
        {
            m_tableName = "channel";
        }

        public async Task<Channel> CreateNewChannel(long id, string username, string displayName)
        {
            var channelData = new Channel()
            {
                Id = id,

                Username = username,
                DisplayName = displayName
            };

            var query = @$"INSERT INTO {m_tableName} (username, display_name)
VALUES (@Username, @DisplayName)";

            return await Save(channelData, query);
        }

        public async Task<Channel> SaveChannelDetails(long id, string username, string displayName)
        {
            var channelData = new Channel()
            {
                Id = id,

                Username = username,
                DisplayName = displayName
            };

            var query = @$"UPDATE 
    {m_tableName}
SET
    username        = @Username,
    display_name    = @DisplayName
WHERE
    id = @Id";

            return await Save(channelData, query);
        }

        private async Task<Channel> Save(Channel data, string query) 
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

                    var updatedRows = cmd.ExecuteNonQuery();
                    if (updatedRows > 1)
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
                ? await ListAll<Channel>()
                : new List<Channel>();

            return result.FirstOrDefault(c => c.Username.Equals(data.Username));
        }
    }
}
