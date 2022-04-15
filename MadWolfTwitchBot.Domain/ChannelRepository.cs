using System;
using System.Collections.Generic;
using System.Text;

namespace MadWolfTwitchBot.Domain
{
    public class ChannelRepository : BaseRepository
    {
        public ChannelRepository(string dbPath) : base(dbPath)
        {
            m_tableName = "channel";
        }
    }
}
