using System.Windows;

namespace ServicesChecker
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private LocalServiceChecker serviceChecker;
        public MainWindow()
        {
            InitializeComponent();
            serviceChecker = new LocalServiceChecker();
        }

        private void CheckServiceButton_Click(object sender, RoutedEventArgs e)
        {
            string serviceName = ServiceNameTextBox.Text; // Assume you have a TextBox named ServiceNameTextBox
            string status = serviceChecker.CheckServiceStatus(serviceName);
            MessageBox.Show(status, "Service Status");
        }
    }
}