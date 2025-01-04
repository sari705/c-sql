using System.Windows;
 

namespace Task
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void AddTaskButton_Click(object sender, RoutedEventArgs e)
        {
            AddTaskWindow addTaskWindow = new AddTaskWindow();
            addTaskWindow.ShowDialog();
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
        private void ViewTasksButton_Click(object sender, RoutedEventArgs e)
        {
            ViewTasksWindow viewTasksWindow = new ViewTasksWindow();
            viewTasksWindow.ShowDialog();
        }

    }
}
