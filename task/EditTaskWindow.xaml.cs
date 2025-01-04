using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Windows;

namespace Task
{
    public partial class EditTaskWindow : Window
    {
        private readonly int _taskId;

        private List<string> _statuses;

        public EditTaskWindow(int taskId, string title, string description, DateTime? dueDate, string status, List<string> statuses)
        {
            InitializeComponent();

            _taskId = taskId; // תיקון העברת המזהה
            _statuses = statuses;

            TaskTitleTextBox.Text = title;
            TaskDescriptionTextBox.Text = description;
            TaskDueDatePicker.SelectedDate = dueDate;

            // עדכון תיבת הבחירה
            TaskStatusComboBox.Items.Clear(); // מחיקה בטוחה של פריטים קיימים
            foreach (var s in _statuses)
            {
                TaskStatusComboBox.Items.Add(s);
            }

            TaskStatusComboBox.SelectedItem = status; // בחירת הסטטוס הנוכחי
        }




        private void SaveChangesButton_Click(object sender, RoutedEventArgs e)
        {
            string title = TaskTitleTextBox.Text;
            string description = TaskDescriptionTextBox.Text;
            string status = TaskStatusComboBox.SelectedItem?.ToString();

            if (string.IsNullOrWhiteSpace(title))
            {
                MessageBox.Show("Title is required!");
                return;
            }

            try
            {
                string connectionString = "Server=sara;Database=TaskManagerDB;Trusted_Connection=True;";
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string query = "UPDATE Tasks SET Title = @Title, Description = @Description, Status = @Status WHERE Id = @Id";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Id", _taskId);
                        command.Parameters.AddWithValue("@Title", title);
                        command.Parameters.AddWithValue("@Description", string.IsNullOrEmpty(description) ? DBNull.Value : (object)description);
                        command.Parameters.AddWithValue("@Status", status);

                        command.ExecuteNonQuery();
                    }
                }

                MessageBox.Show("Task updated successfully!");
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
            }
        }
    }
}
