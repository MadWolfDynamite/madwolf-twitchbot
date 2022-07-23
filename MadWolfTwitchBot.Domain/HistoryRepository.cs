using System.Collections.Generic;
using System.Threading.Tasks;

namespace MadWolfTwitchBot.Domain
{
    public class HistoryRepository : BaseRepository
    {
        public HistoryRepository(string dbPath) : base(dbPath)
        {
            m_tableName = "bot_history";
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
    }
}
