using Avalonia.Controls;
using Avalonia.Interactivity;
using System.Threading.Tasks;

namespace demodemo
{
    public enum MessageBoxButtons
    {
        Ok,
        YesNo
    }

    public enum MessageBoxResult
    {
        Ok,
        Yes,
        No
    }

    public partial class CustomMessageBox : Window
    {
        private MessageBoxResult _result = MessageBoxResult.No;

        public CustomMessageBox()
        {
            InitializeComponent();
        }

        public static async Task<MessageBoxResult> ShowAsync(Window owner, string title, string message, MessageBoxButtons buttons)
        {
            var dialog = new CustomMessageBox();
            dialog.Title = title;
            dialog.MessageTextBlock.Text = message;

            switch (buttons)
            {
                case MessageBoxButtons.Ok:
                    dialog.OkButton.IsVisible = true;
                    dialog._result = MessageBoxResult.Ok; 
                    break;
                case MessageBoxButtons.YesNo:
                    dialog.YesButton.IsVisible = true;
                    dialog.NoButton.IsVisible = true;
                    dialog._result = MessageBoxResult.No; 
                    break;
            }

            await dialog.ShowDialog(owner);
            return dialog._result;
        }

        private void Button_Click(object? sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                switch (button.Name)
                {
                    case "OkButton":
                        _result = MessageBoxResult.Ok;
                        break;
                    case "YesButton":
                        _result = MessageBoxResult.Yes;
                        break;
                    case "NoButton":
                        _result = MessageBoxResult.No;
                        break;
                }
            }
            Close();
        }
    }
}