using MadWolfTwitchBot.Models;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MadWolfTwitchBot.Domain
{
    public class CommandRepository : BaseRepository
    {
        public CommandRepository(string dbPath) : base(dbPath)
        {
            m_tableName = "command";
        }
        public async Task<IEnumerable<T>> GetByBotId<T>(long id) where T : class, new()
        {
            var sql = $"SELECT * FROM {m_tableName} WHERE bot_id = @BotId";
            var param = new Dictionary<string, object>()
            {
                {"@BotId", id},
            };

            return await ExecuteReaderAsync<T>(sql, param);
        }

        public async Task<Command> CreateNewCommand(long id, string name, string message, long? botId) 
        {
            var commandData = new Command()
            {
                Id = id,

                Name = name,
                ResponseMessage = message,

                BotId = botId
            };

            var query = $@"INSERT INTO {m_tableName} (name, response_message, bot_id)
VALUES (@Name, @Message, @BotId)";

            return await Save(commandData, query);
        }

        public async Task<Command> UpdateCommand(long id, string name, string message, long? botId) 
        {
            var commandData = new Command()
            {
                Id = id,

                Name = name,
                ResponseMessage = message,

                BotId = botId
            };

            var query = $@"UPDATE 
    {m_tableName}
SET
    name             = @Name,
    response_message = @Message,
    bot_id           = @BotId
WHERE
    id = @Id";

            return await Save(commandData, query);
        }

        private async Task<Command> Save(Command data, string query)
        {
            var isSuccessful = true;

            await m_dbConnection.OpenAsync();

            using (var transaction = await m_dbConnection.BeginTransactionAsync())
            {
                try
                {
                    using var cmd = new SqliteCommand(query, m_dbConnection, (SqliteTransaction)transaction);
                    cmd.Parameters.Add(new SqliteParameter("@Id", SqliteType.Integer)).Value = data.Id;

                    cmd.Parameters.Add(new SqliteParameter("@Name", SqliteType.Text)).Value = data.Name;
                    cmd.Parameters.Add(new SqliteParameter("@Message", SqliteType.Text)).Value = data.ResponseMessage;

                    cmd.Parameters.Add(new SqliteParameter("@BotId", SqliteType.Integer)).Value = data.BotId != null ? data.BotId : DBNull.Value;

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
                ? await ListAll<Command>()
                : new List<Command>();

            return result.FirstOrDefault(b => b.Name.Equals(data.Name));
        }
    }
}
