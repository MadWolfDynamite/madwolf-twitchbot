using MadWolfTwitchBot.Models;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MadWolfTwitchBot.Domain
{
    public class PromoRepository : BaseRepository
    {
        public PromoRepository(string dbPath) : base(dbPath) 
        {
            m_tableName = "bot_promo";
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

        public async Task<BotPromo> CreateNewBotPromo(long id, long botId, string message)
        {
            var promoData = new BotPromo
            {
                Id = id,
                BotId = botId,

                ResponseMessage = message
            };

            var query = $@"INSERT INTO {m_tableName} (bot_id, response_message)
VALUES (@BotId, @Message)";

            return await Save(promoData, query);
        }

        public async Task<BotPromo> UpdateBotPromo(long id, long botId, string message)
        {
            var promoData = new BotPromo
            {
                Id = id,
                BotId = botId,

                ResponseMessage = message
            };

            var query = $@"UPDATE 
    {m_tableName}
SET
    bot_id           = @BotId,
    response_message = @Message
WHERE
    id = @Id";

            return await Save(promoData, query);
        }

        private async Task<BotPromo> Save(BotPromo data, string query)
        {
            var isSuccessful = true;

            await m_dbConnection.OpenAsync();

            using (var transaction = await m_dbConnection.BeginTransactionAsync())
            {
                try
                {
                    using var cmd = new SqliteCommand(query, m_dbConnection, (SqliteTransaction)transaction);

                    cmd.Parameters.Add(new SqliteParameter("@Id", SqliteType.Integer)).Value = data.Id;
                    cmd.Parameters.Add(new SqliteParameter("@BotId", SqliteType.Integer)).Value = data.BotId;
                    cmd.Parameters.Add(new SqliteParameter("@Message", SqliteType.Text)).Value = data.ResponseMessage;

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
                ? await ListAll<BotPromo>()
                : new List<BotPromo>();

            return result.FirstOrDefault(p => p.ResponseMessage.Equals(data.ResponseMessage));
        }
    }
}
