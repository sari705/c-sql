using System;
using System.Data.SqlClient;
using System.Windows;
using NLog;

namespace Task
{
    public partial class AddTaskWindow : Window
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public AddTaskWindow()
        {
            InitializeComponent();
        }

        private void SaveTaskButton_Click(object sender, RoutedEventArgs e)
        {
            string title = TaskTitleTextBox.Text.Trim();
            string description = TaskDescriptionTextBox.Text.Trim();
            string dueDate = TaskDueDatePicker.SelectedDate.HasValue
                ? TaskDueDatePicker.SelectedDate.Value.ToString("yyyy-MM-dd")
                : null;
            bool isRecurring = IsRecurringCheckBox.IsChecked ?? false;

            // ולידציה לקלט
            if (!ValidateInput(title, dueDate))
            {
                return;
            }

            try
            {
                SaveTaskToDatabase(title, description, dueDate, isRecurring);
                MessageBox.Show("Task saved successfully!");
                Logger.Info($"Task '{title}' saved successfully.");
                this.Close();
            }
            catch (SqlException ex)
            {
                Logger.Error(ex, "Database error while saving the task.");
                MessageBox.Show("An error occurred while saving the task. Please try again.");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Unexpected error while saving the task.");
                MessageBox.Show("An unexpected error occurred. Please contact support.");
            }
        }

        // ולידציה של הקלט
        private bool ValidateInput(string title, string dueDate)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                MessageBox.Show("Title is required and cannot be empty.");
                return false;
            }
            if (string.IsNullOrWhiteSpace(dueDate))
            {
                MessageBox.Show("Due date is required and cannot be empty.");
                return false;
            }

            if (title.Length > 50)
            {
                MessageBox.Show("Title cannot exceed 50 characters.");
                return false;
            }

            if (!string.IsNullOrEmpty(dueDate) && DateTime.Parse(dueDate) < DateTime.Now.Date)
            {
                MessageBox.Show("Due date cannot be in the past.");
                return false;
            }

            return true;
        }

        // שמירת המשימה למסד הנתונים
        private void SaveTaskToDatabase(string title, string description, string dueDate, bool isRecurring)
        {
            string connectionString = "Server=sara;Database=TaskManagerDB;Trusted_Connection=True;";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                string query = "INSERT INTO Tasks (Title, Description, DueDate, IsRecurring) VALUES (@Title, @Description, @DueDate, @IsRecurring)";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Title", title);
                    command.Parameters.AddWithValue("@Description", string.IsNullOrEmpty(description) ? DBNull.Value : (object)description);
                    command.Parameters.AddWithValue("@DueDate", string.IsNullOrEmpty(dueDate) ? DBNull.Value : (object)dueDate);
                    command.Parameters.AddWithValue("@IsRecurring", isRecurring);

                    command.ExecuteNonQuery();
                }
            }
        }
    }
}
