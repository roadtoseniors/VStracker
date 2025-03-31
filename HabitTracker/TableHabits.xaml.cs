using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Collections.ObjectModel;

namespace HabitTracker
{
    public partial class TableHabits : Window
    {
        private readonly DatabaseService _dbService;
        private readonly string _username;

        public ObservableCollection<Habit> Habits { get; set; }

        public TableHabits(string username)
        {
            InitializeComponent();

            _username = username;
            string connectionString = "Host=localhost;Username=postgres;Password=postgres;Database=HabitTracker";
            _dbService = new DatabaseService(connectionString);

            Habits = new ObservableCollection<Habit>();
            lstHabits.ItemsSource = Habits;

            LoadHabits();
        }

        private void LoadHabits()
        {
            int? userId = _dbService.GetUserId(_username);

            if (userId.HasValue)
            {
                // Получаем привычки для текущего пользователя
                var habits = _dbService.GetHabits(userId.Value);
                foreach (var habit in habits)
                {
                    // Загружаем логи для привычки
                    habit.Logs = _dbService.GetHabitLogs(habit.Id);

                    // Устанавливаем последний лог как текущие данные
                    if (habit.Logs.Any())
                    {
                        var lastLog = habit.Logs.First();
                        habit.Date = lastLog.Date;
                        habit.Status = lastLog.Status;
                    }
                    else
                    {
                        // Если логов нет, устанавливаем текущую дату и статус по умолчанию
                        habit.Date = DateTime.Now;
                        habit.Status = "Not Started";
                    }

                    Habits.Add(habit);
                }
            }
            else
            {
                MessageBox.Show("Пользователь не найден.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnAddHabit_Click(object sender, RoutedEventArgs e)
        {
            string habitName = txtNewHabit.Text.Trim();
            string habitDescription = txtHabitDescription.Text.Trim();

            if (!string.IsNullOrEmpty(habitName))
            {
                int? userId = _dbService.GetUserId(_username);

                if (userId.HasValue)
                {
                    int habitId = _dbService.AddHabit(userId.Value, habitName, habitDescription);

                    var newHabit = new Habit
                    {
                        Id = habitId,
                        UserId = userId.Value,
                        Name = habitName,
                        Description = habitDescription,
                        Date = DateTime.Now,
                        Status = "Not Started"
                    };

                    Habits.Add(newHabit);

                    txtNewHabit.Clear();
                    txtHabitDescription.Clear();
                }
                else
                {
                    MessageBox.Show("Пользователь не найден.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void btnSaveHabit_Click(object sender, RoutedEventArgs e)
        {
            var selectedHabit = lstHabits.SelectedItem as Habit;
            if (selectedHabit != null)
            {
                try
                {
                    DateTime date = selectedHabit.Date;
                    string status = selectedHabit.Status;

                    _dbService.AddHabitLog(selectedHabit.Id, date, status);

                    MessageBox.Show("Лог привычки успешно сохранен.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Пожалуйста, выберите привычку для сохранения лога.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnExit_Click(object sender, RoutedEventArgs e)
        {
            MainWindow mainWindow = new MainWindow();
            mainWindow.Show();
            this.Close();
        }

        private void btnDeleteHabit_Click(object sender, RoutedEventArgs e)
        {
            var selectedHabit = lstHabits.SelectedItem as Habit;
            if (selectedHabit != null)
            {
                _dbService.DeleteHabit(selectedHabit.Id);
                Habits.Remove(selectedHabit);
            }
            else
            {
                MessageBox.Show("Пожалуйста, выберите привычку для удаления.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    public class Habit
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime Date { get; set; }
        public string Status { get; set; }
        public List<HabitLog> Logs { get; set; } = new List<HabitLog>();
    }
    
    //интересная ситуация
    public class HabitLog
    {
        public int Id { get; set; }
        public int HabitId { get; set; }
        public DateTime Date { get; set; }
        public string Status { get; set; }
    }
}

