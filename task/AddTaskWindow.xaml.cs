using System;
using System.Data.SqlClient;
using System.Windows;

namespace Task
{
    public partial class AddTaskWindow : Window
    {
        public AddTaskWindow()
        {
            InitializeComponent();
        }

        private void SaveTaskButton_Click(object sender, RoutedEventArgs e)
        {
            string title = TaskTitleTextBox.Text;
            string description = TaskDescriptionTextBox.Text;
            string dueDate = TaskDueDatePicker.SelectedDate.HasValue ? TaskDueDatePicker.SelectedDate.Value.ToString("yyyy-MM-dd") : null;
            bool isRecurring = IsRecurringCheckBox.IsChecked ?? false; // קבלת הערך של CheckBox

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

                MessageBox.Show("Task saved successfully!");
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
            }
        }



    }
}
