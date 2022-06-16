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
using System.Configuration;
using System.Xml;

using shared;


namespace ControllerClient
{

    //User Gui
    public partial class KeybindsPage : Page
    {
        public MainWindow parentWindow = null;

        //Keybinds
        keybindArray newKeybinds;

        //hold same button objects used in display. entries in same order as newKeybinds
        Button [] controlButtons;
        Button[] keyButtons;

        //Key Setting
        Button curKeySetting = null;
        Brush format_keybutton = null;
        Brush format_SaveButton = null;

        SolidColorBrush highlightBrush = new SolidColorBrush();

        public KeybindsPage()
        {
            InitializeComponent();
            highlightBrush.Color = Color.FromArgb(0xff, 0x34, 0x01, 0x01);
            
        }

        public void OnPageLoad() {
            if (newKeybinds == null)
            {
                newKeybinds = new keybindArray(parentWindow.keybinds);
                ConstructKeyMap();
                format_SaveButton = Save_Button.Background;
            }
        }

        private void ConstructKeyMap() {
            Keybind_Panel.Children.Clear();
            Keybind_Panel.Orientation = Orientation.Vertical;
            controlButtons = new Button[newKeybinds.getLength()];
            keyButtons = new Button[newKeybinds.getLength()];
            for (int i = 0; i < newKeybinds.getLength(); i++) {
                Grid temp = new Grid();
                temp.ColumnDefinitions.Add(new ColumnDefinition());
                temp.ColumnDefinitions.Add(new ColumnDefinition());

                //controller button
                Button tempButton = new Button();
                tempButton.Name = string.Format("Control_{0}",(int)i);  //should generate unique names since new ones are only created here
                tempButton.Content = newKeybinds.getEntry(i).controlName;
                tempButton.Foreground = Control_Name.Foreground.Clone();
                tempButton.Cursor = Control_Name.Cursor;
                tempButton.Background = Control_Name.Background.Clone();

                //keyboard key
                Button tempKey = new Button();
                tempKey.Name = string.Format("Key_{0}", (int)i);
                tempKey.Content = newKeybinds.getEntry(i).keyName;
                tempKey.Foreground = Control_Key.Foreground.Clone();
                tempKey.Cursor = Control_Key.Cursor;
                tempKey.Background = Control_Key.Background.Clone();
                tempKey.Focusable = false;
                tempKey.Click += Key_Button_Click;

                //both array and child element point to same object
                controlButtons[i] = tempButton;
                keyButtons[i] = tempKey;
                temp.Children.Add(tempButton);
                temp.Children.Add(tempKey);
                Grid.SetColumn(tempKey, 0);
                Grid.SetColumn(tempKey, 1);

                Keybind_Panel.Children.Add(temp);
            }
        }

        private void Back_Button_Click(object sender, RoutedEventArgs e)
        {
            parentWindow.SwitchPage(parentWindow.prev);
        }

        private void Revert_Button_Click(object sender, RoutedEventArgs e)
        {
            newKeybinds.deepCopy(parentWindow.keybinds);
            ConstructKeyMap();
            Save_Button.Background = format_SaveButton;
        }

        private void Save_Button_Click(object sender, RoutedEventArgs e)
        {
            parentWindow.keybinds.deepCopy(newKeybinds);
            Save_Button.Background = format_SaveButton;
        }

        private void Key_Button_Click(object sender, RoutedEventArgs e)
        {
            if (curKeySetting != null) {
                CurKey_Lost_Focus(sender, (KeyboardFocusChangedEventArgs)null);
            }

            //give current button keyboard focus and set handlers
            try
            {
                curKeySetting = (Button)sender;
                curKeySetting.KeyDown += CurKey_KeyDown;
                curKeySetting.LostKeyboardFocus += CurKey_Lost_Focus;
                curKeySetting.Focusable = true;
                if (!(curKeySetting.Focus())){
                    curKeySetting.LostKeyboardFocus -= CurKey_Lost_Focus;
                    curKeySetting.KeyDown -= CurKey_KeyDown;
                    return;
                }
                format_keybutton = curKeySetting.Background;
                curKeySetting.Background = highlightBrush;
            }
            catch (Exception ex) {
                parentWindow.WriteError("ERROR ON KEYSET");
            }
            
        }

        private string SetKeybindsEntry(Button sender, Key val) {
            Button temp;
            string keyName = "";

            

            for (int i = 0; i < keyButtons.Length; i++)
            {
                temp = (Button)((Grid)Keybind_Panel.Children[i]).Children[1];
                //names were generate to be unique
                if (temp.Name == sender.Name)
                {
                    keybindArrayEntry entry = newKeybinds.getEntry(i);
                    if (!parentWindow.kbfilter.isValid(val))
                    {      //if key is invalid remove keybind. could just leave alone, but currently no other way to clear.
                        val = Key.None;
                        keyName = "_None";
                    }
                    else
                    {
                        keyName = parentWindow.kbfilter.getString(val);
                    }
                    entry.keyCode = val;
                    entry.keyName = keyName;
                    //denotes save needed
                    Save_Button.Background = highlightBrush;
                    break;
                }
            }
            return keyName;
        }

        //handler for key set 
        private void CurKey_KeyDown(object sender, KeyEventArgs e)
        {
            curKeySetting.Content = SetKeybindsEntry((Button)sender, e.Key);
            SetKeybindsEntry((Button)sender, e.Key);
            CurKey_Lost_Focus(sender,(KeyboardFocusChangedEventArgs)null);
        }

        //handler for keyset cleanup
        private void CurKey_Lost_Focus(object sender, KeyboardFocusChangedEventArgs e) {
            curKeySetting.KeyDown -= CurKey_KeyDown;
            curKeySetting.LostKeyboardFocus -= CurKey_Lost_Focus;
            curKeySetting.Background = format_keybutton;
            curKeySetting.Focusable = false;
            format_keybutton = null;
            curKeySetting = null;
            
        }
    }
}
