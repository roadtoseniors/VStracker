using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Npgsql;

namespace HabitTracker  
{
    public class DatabaseService
    {
        private readonly string _connectionString;

        public DatabaseService(string connectionString)
        {
            _connectionString = connectionString;
        }

        // метод для запросов
        private void ExecuteNonQuery(string sql)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand(sql, conn))
                {
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public bool HabitExists(int habitId)
        {
            string sql = $"SELECT COUNT(*) FROM habits WHERE id = {habitId}";
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand(sql, conn))
                {
                    int count = Convert.ToInt32(cmd.ExecuteScalar());
                    return count > 0;
                }
            }
        }

        // получаем из базы
        private List<T> ExecuteQuery<T>(string sql, Func<NpgsqlDataReader, T> mapper)
        {
            var result = new List<T>();
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand(sql, conn))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            result.Add(mapper(reader));
                        }
                    }
                }
            }
            return result;
        }
        public void RegisterUser(string username, string password)
        {
            string sql = $"INSERT INTO users (name, goal) VALUES ('{username}', '{password}')";
            ExecuteNonQuery(sql);
        }

        public bool LoginUser(string username, string password)
        {
            string sql = $"SELECT * FROM users WHERE name = '{username}' AND goal = '{password}'";
            var users = ExecuteQuery(sql, reader => new
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                Goal = reader.GetString(2)
            });

            return users.Count > 0;
        }

        public int? GetUserId(string username)
        {
            string sql = $"SELECT id FROM users WHERE name = '{username}'";
            var userIds = ExecuteQuery(sql, reader => reader.GetInt32(0));
            return userIds.FirstOrDefault();
        }

        public List<Habit> GetHabits(int userId)
        {
            string sql = $"SELECT * FROM habits WHERE user_id = {userId}";
            return ExecuteQuery(sql, reader => new Habit
            {
                Id = reader.GetInt32(0),
                UserId = reader.GetInt32(1),
                Name = reader.GetString(2),
                Description = reader.GetString(3)
            });
        }

        public int AddHabit(int userId, string name, string description)
        {
            string sql = $@"
                INSERT INTO habits (user_id, name, description)
                VALUES ({userId}, '{name}', '{description}')
                RETURNING id;"; // Возвращаем сгенерированный id

            using (var conn = new NpgsqlConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand(sql, conn))
                {
                    // Выполняем запрос и получаем сгенерированный id
                    return Convert.ToInt32(cmd.ExecuteScalar());
                }
            }
        }

        //интересная ситуация
        public void AddHabitLog(int habitId, DateTime date, string status)
        {
            if (!HabitExists(habitId))
            {
                throw new Exception($"Привычка с ID {habitId} не существует.");
            }

            string sql = $"INSERT INTO habit_logs (habit_id, date, status) VALUES ({habitId}, '{date:yyyy-MM-dd}', '{status}')";
            ExecuteNonQuery(sql);
        }

        public List<HabitLog> GetHabitLogs(int habitId)
        {
            string sql = $"SELECT * FROM habit_logs WHERE habit_id = {habitId} ORDER BY date DESC";
            return ExecuteQuery(sql, reader => new HabitLog
            {
                Id = reader.GetInt32(0),
                HabitId = reader.GetInt32(1),
                Date = reader.GetDateTime(2),
                Status = reader.GetString(3)
            });
        }


        public void DeleteHabit(int habitId)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                conn.Open();

                // Удаляем записи из habit_logs
                using (var cmdDeleteLogs = new NpgsqlCommand("DELETE FROM habit_logs WHERE habit_id = @id", conn))
                {
                    cmdDeleteLogs.Parameters.AddWithValue("id", habitId);
                    cmdDeleteLogs.ExecuteNonQuery();
                }

                // Удаляем запись из habits
                using (var cmdDeleteHabit = new NpgsqlCommand("DELETE FROM habits WHERE id = @id", conn))
                {
                    cmdDeleteHabit.Parameters.AddWithValue("id", habitId);
                    cmdDeleteHabit.ExecuteNonQuery();
                }
            }
        }
    }
}
