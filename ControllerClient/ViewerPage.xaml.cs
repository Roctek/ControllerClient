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
    /// Interaction logic for ViewerPage.xaml
    /// </summary>
    public partial class ViewerPage : Page
    {
        public MainWindow parentWindow = null;

        public ViewerPage()
        {
            InitializeComponent();
        }

        public void OnPageLoad() { 
        
        }

        private void Back_Button_Click(object sender, RoutedEventArgs e)
        {
            parentWindow.SwitchPage(parentWindow.prev);
        }
    }
}
