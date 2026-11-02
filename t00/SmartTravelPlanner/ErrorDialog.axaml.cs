using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace SmartTravelPlanner
{
    public partial class ErrorDialog : Window
    {
        public ErrorDialog()
        {
            InitializeComponent();
        }

        public static async Task ShowAsync(Window owner, string message)
        {
            ErrorDialog dialog = new ErrorDialog();
            dialog.MessageTextBlock.Text = message;
            await dialog.ShowDialog(owner);
        }

        private void OkButton_Click(object? sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}