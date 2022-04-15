using MadWolfTwitchBot.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MadWolfTwitchBot.Domain
{
    public class HistoryRepository : BaseRepository
    {
        public HistoryRepository(string dbPath) : base(dbPath)
        {
            m_tableName = "bot_history";
        }

        public async Task<IEnumerable<BotHistory>> GetBotHistory(long id)
        {
            var sql = "SELECT * FROM vw_botHistory WHERE bot_id = @BotId";
            var param = new Dictionary<string, object>()
            {
                {"@BotId", id},
            };

            return await ExecuteReaderAsync<BotHistory>(sql, param);
        }
    }
}
