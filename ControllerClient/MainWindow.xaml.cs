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
using System.Net.Sockets;
using System.Configuration;
using System.Xml.Schema;
using System.Xml;
using System.IO;

using shared;


namespace ControllerClient
{

    /// Interaction logic for MainWindow.xaml

    public enum PageState { MENU, VIEWER, KEYBINDS }

    public partial class MainWindow : NavigationWindow
    {
        //window Management
        private MenuPage mainMenu_page = new MenuPage();
        private ViewerPage viewer_page = new ViewerPage();
        private KeybindsPage keybind_page = new KeybindsPage();
        public readonly PageState curPage;
        public readonly PageState prev;

        //config
        private readonly string schemaName = "ControllerSchema.xsd";
        private readonly string configName = "ControllerConfig.xml";
        private readonly string configOutName = "ConfigOutTest.xml";
        public ConfigXmlDocument configFile = new ConfigXmlDocument();

        //key mapping
        public keybindArray keybinds;
        public KeybindTranslator kbTranslator = null;
        public keyFilter kbfilter = null;

        //networking
        public string serverIp = "";
        public string serverPort = "";
        private TcpClient tcpClient = null;
        private NetworkStream serverStream;

        //error
        public String errorString = "";
        bool LockOnMenu = false;

        //testing
        System.Windows.Media.ImageSourceValueSerializer ImageSerializer;

        public MainWindow()
        {
            InitializeComponent();
            mainMenu_page.parentWindow = this;
            viewer_page.parentWindow = this;
            keybind_page.parentWindow = this;
            if (!LoadConfig())
            {
                LockOnMenu = true;
            }
            else if (!ParseConfig())
            {
                LockOnMenu = true;
            }
            if (keybinds == null || kbfilter == null)
            {
                WriteError(string.Format("Failed to initialize from config file: {0}", configName));
                LockOnMenu = true;
            }
            SwitchPage(PageState.MENU);
        }

        public void SwitchPage(PageState page)
        {
            if (LockOnMenu)
            {
                page = PageState.MENU;
            }
            switch (page)
            {
                case PageState.MENU:
                    this.Content = mainMenu_page;
                    mainMenu_page.OnPageLoad();
                    break;
                case PageState.VIEWER:
                    this.Content = viewer_page;
                    viewer_page.OnPageLoad();
                    break;
                case PageState.KEYBINDS:
                    this.Content = keybind_page;
                    keybind_page.OnPageLoad();
                    break;
            }
        }

        public void WriteError(string err)
        {
            errorString = err + errorString;
        }

        private bool LoadConfig()
        {
            XmlReader configReader = null;
            //XmlReader schemaReader = null;
            XmlSchema schema;
            XmlSchemaSet sSet = new XmlSchemaSet();
            XmlReaderSettings settings = new XmlReaderSettings();
            try
            {
                //schemaReader = XmlReader.Create(schemaName);
                schema = sSet.Add(null, schemaName);
                //schemaReader.Close();
            }
            catch (FileNotFoundException)
            {
                //if (schemaReader != null)
                //   schemaReader.Close();
                errorString = string.Format("Could not find config schema file: {0}\n", schemaName) + errorString;
                return false;
            }
            if (schema == null)
            {
                errorString = string.Format("Could not load config schema file: {0}\n", schemaName) + errorString;
                return false;
            }
            settings.Schemas.Add(schema);
            settings.ValidationEventHandler += OnConfigValidationEvent;
            settings.ValidationFlags = settings.ValidationFlags | XmlSchemaValidationFlags.ReportValidationWarnings;
            settings.ValidationType = ValidationType.Schema;
            try
            {
                //FileStream configStream = File.OpenRead(configName);
                configReader = XmlReader.Create(configName, settings);
                configFile = new ConfigXmlDocument();
                configFile.PreserveWhitespace = true;
                configFile.Load(configReader);
                configReader.Close();
            }
            catch (FileNotFoundException)
            {
                errorString = string.Format("Could not find config file: {0}\n", configName) + errorString;
                if (configReader != null)
                    configReader.Close();
                return false;
            }
            catch (Exception e)
            {
                errorString = "Config load Error:" + e.Message;
                return false;
            }
            return true;
        }

        private void OnConfigValidationEvent(object sender, ValidationEventArgs e)
        {
            if (e.Severity == XmlSeverityType.Warning)
            {
                errorString =
                    ("Config warning: " + e.Message + "\n") + errorString;
            }
            else if (e.Severity == XmlSeverityType.Error)
            {
                errorString +=
                    ("Config error: " + e.Message + "\n") + errorString;
                Type objectType = sender.GetType();
            }
        }

        public string cleanName(string name) {
            return name.Replace('_', ' ');
        }

        private bool ParseConfig()
        {
            try
            {
                XmlNode root = configFile.LastChild; //tries to read header with FirstChild()
                for (int i = 0; i < root.ChildNodes.Count; i++)
                {
                    XmlNode curnode = root.ChildNodes[i];

                    if (curnode.Name == "connectHistory")
                    {
                        for (int j = 0; j < curnode.ChildNodes.Count; j++)
                        {
                            XmlNode entryNode = curnode.ChildNodes[j];
                            if (entryNode.Name == "entry")
                            { //only looks at most recent entry
                                serverIp = entryNode.Attributes.GetNamedItem("ip").Value;
                                serverPort = entryNode.Attributes.GetNamedItem("port").Value;
                                break;
                            }
                        }
                    }
                    else if (curnode.Name == "keyFilter")
                    {
                        XmlNode[] tempList = new XmlNode[curnode.ChildNodes.Count];
                        int keyCount = 0;
                        for (int j = 0; j < curnode.ChildNodes.Count; j++)
                        {
                            XmlNode curKey = curnode.ChildNodes[j];
                            if (curKey.Name == "key")
                            {
                                for (int k = 0; k < curKey.ChildNodes.Count; k++)
                                {
                                    if (curKey.ChildNodes[k].HasChildNodes && (curKey.ChildNodes[k].ChildNodes[0].Value != null))
                                    {
                                        tempList[keyCount] = curKey.ChildNodes[k].ChildNodes[0];
                                        keyCount++;
                                        break;
                                    }
                                }
                            }
                        }
                        Key[] karr = new Key[keyCount];
                        string[] narr = new string[keyCount];
                        for (int j = 0; j < keyCount; j++)
                        {
                            int keyval;
                            int.TryParse(tempList[j].Value, out keyval);
                            karr[j] = (Key)keyval;
                            narr[j] = cleanName(tempList[j].ParentNode.Name);
                        }
                        kbfilter = new keyFilter(karr, narr);
                    }
                    else if (curnode.Name == "keybinds")
                    {
                        keybindArrayEntry[] tempKeybinds = new keybindArrayEntry[curnode.ChildNodes.Count];
                        keybindArrayEntry[] finalKeybinds;
                        int entryCount = 0;
                        for (int j = 0; j < curnode.ChildNodes.Count; j++)
                        {
                            XmlNode curentry = curnode.ChildNodes[j];
                            if (curentry.Name == "entry")
                            {
                                Key tempKey = Key.None;
                                ControlKey tempControl = ControlKey.None;
                                string controlString = "";
                                string keyString = "";
                                for (int k = 0; k < curentry.ChildNodes.Count; k++)
                                {
                                    if (curentry.ChildNodes[k].Name == "control")
                                    {
                                        int tempval;
                                        XmlNode curControl = curentry.ChildNodes[k];
                                        for (int h = 0; h < curControl.ChildNodes.Count; h++)
                                        {
                                            if (curControl.ChildNodes[h].HasChildNodes && curControl.ChildNodes[h].ChildNodes[0].Value != null)
                                            {
                                                if (!int.TryParse(curControl.ChildNodes[h].ChildNodes[0].Value, out tempval))
                                                {
                                                    WriteError(string.Format("encoundered invalid control code \'{0}\' at line {1}", curControl.ChildNodes[h].ChildNodes[0].Value, configFile.LineNumber));
                                                    return false;
                                                }
                                                tempControl = (ControlKey)tempval;
                                                controlString = cleanName(curControl.ChildNodes[h].Name);
                                            }
                                        }
                                    }
                                    else if (curentry.ChildNodes[k].Name == "key")
                                    {
                                        int tempval;
                                        XmlNode curKey = curentry.ChildNodes[k];
                                        for (int h = 0; h < curKey.ChildNodes.Count; h++)
                                        {
                                            if (curKey.ChildNodes[h].HasChildNodes && curKey.ChildNodes[h].ChildNodes[0].Value != null)
                                            {
                                                if (!int.TryParse(curKey.ChildNodes[h].ChildNodes[0].Value, out tempval))
                                                {
                                                    WriteError(string.Format("encoundered invalid key code \'{0}\' at line {1}", curKey.ChildNodes[h].ChildNodes[0].Value, configFile.LineNumber));
                                                    return false;
                                                }
                                                tempKey = (Key)tempval;
                                                if (kbfilter != null)
                                                {
                                                    keyString = kbfilter.getString(tempKey);
                                                }
                                            }
                                        }
                                    }
                                }
                                if (tempControl != ControlKey.None) //Can only set Keys to None. A control must always be specified
                                {
                                    tempKeybinds[entryCount] = new keybindArrayEntry(tempControl, tempKey, controlString, keyString);
                                    entryCount++;
                                }
                            }
                        }
                        finalKeybinds = new keybindArrayEntry[entryCount];
                        for (int j = 0; j < entryCount; j++)
                        {
                            finalKeybinds[j] = tempKeybinds[j];
                        }
                        keybinds = new keybindArray(finalKeybinds);
                    }
                    //ignore any entries we don't care about 
                }
            }
            catch (Exception e)
            {
                WriteError(string.Format("Config error on line {0}: {1}\n", configFile.LineNumber, e.Message));
                return false;
            }
            return true;
        }

        public bool writeConfig() {
            //update xml document with new keybinds
            /*
            try
            {
                XmlNode root = configFile.LastChild; //tries to read header with FirstChild()
                for (int i = 0; i < root.ChildNodes.Count; i++)
                {
                    XmlNode curnode = root.ChildNodes[i];
                    if (curnode.Name == "keybinds")
                    {
                        for (int j = 0; j < curnode.ChildNodes.Count; j++)
                        {
                            XmlNode curentry = curnode.ChildNodes[j];
                            if (curentry.Name == "entry")
                            {
                                XmlNode curKey;
                                XmlNode curControl;
                                Key tempKey = Key.None;
                                ControlKey tempControl = ControlKey.None;
                                string keyString = "";
                                for (int k = 0; k < curentry.ChildNodes.Count; k++)
                                {
                                    if (curentry.ChildNodes[k].Name == "control")
                                    {
                                        int tempval;
                                        curControl = curentry.ChildNodes[k];
                                        for (int h = 0; h < curControl.ChildNodes.Count; h++)
                                        {
                                            if (curControl.ChildNodes[h].HasChildNodes && curControl.ChildNodes[h].ChildNodes[0].Value != null)
                                            {
                                                if (!int.TryParse(curControl.ChildNodes[h].ChildNodes[0].Value, out tempval))
                                                {
                                                    WriteError(string.Format("encoundered invalid control code \'{0}\' at line {1}", curControl.ChildNodes[h].ChildNodes[0].Value, configFile.LineNumber));
                                                    return false;
                                                }
                                                tempControl = (ControlKey)tempval;
                                            }
                                        }
                                    }
                                    else if (curentry.ChildNodes[k].Name == "key")
                                    {
                                        curKey = curentry.ChildNodes[k];
                                    }
                                 
                                        for (int h = 0; h < curKey.ChildNodes.Count; h++)
                                        {
                                            if (curKey.ChildNodes[h].HasChildNodes && curKey.ChildNodes[h].ChildNodes[0].Value != null)
                                            {
                                                if (!int.TryParse(curKey.ChildNodes[h].ChildNodes[0].Value, out tempval))
                                                {
                                                    WriteError(string.Format("encoundered invalid key code \'{0}\' at line {1}", curKey.ChildNodes[h].ChildNodes[0].Value, configFile.LineNumber));
                                                    return false;
                                                }
                                                tempKey = (Key)tempval;
                                                if (kbfilter != null)
                                                {
                                                    keyString = kbfilter.getString(tempKey);
                                                }
                                            }
                                        }
                                        
                                }
                                for (int k = 0; k < keybinds.getLength(); k++) //loop through array to find... this is C
                                {
                                    if (keybinds.getEntry(i).controlCode == tempControl) { 
                                        
                                    }
                                }
                                if (tempControl != ControlKey.None) //Can only set Keys to None. A control must always be specified
                                {
                                    tempKeybinds[entryCount] = new keybindArrayEntry(tempControl, tempKey, controlString, keyString);
                                    entryCount++;
                                }
                            }
                        }
                    }
                    //ignore any entries we don't care about 
                }
            }
            catch (Exception e)
            {
                WriteError(string.Format("Config updating error on line {0}: {1}\n", configFile.LineNumber, e.Message));
                return false;
            }

            //write to file
            try
            {
                configFile.Save(configOutName);
            }
            catch (FileNotFoundException)
            {
                errorString = string.Format("Could not find config file: {0}\n", configOutName) + errorString;
                return false;
            }
            catch (Exception e)
            {
                errorString = "Config Writing Error:" + e.Message;
                return false;
            }
            return true;
            */
            return false;
        }

        public bool Connect(String IPString, Int32 port)
        {
            try
            {
                //tcpClient = new TcpClient(IPString, port);
            }
            catch (Exception)
            {
                WriteError(string.Format("Failed to connect to {0} : {1}\n", IPString, port));
                return false;
            }
            return true;
        }

        private bool SendKeys()
        {
            return true;
            //Byte[] data = System.Text.Encoding.ASCII.GetBytes(message);
        }
    }
}
