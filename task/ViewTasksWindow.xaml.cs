using OfficeOpenXml;
using OfficeOpenXml.Style;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Drawing;
using System.Linq;

namespace Task
{
    public partial class ViewTasksWindow : Window
    {
        private List<string> _statuses = new List<string>();

        private void LoadStatuses()
        {
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["TaskManagerDB"].ConnectionString;
                if (string.IsNullOrEmpty(connectionString))
                {
                    MessageBox.Show("Connection string 'TaskManagerDB' not found in the configuration file.");
                    return;
                }
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    string query = "SELECT StatusName FROM Statuses";
                    SqlCommand command = new SqlCommand(query, connection);
                    SqlDataReader reader = command.ExecuteReader();

                    _statuses.Clear();
                    while (reader.Read())
                    {
                        _statuses.Add(reader["StatusName"].ToString());
                    }

                    // עדכון ComboBox
                    StatusFilterComboBox.Items.Clear();
                    StatusFilterComboBox.Items.Add("All");
                    foreach (var status in _statuses)
                    {
                        StatusFilterComboBox.Items.Add(status);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading statuses: {ex.Message}");
            }
        }

        public ViewTasksWindow()
        {
            InitializeComponent();

            // ביטול זמני של האירוע
            StatusFilterComboBox.SelectionChanged -= StatusFilterComboBox_SelectionChanged;

            LoadStatuses();
            LoadTasks();

            // החזרת האירוע לאחר הטעינה
            StatusFilterComboBox.SelectionChanged += StatusFilterComboBox_SelectionChanged;
        }

        private void LoadTasks()
        {
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["TaskManagerDB"].ConnectionString;
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    string query = "SELECT Id, Title, Description, DueDate, Status, IsRecurring FROM Tasks";
                    SqlDataAdapter adapter = new SqlDataAdapter(query, connection);
                    DataTable tasksTable = new DataTable();
                    adapter.Fill(tasksTable);

                    TasksDataGrid.ItemsSource = tasksTable.DefaultView;

                    // רענון הצעות חיפוש וסגנונות שורות
                    LoadSearchSuggestions();
                    RefreshRowStyles();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading tasks: {ex.Message}");
            }
        }

        private void RefreshRowStyles()
        {
            foreach (var item in TasksDataGrid.Items)
            {
                if (TasksDataGrid.ItemContainerGenerator.ContainerFromItem(item) is DataGridRow row)
                {
                    row.InvalidateProperty(DataGridRow.BackgroundProperty);
                }
            }
        }

        private void EditTaskButton_Click(object sender, RoutedEventArgs e)
        {
            if (TasksDataGrid.SelectedItem is DataRowView row)
            {
                int taskId = Convert.ToInt32(row["Id"]);
                string title = row["Title"].ToString();
                string description = row["Description"].ToString();
                DateTime? dueDate = row["DueDate"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(row["DueDate"]);
                string status = row["Status"].ToString();

                // פתיחת חלון העריכה
                EditTaskWindow editTaskWindow = new EditTaskWindow(taskId, title, description, dueDate, status, _statuses);
                editTaskWindow.ShowDialog();

                // רענון הרשימה לאחר סגירת החלון
                LoadTasks();
            }
            else
            {
                MessageBox.Show("Please select a task to edit.");
            }
        }

        private void DeleteTaskButton_Click(object sender, RoutedEventArgs e)
        {
            if (TasksDataGrid.SelectedItem is DataRowView row)
            {
                int taskId = Convert.ToInt32(row["Id"]);

                try
                {
                    string connectionString = ConfigurationManager.ConnectionStrings["TaskManagerDB"].ConnectionString;
                    using (SqlConnection connection = new SqlConnection(connectionString))
                    {
                        connection.Open();

                        string query = "DELETE FROM Tasks WHERE Id = @Id";
                        using (SqlCommand command = new SqlCommand(query, connection))
                        {
                            command.Parameters.AddWithValue("@Id", taskId);
                            command.ExecuteNonQuery();
                        }
                    }

                    MessageBox.Show("Task deleted successfully!");
                    LoadTasks();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error: {ex.Message}");
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
                dataView.RowFilter = $"Title LIKE '%{searchText}%' OR Description LIKE '%{searchText}%";
            }
        }

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            DataTable dataTable = (TasksDataGrid.ItemsSource as DataView)?.ToTable();
            if (dataTable.Rows.Count == 0)
            {
                MessageBox.Show("No data to export.");
                return;
            }

            if (dataTable != null)
            {
                Microsoft.Win32.SaveFileDialog saveFileDialog = new Microsoft.Win32.SaveFileDialog
                {
                    FileName = "tasks.xlsx",
                    DefaultExt = ".xlsx",
                    Filter = "Excel files (*.xlsx)|*.xlsx|All files (*.*)|*.*"
                };

                bool? result = saveFileDialog.ShowDialog();

                if (result == true)
                {
                    string filePath = saveFileDialog.FileName;

                    ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                    using (ExcelPackage package = new ExcelPackage())
                    {
                        ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Tasks");

                        // הוספת כותרות
                        for (int i = 0; i < dataTable.Columns.Count; i++)
                        {
                            worksheet.Cells[1, i + 1].Value = dataTable.Columns[i].ColumnName;
                        }

                        // עיצוב כותרות
                        using (ExcelRange range = worksheet.Cells[1, 1, 1, dataTable.Columns.Count])
                        {
                            range.Style.Font.Bold = true;
                            range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                            //range.Style.Fill.BackgroundColor.SetColor(Color.LightGray);
                            range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        }

                        // הוספת נתונים
                        for (int i = 0; i < dataTable.Rows.Count; i++)
                        {
                            for (int j = 0; j < dataTable.Columns.Count; j++)
                            {
                                object value = dataTable.Rows[i][j];

                                // המרה לתאריך אם הערך הוא מספר בתור תאריך
                                if (dataTable.Columns[j].ColumnName.Contains("Date") && value is double numericDate)
                                {
                                    value = DateTime.FromOADate(numericDate).ToString("dd/MM/yyyy");
                                }

                                worksheet.Cells[i + 2, j + 1].Value = value;
                            }
                        }

                        // התאמת רוחב עמודות
                        worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                        package.SaveAs(new FileInfo(filePath));
                    }

                    MessageBox.Show($"Tasks exported to {filePath}");
                    if (MessageBox.Show("Do you want to open the file?", "Export Complete", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo()
                        {
                            FileName = filePath,
                            UseShellExecute = true
                        });
                    }

                }
                else
                {
                    MessageBox.Show("Export cancelled by user.");
                }
            }
        }

        private void LoadSearchSuggestions()
        {
            SearchTextBox.Items.Clear();
            foreach (DataRowView row in TasksDataGrid.ItemsSource as DataView)
            {
                string title = row["Title"].ToString();
                if (!SearchTextBox.Items.Contains(title))
                {
                    SearchTextBox.Items.Add(title);
                }
            }
        }

        private void MarkAsCompletedButton_Click(object sender, RoutedEventArgs e)
        {
            if (TasksDataGrid.SelectedItem is DataRowView row)
            {
                int taskId = Convert.ToInt32(row["Id"]);

                try
                {
                    string connectionString = ConfigurationManager.ConnectionStrings["TaskManagerDB"].ConnectionString;
                    using (SqlConnection connection = new SqlConnection(connectionString))
                    {
                        connection.Open();

                        string query = "UPDATE Tasks SET Status = 'Completed' WHERE Id = @Id";
                        using (SqlCommand command = new SqlCommand(query, connection))
                        {
                            command.Parameters.AddWithValue("@Id", taskId);
                            command.ExecuteNonQuery();
                        }
                    }

                    MessageBox.Show("Task marked as completed!");
                    LoadTasks();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error: {ex.Message}");
                }
            }
            else
            {
                MessageBox.Show("Please select a task to mark as completed.");
            }
        }

        private void StatusFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // בדיקה אם TasksDataGrid מאותחל
            if (TasksDataGrid == null || TasksDataGrid.ItemsSource == null)
            {
                return; // יציאה מהפונקציה אם TasksDataGrid או ItemsSource לא מאותחלים
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
                    dataView.RowFilter = $"Status = '{selectedStatus}'"; // סינון לפי הסטטוס הנבחר
                }
            }
        }

        private void AddStatusButton_Click(object sender, RoutedEventArgs e)
        {
            string newStatus = NewStatusTextBox.Text.Trim();
            if (!string.IsNullOrEmpty(newStatus) && !_statuses.Contains(newStatus))
            {
                try
                {
                    string connectionString = ConfigurationManager.ConnectionStrings["TaskManagerDB"].ConnectionString;
                    using (SqlConnection connection = new SqlConnection(connectionString))
                    {
                        connection.Open();

                        string query = "INSERT INTO Statuses (StatusName) VALUES (@StatusName)";
                        using (SqlCommand command = new SqlCommand(query, connection))
                        {
                            command.Parameters.AddWithValue("@StatusName", newStatus);
                            command.ExecuteNonQuery();
                        }
                    }

                    // עדכון רשימת הסטטוסים המקומית
                    _statuses.Add(newStatus);

                    // עדכון ComboBox בצורה בטוחה
                    StatusFilterComboBox.Items.Add(newStatus);

                    MessageBox.Show($"Status '{newStatus}' added!");
                    NewStatusTextBox.Text = string.Empty;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error adding status: {ex.Message}");
                }
            }
            else
            {
                MessageBox.Show("Please enter a valid and unique status.");
            }
        }
    }
}
