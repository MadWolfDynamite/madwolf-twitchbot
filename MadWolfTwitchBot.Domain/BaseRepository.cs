using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using MadWolfTools.Common.AttributeExtension;
using System.Threading.Tasks;
using System.Linq;

namespace MadWolfTwitchBot.Domain
{
    public abstract class BaseRepository
    {
        protected readonly SqliteConnection m_dbConnection;
        protected string m_tableName;

        protected BaseRepository(string dbPath)
        {
            m_dbConnection = new SqliteConnection($"Data Source={dbPath}");
        }

        public async Task<IEnumerable<T>> ListAll<T>() where T : class, new()
        {
            var sql = $"SELECT * FROM {m_tableName}";
            return await ExecuteReaderAsync<T>(sql);
        }

        public async Task<T> GetById<T>(long id) where T : class, new()
        {
            var sql = $"SELECT * FROM {m_tableName} WHERE id = @Id";
            var param = new Dictionary<string, object>()
            {
                {"@Id", id},
            };

            var result = await ExecuteReaderAsync<T>(sql, param);
            return result.FirstOrDefault();
        }

        public async Task<bool> DeleteById(long id)
        {
            return await Delete(id);
        }
        private async Task<bool> Delete(long id)
        {
            var isSuccessful = true;

            var query = $"DELETE FROM {m_tableName} WHERE id = @Id";
            await m_dbConnection.OpenAsync();

            using (var transaction = await m_dbConnection.BeginTransactionAsync())
            {
                try
                {
                    using var cmd = new SqliteCommand(query, m_dbConnection, (SqliteTransaction)transaction);
                    cmd.Parameters.AddWithValue("@Id", id);

                    var afftectedRows = cmd.ExecuteNonQuery();
                    if (afftectedRows < 1)
                        throw new InvalidOperationException($"Id {id} not found in {m_tableName}. May already be deleted.");

                    transaction.Commit();
                }
                catch
                {
                    isSuccessful = false;
                    transaction.Rollback();
                }
            }

            await m_dbConnection.CloseAsync();

            return isSuccessful;
        }

        protected async Task<IEnumerable<T>> ExecuteReaderAsync<T>(string query, IDictionary<string, object> args = null) where T : class, new()
        {
            var result = new List<T>();

            await m_dbConnection.OpenAsync();

            using (var cmd = new SqliteCommand(query, m_dbConnection))
            {
                if (args != null)
                {
                    foreach (var parameter in args)
                        cmd.Parameters.AddWithValue(parameter.Key, parameter.Value);
                }

                using var reader = await cmd.ExecuteReaderAsync();
                while (reader.Read())
                {
                    T obj = new();
                    foreach (var prop in obj.GetType().GetProperties())
                    {
                        var columnAttribute = (DbColumnAttribute)Attribute.GetCustomAttribute(prop, typeof(DbColumnAttribute));

                        string columnName;
                        if (columnAttribute != null)
                            columnName = string.IsNullOrWhiteSpace(columnAttribute.Name) ? prop.Name : columnAttribute.Name;
                        else
                        {
                            columnAttribute = new DbColumnAttribute();
                            columnName = prop.Name;
                        }

                        if (reader[columnName] != DBNull.Value)
                            prop.SetValue(obj, columnAttribute.Convert ? Convert.ChangeType(reader[columnName], Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType) : reader[columnName]);
                    }

                    result.Add(obj);
                }
            }

            await m_dbConnection.CloseAsync();

            return result;
        }
    }
}
