using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace AssistantAi.Classes
{
    public class ConversationEntry
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string DisplayText => $"{Name}  ({CreatedAt:yyyy-MM-dd HH:mm})";
    }

    public class MessageEntry
    {
        public int Id { get; set; }
        public int ConversationId { get; set; }
        public string Role { get; set; } = "";
        public string Content { get; set; } = "";
        public DateTime Timestamp { get; set; }
        public string? Model { get; set; }
    }

    public class ConversationDatabase
    {
        private readonly string _dbPath;

        public ConversationDatabase(string dbPath)
        {
            _dbPath = dbPath;
            string? dir = Path.GetDirectoryName(dbPath);
            if (dir != null)
                Directory.CreateDirectory(dir);
            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            using var conn = new SqliteConnection($"Data Source={_dbPath}");
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS Conversations (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name TEXT NOT NULL,
                    CreatedAt TEXT NOT NULL,
                    UpdatedAt TEXT NOT NULL
                );
                CREATE TABLE IF NOT EXISTS Messages (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    ConversationId INTEGER NOT NULL,
                    Role TEXT NOT NULL,
                    Content TEXT NOT NULL,
                    Timestamp TEXT NOT NULL,
                    Model TEXT,
                    FOREIGN KEY (ConversationId) REFERENCES Conversations(Id)
                );";
            cmd.ExecuteNonQuery();
        }

        public async Task<int> CreateConversationAsync(string name)
        {
            using var conn = new SqliteConnection($"Data Source={_dbPath}");
            await conn.OpenAsync();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO Conversations (Name, CreatedAt, UpdatedAt)
                VALUES ($name, $created, $updated);
                SELECT last_insert_rowid();";
            cmd.Parameters.AddWithValue("$name", name);
            string now = DateTime.UtcNow.ToString("O");
            cmd.Parameters.AddWithValue("$created", now);
            cmd.Parameters.AddWithValue("$updated", now);
            var id = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(id);
        }

        public async Task AddMessageAsync(int conversationId, string role, string content, string? model = null)
        {
            using var conn = new SqliteConnection($"Data Source={_dbPath}");
            await conn.OpenAsync();
            string now = DateTime.UtcNow.ToString("O");
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO Messages (ConversationId, Role, Content, Timestamp, Model)
                VALUES ($convId, $role, $content, $timestamp, $model);
                UPDATE Conversations SET UpdatedAt = $timestamp WHERE Id = $convId;";
            cmd.Parameters.AddWithValue("$convId", conversationId);
            cmd.Parameters.AddWithValue("$role", role);
            cmd.Parameters.AddWithValue("$content", content);
            cmd.Parameters.AddWithValue("$timestamp", now);
            cmd.Parameters.AddWithValue("$model", (object?)model ?? DBNull.Value);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<List<ConversationEntry>> GetConversationsAsync()
        {
            var list = new List<ConversationEntry>();
            using var conn = new SqliteConnection($"Data Source={_dbPath}");
            await conn.OpenAsync();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id, Name, CreatedAt, UpdatedAt FROM Conversations ORDER BY UpdatedAt DESC";
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new ConversationEntry
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    CreatedAt = DateTime.Parse(reader.GetString(2)),
                    UpdatedAt = DateTime.Parse(reader.GetString(3))
                });
            }
            return list;
        }

        public async Task<List<MessageEntry>> GetMessagesAsync(int conversationId)
        {
            var list = new List<MessageEntry>();
            using var conn = new SqliteConnection($"Data Source={_dbPath}");
            await conn.OpenAsync();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id, ConversationId, Role, Content, Timestamp, Model FROM Messages WHERE ConversationId = $convId ORDER BY Timestamp ASC";
            cmd.Parameters.AddWithValue("$convId", conversationId);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new MessageEntry
                {
                    Id = reader.GetInt32(0),
                    ConversationId = reader.GetInt32(1),
                    Role = reader.GetString(2),
                    Content = reader.GetString(3),
                    Timestamp = DateTime.Parse(reader.GetString(4)),
                    Model = reader.IsDBNull(5) ? null : reader.GetString(5)
                });
            }
            return list;
        }

        public async Task DeleteConversationAsync(int conversationId)
        {
            using var conn = new SqliteConnection($"Data Source={_dbPath}");
            await conn.OpenAsync();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                DELETE FROM Messages WHERE ConversationId = $convId;
                DELETE FROM Conversations WHERE Id = $convId;";
            cmd.Parameters.AddWithValue("$convId", conversationId);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task RenameConversationAsync(int conversationId, string newName)
        {
            using var conn = new SqliteConnection($"Data Source={_dbPath}");
            await conn.OpenAsync();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE Conversations SET Name = $name WHERE Id = $convId";
            cmd.Parameters.AddWithValue("$name", newName);
            cmd.Parameters.AddWithValue("$convId", conversationId);
            await cmd.ExecuteNonQueryAsync();
        }
    }
}
