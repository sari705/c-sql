using OfficeOpenXml;
using OfficeOpenXml.Style;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace Task
{
    public partial class ViewTasksWindow : Window
    {
        private List<string>  _statuses = new List<string> { "All", "Pending", "Completed", "In Progress" };


        public ViewTasksWindow()
        {
            InitializeComponent();

            // ביטול זמני של האירוע
            StatusFilterComboBox.SelectionChanged -= StatusFilterComboBox_SelectionChanged;

            // הוספת סטטוסים ישירות
            _statuses = new List<string> { "All", "Pending", "Completed", "In Progress" }; // הגדרת סטטוסים קשיחים
            StatusFilterComboBox.Items.Clear();
            foreach (var status in _statuses)
            {
                StatusFilterComboBox.Items.Add(status);
            }
            StatusFilterComboBox.SelectedIndex = 0; // ברירת מחדל - "All"

            // קריאה לטעינת נתונים אסינכרונית
            InitializeAsync();

            // החזרת האירוע לאחר שהכל נטען
            StatusFilterComboBox.SelectionChanged += StatusFilterComboBox_SelectionChanged;
        }

        private async System.Threading.Tasks.Task InitializeAsync()
        {
            await LoadTasksAsync(); // טוען את המשימות
        }

        private async Task<string> GetConnectionStringAsync()
        {
            return await System.Threading.Tasks.Task.FromResult(ConfigurationManager.ConnectionStrings["TaskManagerDB"].ConnectionString);
        }

        



        private async System.Threading.Tasks.Task LoadTasksAsync()
        {
            try
            {
                string connectionString = await GetConnectionStringAsync();

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    string query = "SELECT Id, Title, Description, DueDate, Status, IsRecurring FROM Tasks";
                    using (SqlDataAdapter adapter = new SqlDataAdapter(query, connection))
                    {
                        DataTable tasksTable = new DataTable();
                        adapter.Fill(tasksTable);

                        TasksDataGrid.ItemsSource = tasksTable.DefaultView;

                        // רענון הצעות חיפוש וסגנונות שורות
                        LoadSearchSuggestions(tasksTable);
                    }
                }
            }
            catch (Exception ex)
            {
                LogError(ex);
                MessageBox.Show("Error loading tasks. Please try again.");
            }
        }

        private void LoadSearchSuggestions(DataTable tasksTable)
        {
            SearchTextBox.Items.Clear();
            foreach (DataRow row in tasksTable.Rows)
            {
                string title = row["Title"].ToString();
                if (!SearchTextBox.Items.Contains(title))
                {
                    SearchTextBox.Items.Add(title);
                }
            }
        }

        private async void EditTaskButton_Click(object sender, RoutedEventArgs e)
        {
            if (TasksDataGrid.SelectedItem is DataRowView row)
            {
                int taskId = Convert.ToInt32(row["Id"]);
                string title = row["Title"].ToString();
                string description = row["Description"].ToString();
                DateTime? dueDate = row["DueDate"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(row["DueDate"]);
                string status = row["Status"].ToString();
                bool isRecurring = row["IsRecurring"] != DBNull.Value && Convert.ToBoolean(row["IsRecurring"]);


                EditTaskWindow editTaskWindow = new EditTaskWindow(taskId, title, description, dueDate, status, isRecurring, _statuses);
                editTaskWindow.ShowDialog();

                await LoadTasksAsync();
            }
            else
            {
                MessageBox.Show("Please select a task to edit.");
            }
        }

        private async void DeleteTaskButton_Click(object sender, RoutedEventArgs e)
        {
            if (TasksDataGrid.SelectedItem is DataRowView row)
            {
                int taskId = Convert.ToInt32(row["Id"]);
                bool isRecurring = row["IsRecurring"] != DBNull.Value && Convert.ToBoolean(row["IsRecurring"]);

                if (isRecurring)
                {
                    MessageBox.Show("Cannot delete a recurring task. Please update the task status instead.", "Delete Not Allowed", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (MessageBox.Show("Are you sure you want to delete this task?", "Confirm Delete", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    try
                    {
                        string connectionString = await GetConnectionStringAsync();
                        using (SqlConnection connection = new SqlConnection(connectionString))
                        {
                            await connection.OpenAsync();

                            string query = "DELETE FROM Tasks WHERE Id = @Id";
                            using (SqlCommand command = new SqlCommand(query, connection))
                            {
                                command.Parameters.AddWithValue("@Id", taskId);
                                await command.ExecuteNonQueryAsync();
                            }
                        }

                        MessageBox.Show("Task deleted successfully!");
                        await LoadTasksAsync();
                    }
                    catch (Exception ex)
                    {
                        LogError(ex);
                        MessageBox.Show("Error deleting task. Please try again.");
                    }
                }
            }
            else
            {
                MessageBox.Show("Please select a task to delete.");
            }
        }


        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            string searchText = SearchTextBox.Text.ToLower();

            if (TasksDataGrid.ItemsSource is DataView dataView)
            {
                dataView.RowFilter = string.Format("Title LIKE '%{0}%' OR Description LIKE '%{0}%'", searchText.Replace("'", "''"));
            }
        }

        private async void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            DataTable dataTable = (TasksDataGrid.ItemsSource as DataView)?.ToTable();
            if (dataTable == null || dataTable.Rows.Count == 0)
            {
                MessageBox.Show("No data to export.");
                return;
            }

            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                FileName = "tasks.xlsx",
                DefaultExt = ".xlsx",
                Filter = "Excel files (*.xlsx)|*.xlsx|All files (*.*)|*.*"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                string filePath = saveFileDialog.FileName;

                try
                {
                    ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                    using (ExcelPackage package = new ExcelPackage())
                    {
                        ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Tasks");

                        for (int i = 0; i < dataTable.Columns.Count; i++)
                        {
                            worksheet.Cells[1, i + 1].Value = dataTable.Columns[i].ColumnName;
                        }

                        using (ExcelRange range = worksheet.Cells[1, 1, 1, dataTable.Columns.Count])
                        {
                            range.Style.Font.Bold = true;
                            range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                            range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                            range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        }

                        for (int i = 0; i < dataTable.Rows.Count; i++)
                        {
                            for (int j = 0; j < dataTable.Columns.Count; j++)
                            {
                                worksheet.Cells[i + 2, j + 1].Value = dataTable.Rows[i][j];
                            }
                        }

                        worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                        package.SaveAs(new FileInfo(filePath));
                    }

                    MessageBox.Show($"Tasks exported to {filePath}");

                    if (MessageBox.Show("Do you want to open the file?", "Export Complete", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = filePath,
                            UseShellExecute = true
                        });
                    }
                }
                catch (Exception ex)
                {
                    LogError(ex);
                    MessageBox.Show("Error exporting tasks. Please try again.");
                }
            }
        }

        private void LogError(Exception ex)
        {
            // שמירת השגיאה ללוג (או לפלט שגיאות)
            File.AppendAllText("error_log.txt", $"{DateTime.Now}: {ex}\n");
        }

        private void StatusFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // אם TasksDataGrid לא מאותחל עדיין, חזרה מיידית
            if (TasksDataGrid == null || TasksDataGrid.ItemsSource == null)
            {
                return;
            }

            if (TasksDataGrid.ItemsSource is DataView dataView)
            {
                string selectedStatus = StatusFilterComboBox.SelectedItem?.ToString();
                if (selectedStatus == "All")
                {
                    dataView.RowFilter = string.Empty; // הצגת כל המשימות
                }
                else
                {
                    dataView.RowFilter = $"Status = '{selectedStatus.Replace("'", "''")}'"; // סינון לפי הסטטוס הנבחר
                }
            }
        }




        private async void MarkAsCompletedButton_Click(object sender, RoutedEventArgs e)
        {
            if (TasksDataGrid.SelectedItem is DataRowView row)
            {
                int taskId = Convert.ToInt32(row["Id"]);

                try
                {
                    string connectionString = await GetConnectionStringAsync();
                    using (SqlConnection connection = new SqlConnection(connectionString))
                    {
                        await connection.OpenAsync();

                        string query = "UPDATE Tasks SET Status = 'Completed' WHERE Id = @Id";
                        using (SqlCommand command = new SqlCommand(query, connection))
                        {
                            command.Parameters.AddWithValue("@Id", taskId);
                            await command.ExecuteNonQueryAsync();
                        }
                    }

                    MessageBox.Show("Task marked as completed!");
                    await LoadTasksAsync();
                }
                catch (Exception ex)
                {
                    LogError(ex);
                    MessageBox.Show("Error marking task as completed. Please try again.");
                }
            }
            else
            {
                MessageBox.Show("Please select a task to mark as completed.");
            }
        }

    }
}
