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

        public EditTaskWindow(int taskId, string title, string description, DateTime? dueDate, string status, bool isRecurring, List<string> statuses)
        {
            InitializeComponent();

            _taskId = taskId;
            _statuses = statuses;

            TaskTitleTextBox.Text = title;
            TaskDescriptionTextBox.Text = description;
            TaskDueDatePicker.SelectedDate = dueDate;

            // עדכון ComboBox עם סטטוסים
            TaskStatusComboBox.Items.Clear();
            foreach (var s in _statuses)
            {
                TaskStatusComboBox.Items.Add(s);
            }
            TaskStatusComboBox.SelectedItem = status;

            // קביעת הערך של CheckBox
            RecurringCheckBox.IsChecked = isRecurring;
        }





        private void SaveChangesButton_Click(object sender, RoutedEventArgs e)
        {
            string title = TaskTitleTextBox.Text;
            string description = TaskDescriptionTextBox.Text;
            string status = TaskStatusComboBox.SelectedItem?.ToString();
            bool isRecurring = RecurringCheckBox.IsChecked ?? false;

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
                    string query = "UPDATE Tasks SET Title = @Title, Description = @Description, Status = @Status, IsRecurring = @IsRecurring WHERE Id = @Id";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Id", _taskId);
                        command.Parameters.AddWithValue("@Title", title);
                        command.Parameters.AddWithValue("@Description", string.IsNullOrEmpty(description) ? DBNull.Value : (object)description);
                        command.Parameters.AddWithValue("@Status", status);
                        command.Parameters.AddWithValue("@IsRecurring", isRecurring);

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
