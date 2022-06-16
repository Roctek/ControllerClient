using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ControllerClient
{
    /// <summary>
    /// Interaction logic for MenuPage.xaml
    /// </summary>
    /// 

    public partial class MenuPage : Page
    {
        public MainWindow parentWindow = null;

        public MenuPage()
        {
            InitializeComponent();
            Password_Display_Box.Visibility = Visibility.Hidden;
        }

        public void OnPageLoad() {
            if (parentWindow.errorString.Length > 0) {
                Output_Box.Text = parentWindow.errorString;
            }
            IP_Entry_Box.Text = parentWindow.serverIp;
            Port_Entry_Box.Text = parentWindow.serverPort;
    }

        private void localWriteError(string error) {
            parentWindow.WriteError(error);
            Output_Box.Text = parentWindow.errorString;
        }

        private void Password_Show_Button_Click(object sender, RoutedEventArgs e)
        {
            if (Password_Display_Box.Visibility == Visibility.Visible) //Show button Release
            {
                Password_Show_Button.ClickMode = ClickMode.Press;
                Password_Display_Box.Text = "";

                Password_Entry_Box.Visibility = Visibility.Visible;
                Password_Display_Box.Visibility = Visibility.Hidden;
            }
            else { //Show Button Push
                Password_Show_Button.ClickMode = ClickMode.Release;
                Password_Display_Box.Text = Password_Entry_Box.Password;

                Password_Display_Box.Visibility = Visibility.Visible;
                Password_Entry_Box.Visibility = Visibility.Hidden;
            }
        }

        private void Connect_Button_Click(object sender, RoutedEventArgs e)
        {
            if (IP_Entry_Box.Text.Length == 0)
            {
                localWriteError("Please enter an IP.\n");
                return;
            }
            if (Port_Entry_Box.Text.Length == 0)
            {
                localWriteError("Please enter a port number.\n");
                return;
            }
            Int32 port;
            if (!Int32.TryParse(Port_Entry_Box.Text, out port))
            {
                localWriteError("Invalid Port. Please specify a positive integer.\n");
                return;
            }
            string ip = IP_Entry_Box.Text;
            localWriteError(string.Format("Connecting to {0}:{1}...\n", ip, port));
            if (!parentWindow.Connect(ip, port))
            {
                Output_Box.Text = parentWindow.errorString;
            }
            parentWindow.SwitchPage(PageState.VIEWER);
        }

        private void Keybinds_Button_Click(object sender, RoutedEventArgs e)
        {
            parentWindow.SwitchPage(PageState.KEYBINDS);
        }
    }
}
