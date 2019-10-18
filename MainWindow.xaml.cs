using Newtonsoft.Json;
using RfReader_demo.BAL;
using RfReader_demo.DAL;
using RfReader_demo.Helper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml;
using System.Xml.Linq;
using static RfReader_demo.BAL._BAL;
using Sentry;
using System.Net;
using System.Configuration;
using Sentry.Protocol;
using SharpRaven;
using MaterialDesignThemes.Wpf;

namespace RfReader_demo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region _Variables
        public static string ConnectionString;
        private bool IsLive = false;
        public DataTable allPortData;
        private string EditValue;
        SerialPort[] sps = null;
        _BAL Blayer = new _BAL();
        vt_Common vtCommon = new vt_Common();
        public static string conn = ConfigurationManager.ConnectionStrings["DSN"].ConnectionString;
        #endregion

        public MainWindow()
        {
            using (SentrySdk.Init(conn))
            {                
                InitializeComponent();
                readXML(true);
                CreatingLogFile();
                BindComboBoxWithAvailablePorts();
                InitializeAllPortsToSerialPorts(true);                                
            }
        }

        #region PORT FUNCTIONS
        public void InitializeAllPortsToSerialPorts(bool starting)
        {
            var portName = "";
            if (starting)
            {
                if (allPortData != null && allPortData.AsEnumerable().ToList().Count > 0)
                {
                    sps = new SerialPort[allPortData.AsEnumerable().ToList().Count];
                    for (int i = 0; i < allPortData.AsEnumerable().ToList().Count; i++)
                    {
                        try
                        {
                            //portName = portList[i].ToString();
                            portName = allPortData.Rows[i]["DevicePort"].ToString();
                            sps[i] = new SerialPort();
                            sps[i].PortName = portName;
                            sps[i].BaudRate = 9600;
                            sps[i].Parity = Parity.None;
                            sps[i].StopBits = StopBits.One;
                            sps[i].DataBits = 8;
                            sps[i].Handshake = Handshake.None;
                            sps[i].DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);
                            sps[i].Open();
                        }
                        catch (Exception ex)
                        {
                            txtLogs.Document.Blocks.Add(new Paragraph(new Run(ex.Message.ToString())));
                            InsertingLogTextToLogFile(ex.Message.ToString());
                            SentrySdk.CaptureException(ex);
                        }
                    }
                }
                else
                {
                    CloseAllSerialPorts();
                }
            }
            else
            {
                CloseAllSerialPorts();
                if (allPortData != null && allPortData.AsEnumerable().ToList().Count > 0)
                {
                    sps = new SerialPort[allPortData.AsEnumerable().ToList().Count];
                    for (int i = 0; i < allPortData.AsEnumerable().ToList().Count; i++)
                    {
                        portName = allPortData.Rows[i]["DevicePort"].ToString();
                        sps[i] = new SerialPort();
                        sps[i].PortName = portName;
                        sps[i].BaudRate = 9600;
                        sps[i].Parity = Parity.None;
                        sps[i].StopBits = StopBits.One;
                        sps[i].DataBits = 8;
                        sps[i].Handshake = Handshake.None;
                        sps[i].DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);
                        if (sps[i].IsOpen == true)
                        {
                            try
                            { sps[i].Close(); sps[i].Open(); }
                            catch (Exception ex) {  }
                        }
                        else
                        {
                            try
                            { sps[i].Close(); sps[i].Open(); }
                            catch (Exception ex) { }
                        }
                    }
                }
                BindComboBoxWithAvailablePorts();
            }
        }

        private void CloseAllSerialPorts()
        {
            int num;
            string[] portList = SerialPort.GetPortNames().OrderBy(a => a.Length > 3 && int.TryParse(a.Substring(3), out num) ? num : 0).ToArray();
            if (portList != null)
            {
                sps = new SerialPort[portList.ToList().Count];
                for (int i = 0; i < portList.Count(); i++)
                {
                    try
                    {
                        var portName = portList[i].ToString();
                        sps[i] = new SerialPort();
                        sps[i].PortName = portName;
                        sps[i].BaudRate = 9600;
                        sps[i].Parity = Parity.None;
                        sps[i].StopBits = StopBits.One;
                        sps[i].DataBits = 8;
                        sps[i].Handshake = Handshake.None;
                        sps[i].Close();
                    }
                    catch (Exception ex)
                    {
                        txtLogs.Document.Blocks.Add(new Paragraph(new Run(ex.Message.ToString())));
                        InsertingLogTextToLogFile(ex.Message.ToString());
                        SentrySdk.CaptureException(ex);
                    }

                }
            }
        }

        private void BindComboBoxWithAvailablePorts()
        {
            List<string> assingedPorts = new List<string>();
            try
            {
                if (allPortData != null)
                {
                    foreach (DataRow row in allPortData.Rows)
                    {
                        assingedPorts.Add(row[1].ToString());
                    }
                }

                int num;
                string[] getPorts = SerialPort.GetPortNames().OrderBy(a => a.Length > 3 && int.TryParse(a.Substring(3), out num) ? num : 0).ToArray();
                var lst_AllPorts = getPorts.ToList();

                var availablePortsLeft = (from item in lst_AllPorts
                                          where !assingedPorts.Contains(item)
                                          select item).ToList();

                comboBox.Items.Clear();
                foreach (string comport in availablePortsLeft)
                { comboBox.Items.Add(comport); }
            }
            catch (Exception ex)
            {
                txtLogs.Document.Blocks.Add(new Paragraph(new Run(ex.Message.ToString())));
                InsertingLogTextToLogFile(ex.Message.ToString());
                SentrySdk.CaptureException(ex);
            }
        }


        private void DataReceivedHandler(object sender, SerialDataReceivedEventArgs e)
         {
            using (SentrySdk.Init(conn))
            {
                SerialPort sp = (SerialPort)sender;
                try
                {
                    if (!sp.IsOpen) return;
                    string data = sp.ReadLine();
                    var checkPortName = sp.PortName;
                    var rows = allPortData.AsEnumerable().Where(r => r.Field<string>("DevicePort") == checkPortName).ToList();

                    if (rows.Count > 0)
                    {
                        string tablename = rows[0].ItemArray[0].ToString();
                        string[] arr = data.Split('\r');
                        string newVal = arr[0].Substring(arr[0].Length - 8);

                        int decValue = Convert.ToInt32(newVal, 16);
                        string newValueGet = Convert.ToString(decValue);
                        this.Dispatcher.Invoke(() =>
                        {
                            UpdateScreen(newValueGet, false, tablename, checkPortName);
                        });
                    }
                    else
                    {
                        sp.Dispose();
                    }
                }
                catch (Exception ex)
                {
                    txtLogs.Document.Blocks.Add(new Paragraph(new Run(ex.Message.ToString())));
                    InsertingLogTextToLogFile(ex.Message.ToString());
                    SentrySdk.CaptureException(ex);
                }
            }
        }

        #endregion        

        #region XML FUNCTIONS        

        //--------------------------------------------------- XML FUNCTIONS --------------------------------------------------------//
        void readXML(bool starting)
        {
            string path = Directory.GetParent(System.Reflection.Assembly.GetExecutingAssembly().Location).FullName;
            string fileName = System.IO.Path.Combine(path, "appData.xml");
            try
            {
                if (starting == true)
                {
                    vtCommon.dsXML.ReadXml(fileName);
                    vtCommon.dsForDevices.ReadXml(fileName);
                }
                else
                {
                    vtCommon.dsXML.Clear();
                    vtCommon.dsXML.Tables.Clear();
                    vtCommon.dsXML.ReadXml(fileName);
                    vtCommon.dsForDevices.Clear();
                    vtCommon.dsForDevices.ReadXml(fileName);

                    if (vtCommon.dsXML.Tables.Count <= 0)
                    {
                        vtCommon.dsXML.ReadXml(fileName);
                        //vtCommon.dsForDevices.ReadXml(fileName);
                    }
                }
                if (vtCommon.dsXML != null && vtCommon.dsXML.Tables.Count > 0 && vtCommon.dsXML.Tables[0].Rows.Count > 0)
                {
                    var _db = vtCommon.dsXML.Tables["DatabaseConfig"];
                    var _sentry = vtCommon.dsXML.Tables["Sentry"];
                    var _devices = vtCommon.dsXML.Tables["Devices"];
                    allPortData = vtCommon.dsXML.Tables["Devices"];

                    if (_db != null)
                    {
                        txt_IPAddress.Text = Helper.Crypto.Decrypt(_db.Rows[0][0].ToString());
                        txt_PortNumber.Text = Helper.Crypto.Decrypt(_db.Rows[0][1].ToString());
                        txt_ServerName.Text = Helper.Crypto.Decrypt(_db.Rows[0][2].ToString());
                        txt_DatabaseName.Text = Helper.Crypto.Decrypt(_db.Rows[0][3].ToString());
                        txt_Username.Text = Helper.Crypto.Decrypt(_db.Rows[0][4].ToString());
                        txt_Password.Password = Helper.Crypto.Decrypt(_db.Rows[0][5].ToString());
                    }

                    if (_sentry != null)
                    {
                        //txt_SentryKey.Text = Helper.Crypto.Decrypt(_sentry.Rows[0][0].ToString());
                    }

                    if (_devices != null)
                    {
                        int Index = 0;
                        foreach (DataRow dr in _devices.Rows)
                        {
                            foreach (DataColumn Column in _devices.Columns)
                            {
                                dr[Column.ColumnName] = Helper.Crypto.Decrypt(dr[Column.ColumnName].ToString());
                            }
                            Index++;

                        }

                        dg_Devices.Items.Clear();
                        dg_Devices.CanUserAddRows = false;
                        dg_Devices.ItemsSource = _devices.DefaultView;
                    }
                    ConnectionString = "Data Source=(DESCRIPTION =" + "(ADDRESS = (PROTOCOL = TCP)(HOST = " + txt_IPAddress.Text + ")(PORT = " + txt_PortNumber.Text + "))" +
                        "(CONNECT_DATA =" + "(SERVER = DEDICATED)" + "(SERVICE_NAME = ORCL)));" + "User Id=" + txt_Username.Text + ";Password=" + txt_Password.Password + ";";

                }
            }
            catch (Exception ex)
            {
                txtLogs.Document.Blocks.Add(new Paragraph(new Run(ex.Message.ToString())));
                InsertingLogTextToLogFile(ex.Message.ToString());
                SentrySdk.CaptureException(ex);
            }
        }
        void CompareXML()
        {
            string path = Directory.GetParent(System.Reflection.Assembly.GetExecutingAssembly().Location).FullName;
            string fileName = System.IO.Path.Combine(path, "appData.xml");
            try
            {
                vtCommon.dsNew.Clear();
                vtCommon.dsNew.ReadXml(fileName);
               
                if (vtCommon.dsNew != null && vtCommon.dsNew.Tables.Count > 0 && vtCommon.dsNew.Tables[0].Rows.Count > 0)
                {
                    var _db = vtCommon.dsNew.Tables["DatabaseConfig"];
                    var _sentry = vtCommon.dsNew.Tables["Sentry"];
                    var _devices = vtCommon.dsNew.Tables["Devices"];


                    if (_db != null)
                    {
                        txt_IPAddress.Text = Helper.Crypto.Decrypt(_db.Rows[0][0].ToString());
                        txt_PortNumber.Text = Helper.Crypto.Decrypt(_db.Rows[0][1].ToString());
                        txt_ServerName.Text = Helper.Crypto.Decrypt(_db.Rows[0][2].ToString());
                        txt_DatabaseName.Text = Helper.Crypto.Decrypt(_db.Rows[0][3].ToString());
                        txt_Username.Text = Helper.Crypto.Decrypt(_db.Rows[0][4].ToString());
                        txt_Password.Password = Helper.Crypto.Decrypt(_db.Rows[0][5].ToString());
                    }

                    if (_sentry != null)
                    { //txt_SentryKey.Text = Helper.Crypto.Decrypt(_sentry.Rows[0][0].ToString());
                    }

                    if (_devices != null)
                    {
                        var new_Devicetbl = _devices;
                        int Index = 0;
                        foreach (DataRow dr in new_Devicetbl.Rows)
                        {
                            foreach (DataColumn Column in new_Devicetbl.Columns)
                            {
                                dr[Column.ColumnName] = Helper.Crypto.Decrypt(dr[Column.ColumnName].ToString());
                            }
                            Index++;
                        }

                        dg_Devices.ItemsSource = null;
                        dg_Devices.Items.Clear();
                        dg_Devices.CanUserAddRows = false;
                        dg_Devices.ItemsSource = new_Devicetbl.DefaultView;

                        int Index2 = 0;
                        foreach (DataRow dr in new_Devicetbl.Rows)
                        {
                            foreach (DataColumn Column in new_Devicetbl.Columns)
                            {
                                dr[Column.ColumnName] = Helper.Crypto.Encrypt(dr[Column.ColumnName].ToString());
                            }
                            Index2++;
                        }
                    }

                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in reading xml file.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                txtLogs.Document.Blocks.Add(new Paragraph(new Run(ex.Message.ToString())));
                InsertingLogTextToLogFile(ex.Message.ToString());
                SentrySdk.CaptureException(ex);
            }
        }
        void SaveXML(string ipAddress, string portNumber, string serverName, string databaseName, string username, string password, string sentryKey, List<Devices> deviceData)
        {
            string path = Directory.GetParent(System.Reflection.Assembly.GetExecutingAssembly().Location).FullName;
            string fileName = System.IO.Path.Combine(path, "appData.xml");
            try
            {
                if (File.Exists(fileName))
                {
                    File.Delete(fileName);
                }
                XmlWriterSettings xmlWriterSettings = new XmlWriterSettings();
                xmlWriterSettings.Indent = true;
                xmlWriterSettings.NewLineOnAttributes = true;
                using (XmlWriter xmlWriter = XmlWriter.Create(fileName, xmlWriterSettings))
                {
                    xmlWriter.WriteStartDocument();
                    xmlWriter.WriteStartElement("Databases");
                    /*Database Config*/
                    xmlWriter.WriteStartElement("DatabaseConfig");
                    xmlWriter.WriteElementString("IPAddress", ipAddress);
                    xmlWriter.WriteElementString("PortNumber", portNumber);
                    xmlWriter.WriteElementString("ServerName", serverName);
                    xmlWriter.WriteElementString("DatabaseName", databaseName);
                    xmlWriter.WriteElementString("Username", username);
                    xmlWriter.WriteElementString("Password", password);
                    xmlWriter.WriteEndElement();

                    /*Sentry*/
                    xmlWriter.WriteStartElement("Sentry");
                    xmlWriter.WriteElementString("SentryKey", sentryKey);
                    xmlWriter.WriteEndElement();

                    /*Device Data*/
                    foreach (var item in deviceData)
                    {
                        if (item.DeviceName != null || item.DevicePort != null)
                        {
                            try
                            {
                                var decName = Helper.Crypto.Decrypt(item.DeviceName);
                                var decPort = Helper.Crypto.Decrypt(item.DevicePort);
                            }
                            catch (Exception ex)
                            {
                                item.DeviceName = Helper.Crypto.Encrypt(item.DeviceName);
                                item.DevicePort = Helper.Crypto.Encrypt(item.DevicePort);
                            }                            
                            xmlWriter.WriteStartElement("Devices");
                            xmlWriter.WriteElementString("DeviceName", item.DeviceName);
                            xmlWriter.WriteElementString("DevicePort", item.DevicePort);
                            xmlWriter.WriteEndElement();
                        }
                    }
                    xmlWriter.WriteEndDocument();
                    xmlWriter.Flush();
                    xmlWriter.Close();
                }
            }
            catch(Exception ex)
            {
                txtLogs.Document.Blocks.Add(new Paragraph(new Run(ex.Message.ToString())));
                InsertingLogTextToLogFile(ex.Message.ToString());
                SentrySdk.CaptureException(ex);
            }          
        }
        #endregion

        #region OTHER (UPDATE SCREEN, CHECK BEFORE LIVE, CLEAR FIELDS, CONNECTION TEST, START ALL PORTS) FUNCTIONS
        //------------------------------- OTHER (UPDATE SCREEN, CHECK BEFORE LIVE, CLEAR FIELDS, CONNECTION TEST, START ALL PORTS) FUNCTIONS -----------------------------------//                
        public void UpdateScreen(string Data, bool ConfigChange, string tableName, string portName)
        {
            var InsertionInTblStatus = "";
            string textMessage = string.Empty;
            if (IsLive == true && ConfigChange == false)
            {                
                try
                {
                    TableData tblData = new TableData();
                    tblData.RFID = Convert.ToInt32(Data);
                    tblData.TableName = tableName;
                    tblData.CheckTime = DateTime.Now;                    

                    string text = "ID : " + Data + " has been scanned on Port : " + portName + " (" + DateTime.Now + ")";
                    txtLogs.Document.Blocks.Add(new Paragraph(new Run(text)));
                    txtLogs.ScrollToEnd();
                    InsertingLogTextToLogFile(text);

                    InsertionInTblStatus = Blayer.InsertDataToTable(tblData);
                    if (InsertionInTblStatus == "scan")
                    {
                        textMessage = "ID : " + Data + " Insert in database successfully & with Port : " + portName + " (" + DateTime.Now + ")";
                    }
                    else
                    {
                        textMessage = Data + " Insertion in database failed & with Port : " + portName + " (" + DateTime.Now + ") \n Error: " + InsertionInTblStatus.Trim().ToString() + "";
                        SentrySdk.CaptureMessage(InsertionInTblStatus.Trim());
                    }
                }
                catch (Exception ex)
                {                    
                    txtLogs.Document.Blocks.Add(new Paragraph(new Run(ex.Message.ToString())));
                    txtLogs.ScrollToEnd();
                    InsertingLogTextToLogFile(ex.Message.ToString());
                    SentrySdk.CaptureException(ex);
                }
            }
           
            if (ConfigChange)
            {
                try
                {
                    if (Data.Length > 0)
                    {
                        txtLogs.Document.Blocks.Add(new Paragraph(new Run(Data)));
                        InsertingLogTextToLogFile(Data);
                    }
                }
                catch(Exception ex)
                {
                    txtLogs.Document.Blocks.Add(new Paragraph(new Run(ex.Message.ToString())));
                    txtLogs.ScrollToEnd();
                    InsertingLogTextToLogFile(ex.Message.ToString());
                    SentrySdk.CaptureException(ex);
                }
            }
            else
            {
                try
                {
                    if (InsertionInTblStatus == "scan" && IsLive == true)
                    {
                        //string text = "ID : " + Data + " has been scanned on Port : " + portName + " (" + DateTime.Now + ")";
                        txtLogs.Document.Blocks.Add(new Paragraph(new Run(textMessage)));
                        txtLogs.ScrollToEnd();
                        InsertingLogTextToLogFile(textMessage);
                    }
                    else if(InsertionInTblStatus != "scan" && InsertionInTblStatus.Length > 0 && IsLive == true)
                    {
                        //string text = "ID : " + Data + " has been scanned on Port : " + portName + " (" + DateTime.Now + ")";
                        txtLogs.Document.Blocks.Add(new Paragraph(new Run(textMessage)));
                        txtLogs.ScrollToEnd();
                        InsertingLogTextToLogFile(textMessage);
                    }
                    else
                    {
                        string text = "ID : " + Data + " has been scanned on Port : " + portName + " (" + DateTime.Now + ")";
                        txtLogs.Document.Blocks.Add(new Paragraph(new Run(text)));
                        txtLogs.ScrollToEnd();
                        InsertingLogTextToLogFile(text);
                    }
                }
                catch(Exception ex)
                {
                    txtLogs.Document.Blocks.Add(new Paragraph(new Run(ex.Message.ToString())));
                    txtLogs.ScrollToEnd();
                    InsertingLogTextToLogFile(ex.Message.ToString());
                    SentrySdk.CaptureException(ex);
                }
            }
        }
       
        private bool ConnectionTest()
        {
            using (SentrySdk.Init(conn))
            {
                try
                {
                    if (allPortData != null)
                    {
                        var rows = allPortData.AsEnumerable().ToList();
                        if (rows.Count > 0)
                        {
                            for (int i = 0; i < rows.Count; i++)
                            {
                                var getTableName = "";
                                try
                                {
                                    getTableName = rows[i]["DeviceName"].ToString();
                                    DataTable dt = OracleHelper.ExecuteDataset(ConnectionString, CommandType.Text, "Select * from " + getTableName + " where rownum = 1").Tables[0];
                                    if (i == rows.Count - 1)
                                    {
                                        MessageBox.Show("Database connection successfull.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                                        string text = "Database connection successfull. (" + DateTime.Now + ")";
                                        txtLogs.Document.Blocks.Add(new Paragraph(new Run(text)));
                                        InsertingLogTextToLogFile(text);
                                        return true;
                                    }

                                }
                                catch (Exception ex)
                                {
                                    if (ex.Message.ToString().Contains("ORA-00942: table or view does not exist"))
                                    {
                                        MessageBox.Show("" + getTableName + " is not a valid table name on " + rows[i]["DevicePort"].ToString() + " Port.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                                        string text = "" + getTableName + " is not a valid table name on " + rows[i]["DevicePort"].ToString() + " Port. (" + DateTime.Now + ")";
                                        txtLogs.Document.Blocks.Add(new Paragraph(new Run(text)));
                                        InsertingLogTextToLogFile(text);
                                        SentrySdk.CaptureException(ex);
                                        return false;
                                    }
                                    else
                                    {
                                        MessageBox.Show("Database connection failed.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                                        string text = "Database connection failed. (" + DateTime.Now + ")";
                                        txtLogs.Document.Blocks.Add(new Paragraph(new Run(text)));
                                        InsertingLogTextToLogFile(text);
                                        SentrySdk.CaptureException(ex);
                                        return false;
                                    }
                                    //break;                                    
                                }
                            }
                        }
                    }
                    else
                    {
                        MessageBox.Show("Your Port Name or Device Name not found.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        txtLogs.Document.Blocks.Add(new Paragraph(new Run("Your Port Name or Device Name not found. (" + DateTime.Now.ToString() + ")")));
                        InsertingLogTextToLogFile("Your Port Name or Device Name not found. (" + DateTime.Now.ToString() + ")");
                        SentrySdk.CaptureMessage("Your Port Name or Device Name not found. (" + DateTime.Now.ToString() + ")");
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Database not connected.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    txtLogs.Document.Blocks.Add(new Paragraph(new Run("Database not connected. (" + DateTime.Now.ToString() + ")")));
                    InsertingLogTextToLogFile("Database not connected. (" + DateTime.Now.ToString() + ")");
                    SentrySdk.CaptureException(ex);
                    return false;
                }
                return false;
            }
        }
        public bool CheckBeforeLive()
        {
            try
            {
                List<DataRow> getPortDevices = new List<DataRow>();
                if (allPortData != null)
                {
                    getPortDevices = allPortData.AsEnumerable().ToList();
                }

                if (txt_IPAddress.Text.Length > 0 && txt_IPAddress.Text != null &&
                txt_PortNumber.Text.Length > 0 && txt_PortNumber.Text != null &&
                txt_ServerName.Text.Length > 0 && txt_ServerName.Text != null &&
                txt_Username.Text.Length > 0 && txt_Username.Text != null &&
                txt_Password.Password.Length > 0 && txt_Password.Password != null &&
                getPortDevices.Count > 0)
                { return true; }
                else
                {
                    return false;
                }
            }
            catch(Exception ex)
            {
                txtLogs.Document.Blocks.Add(new Paragraph(new Run(ex.Message.ToString())));
                txtLogs.ScrollToEnd();
                InsertingLogTextToLogFile(ex.Message.ToString());
                SentrySdk.CaptureException(ex);
                return false;
            }
        }
        public void clearFields()
        {
            txt_IPAddress.Text = "";
            txt_PortNumber.Text = "";
            txt_ServerName.Text = "";
            txt_DatabaseName.Text = "";
            txt_Username.Text = "";
            txt_Password.Password = "";
            //txt_SentryKey.Text = "";
            dg_Devices.ItemsSource = null;
        }
        #endregion

        #region LOG FILE
        //-------------------------------------------------- LOG FILE FUNCTIONS -----------------------------------------------//
        public void CreatingLogFile()
        {
            try
            {
                var dt = DateTime.Now;
                var year = dt.Year;
                var month = dt.Month;
                var day = dt.Day;
                var hour = dt.Hour.ToString("00.##");
                var min = dt.Minute.ToString("00.##");
                var sec = dt.Second.ToString("00.##");
                var tt = dt.ToString("tt", CultureInfo.InvariantCulture);

                string combileAllTime = year + "" + month + "" + day + "-" + hour + "" + min + "" + sec;

                string path = Directory.GetParent(System.Reflection.Assembly.GetExecutingAssembly().Location).FullName + "\\Logs";
                string fileName = System.IO.Path.Combine(path, "Log-" + combileAllTime + ".txt");
                File.Create(fileName).Dispose();
            }
            catch (Exception ex)
            {
                txtLogs.Document.Blocks.Add(new Paragraph(new Run(ex.Message.ToString())));
                txtLogs.ScrollToEnd();
                InsertingLogTextToLogFile(ex.Message.ToString());
                SentrySdk.CaptureException(ex);
            }
        }
        public static void InsertingLogTextToLogFile(string text)
        {
            RichTextBox txtLogs = new RichTextBox();
            try
            {
                string path = Directory.GetParent(System.Reflection.Assembly.GetExecutingAssembly().Location).FullName + "\\Logs";
                DirectoryInfo di = new DirectoryInfo(path);
                FileInfo[] TXTFiles = di.GetFiles("*.txt");
                var latest_FileName = TXTFiles[TXTFiles.Count() - 1].Name.ToString();

                string pathcombine = System.IO.Path.Combine(path, latest_FileName);

                StringBuilder sb = new StringBuilder();
                sb.AppendLine(text);
                File.AppendAllText(pathcombine, sb.ToString());
                sb.Clear();
            }
            catch(Exception ex)
            {
                txtLogs.Document.Blocks.Add(new Paragraph(new Run(ex.Message.ToString())));
                txtLogs.ScrollToEnd();
                InsertingLogTextToLogFile(ex.Message.ToString());
                SentrySdk.CaptureException(ex);
            }
        }

        #endregion

        #region HEADER BUTTONS (LIVE, TEST CONNECTION, OPEN CONFIG, OPEN LOG, LOG TEXT CHANGE EVENT) (EVENTS)
        //------------------------------------------ HEADER BUTTONS (LIVE, TEST CONNECTION, OPEN CONFIG, OPEN LOG, LOG TEXT CHANGE EVENT) (EVENTS) --------------------------------------------//
        private void btnLive_Click(object sender, RoutedEventArgs e)
        {
            using (SentrySdk.Init(conn))
            {
                try
                {
                    var LiveBtn_TextBlockText = txtBlock_BtnLive.Text.ToString();
                    //var getStatus = CheckBeforeLive();                
                    var connectionTest = ConnectionTest();

                    if (connectionTest == true)
                    {
                        if (LiveBtn_TextBlockText == "Go Live")
                        {
                            IsLive = true;
                            txtBlock_BtnLive.Text = "Go Offline";
                            btnLive.Background = Brushes.Red;
                            string text = "Live is Started ...! (" + DateTime.Now + ")";
                            txtLogs.Document.Blocks.Add(new Paragraph(new Run(text)));
                            InsertingLogTextToLogFile(text);
                        }
                        else
                        {
                            IsLive = false;
                            txtBlock_BtnLive.Text = "Go Live";
                            btnLive.Background = Brushes.LimeGreen;
                            string text = "Live is Stopped ...! (" + DateTime.Now + ")";
                            txtLogs.Document.Blocks.Add(new Paragraph(new Run(text)));
                            InsertingLogTextToLogFile(text);
                        }
                    }
                    else if (connectionTest == false)
                    {
                        MessageBox.Show("Please make valid database connection, then Go Live.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                        txtLogs.Document.Blocks.Add(new Paragraph(new Run("Please make valid database connection, then Go Live. (" + DateTime.Now.ToString() + ")")));
                        InsertingLogTextToLogFile("Please make valid database connection, then Go Live. (" + DateTime.Now.ToString() + ")");
                        SentrySdk.CaptureMessage("Please make valid database connection, then Go Live.");
                    }
                    else
                    {
                        MessageBox.Show("There is some error please try again.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                        txtLogs.Document.Blocks.Add(new Paragraph(new Run("There is some error please try again. (" + DateTime.Now.ToString() + ")")));
                        InsertingLogTextToLogFile("There is some error please try again. (" + DateTime.Now.ToString() + ")");
                        SentrySdk.CaptureMessage("There is some error please try again.");
                    }
                }
                catch (Exception ex)
                {
                    txtLogs.Document.Blocks.Add(new Paragraph(new Run(ex.Message.ToString())));
                    txtLogs.ScrollToEnd();
                    InsertingLogTextToLogFile(ex.Message.ToString());
                    SentrySdk.CaptureException(ex);
                }
            }
        }
        private void btn_TestConnection_Click(object sender, RoutedEventArgs e)
        {
            var status = ConnectionTest();
        }
        
        private void btn_DbConfig_Click(object sender, RoutedEventArgs e)
        {
            using (SentrySdk.Init(conn))
            {
                try
                {
                    string path2 = @"" + Directory.GetParent(System.Reflection.Assembly.GetExecutingAssembly().Location).FullName + "\\appData.xml";
                    System.Diagnostics.Process.Start("notepad.exe", path2);
                }
                catch (Exception ex)
                {
                    txtLogs.Document.Blocks.Add(new Paragraph(new Run(ex.Message + " (" + DateTime.Now.ToString() + ")")));
                    InsertingLogTextToLogFile(ex.Message + " (" + DateTime.Now.ToString() + ")");
                    SentrySdk.CaptureException(ex);
                }
            }
        }
        private void btn_OpenLogFile_Click(object sender, RoutedEventArgs e)
        {
            using (SentrySdk.Init(conn))
            {
                try
                {
                    string path2 = @"" + Directory.GetParent(System.Reflection.Assembly.GetExecutingAssembly().Location).FullName + "\\Logs";
                    System.Diagnostics.Process.Start(path2);
                }
                catch (Exception ex)
                {
                    txtLogs.Document.Blocks.Add(new Paragraph(new Run(ex.Message + " (" + DateTime.Now.ToString() + ")")));
                    InsertingLogTextToLogFile(ex.Message + " (" + DateTime.Now.ToString() + ")");
                    SentrySdk.CaptureException(ex);
                }
            }
        }

        private void txtLogs_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                txtLogs.ScrollToEnd();
                txtLogs.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            }
            catch (Exception ex)
            {
                txtLogs.Document.Blocks.Add(new Paragraph(new Run(ex.Message + " (" + DateTime.Now.ToString() + ")")));
                InsertingLogTextToLogFile(ex.Message + " (" + DateTime.Now.ToString() + ")");
                SentrySdk.CaptureException(ex);
            }
        }
        #endregion

        #region DATA GRID BUTTONS (ADD NEW RECORD, EDIT, DELETE, CANCEL) (EVENTS)
        //------------------------------------------ DATA GRID BUTTONS (ADD NEW RECORD, EDIT, DELETE, CANCEL) (EVENTS) --------------------------------------------//

        private void btn_AddNewRow_Click(object sender, RoutedEventArgs e)
        {
            using (SentrySdk.Init(conn))
            {
                try
                {
                    string getPort_FromComboBox = comboBox.SelectedItem == null ? String.Empty : comboBox.SelectedItem.ToString(); // <-- Exception            

                    if (txt_DeviceName.Text == null || txt_DeviceName.Text.Length < 1 || getPort_FromComboBox == null || getPort_FromComboBox.Length < 1)
                    {
                        MessageBox.Show("Please Enter Device Name or Device Port.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        txt_DeviceName.Text = "";
                        txt_DevicePort.Text = "";
                        comboBox.SelectedItem = null;
                        txtLogs.Document.Blocks.Add(new Paragraph(new Run("Please Enter Device Name or Device Port. (" + DateTime.Now.ToString() + ")")));
                        InsertingLogTextToLogFile("Please Enter Device Name or Device Port. (" + DateTime.Now.ToString() + ")");
                        SentrySdk.CaptureMessage("Please Enter Device Name or Device Port.");
                    }
                    else
                    {
                        var getButtonText = btn_AddNewRow.Content.ToString();
                        if (getButtonText == "Update")
                        {
                            for (int i = allPortData.Rows.Count - 1; i >= 0; i--)
                            {
                                DataRow dr = allPortData.Rows[i];
                                if (dr["DevicePort"].ToString() == EditValue)
                                {
                                    allPortData.Rows[i]["DeviceName"] = txt_DeviceName.Text;
                                    allPortData.Rows[i]["DevicePort"] = comboBox.SelectedItem.ToString();
                                    //dr.Delete();
                                }
                            }
                            dg_Devices.ItemsSource = null;
                            dg_Devices.ItemsSource = allPortData.DefaultView;
                            txt_DeviceName.Text = "";
                            txt_DevicePort.Text = "";
                            comboBox.SelectedItem = null;
                            btn_AddNewRow.Content = "Add";
                        }
                        else
                        {
                            if (allPortData != null)
                            {
                                var rows = allPortData.AsEnumerable().ToList();
                                DataRow dt_row = allPortData.NewRow();
                                dt_row["DevicePort"] = comboBox.SelectedItem.ToString();
                                dt_row["DeviceName"] = txt_DeviceName.Text;
                                allPortData.Rows.Add(dt_row);

                                dg_Devices.ItemsSource = null;
                                dg_Devices.Items.Clear();
                                dg_Devices.ItemsSource = allPortData.DefaultView;
                                txt_DeviceName.Text = "";
                                txt_DevicePort.Text = "";
                                comboBox.SelectedItem = null;
                            }
                            else
                            {
                                allPortData = new DataTable();
                                dg_Devices.ItemsSource = null;
                                dg_Devices.Items.Add(new { DeviceName = txt_DeviceName.Text, DevicePort = comboBox.SelectedItem.ToString() });
                                allPortData.Columns.Add("DeviceName", typeof(System.String));
                                allPortData.Columns.Add("DevicePort", typeof(System.String));

                                DataRow datarow = allPortData.NewRow();
                                datarow["DeviceName"] = txt_DeviceName.Text;
                                datarow["DevicePort"] = comboBox.SelectedItem.ToString();
                                allPortData.Rows.Add(datarow);
                                txt_DeviceName.Text = "";
                                txt_DevicePort.Text = "";
                                comboBox.SelectedItem = null;
                            }
                        }
                    }
                    BindComboBoxWithAvailablePorts();
                }
                catch (Exception ex)
                {
                    txtLogs.Document.Blocks.Add(new Paragraph(new Run(ex.Message + " (" + DateTime.Now.ToString() + ")")));
                    InsertingLogTextToLogFile(ex.Message + " (" + DateTime.Now.ToString() + ")");
                    SentrySdk.CaptureException(ex);
                }
            }
        }
        private void btnEdit_DataGrid_Click(object sender, RoutedEventArgs e)
        {
            using (SentrySdk.Init(conn))
            {
                try
                {
                    BindComboBoxWithAvailablePorts();
                    var selectedItem = dg_Devices.SelectedItem;
                    if (selectedItem != null)
                    {
                        var get_DevicePort = ((DataRowView)selectedItem).Row.ItemArray[1];
                        var get_DeviceName = ((System.Data.DataRowView)selectedItem).Row.ItemArray[0].ToString();

                        EditValue = get_DevicePort.ToString();

                        txt_DeviceName.Text = get_DeviceName;
                        txt_DevicePort.Text = get_DevicePort.ToString();
                        ComboBoxItem item = new ComboBoxItem();
                        comboBox.Items.Add(get_DevicePort);
                        comboBox.SelectedIndex = comboBox.Items.IndexOf(get_DevicePort);
                        btn_AddNewRow.Content = "Update";
                    }
                }
                catch (Exception ex)
                {
                    txtLogs.Document.Blocks.Add(new Paragraph(new Run(ex.Message + " (" + DateTime.Now.ToString() + ")")));
                    InsertingLogTextToLogFile(ex.Message + " (" + DateTime.Now.ToString() + ")");
                    SentrySdk.CaptureException(ex);
                }
            }
        }
        private void btnDelete_DataGrid_Click(object sender, RoutedEventArgs e)
        {
            using (SentrySdk.Init(conn))
            {
                try
                {
                    var selectedItem = dg_Devices.SelectedItem;
                    if (selectedItem != null)
                    {
                        var get = ((System.Data.DataRowView)selectedItem).Row.ItemArray[1].ToString();
                        dg_Devices.ItemsSource = null;
                        dg_Devices.Items.Remove(selectedItem);

                        for (int i = allPortData.Rows.Count - 1; i >= 0; i--)
                        {
                            DataRow dr = allPortData.Rows[i];
                            if (dr["DevicePort"].ToString() == get)
                            {
                                dr.Delete();
                            }
                        }
                        allPortData.AcceptChanges();
                        dg_Devices.ItemsSource = allPortData.DefaultView;

                        JsonSerializerSettings jss = new JsonSerializerSettings();
                        jss.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                        var ipAddress = Helper.Crypto.Encrypt(txt_IPAddress.Text);
                        var portNumber = Helper.Crypto.Encrypt(txt_PortNumber.Text);
                        var serverName = Helper.Crypto.Encrypt(txt_ServerName.Text);
                        var databaseName = Helper.Crypto.Encrypt(txt_DatabaseName.Text);
                        var username = Helper.Crypto.Encrypt(txt_Username.Text);
                        var password = Helper.Crypto.Encrypt(txt_Password.Password);
                        var sentryKey = Helper.Crypto.Encrypt("");

                        string Respone = JsonConvert.SerializeObject(allPortData, jss);

                        List<Devices> deviceData = JsonConvert.DeserializeObject<List<Devices>>(Respone);
                        SaveXML(ipAddress, portNumber, serverName, databaseName, username, password, sentryKey, deviceData);
                        CompareXML();
                        vtCommon.dtChanges = vtCommon.CompareData();
                        if (vtCommon.dtChanges != null && vtCommon.dtChanges.Rows.Count > 0)
                        {
                            var deviceName = "";
                            string text = "";
                            foreach (DataRow dr in vtCommon.dtChanges.Rows)
                            {
                                try
                                {
                                    if (dr["FieldName"].ToString() == "DeviceName")
                                    {
                                        var lst_DeviceNames = Crypto.Decrypt(dr["NewValue"].ToString());
                                        deviceName = lst_DeviceNames;
                                        text = "Table Name : " + deviceName + " with Port " + lst_DeviceNames + ", has been deleted successfully.";
                                    }
                                    else
                                    {
                                        var lst_DeviceNames = Crypto.Decrypt(dr["NewValue"].ToString());
                                        text = "Table Name : " + deviceName + " with Port " + lst_DeviceNames + ", has been deleted successfully.";
                                    }
                                }
                                catch (Exception ex)
                                {
                                    txtLogs.Document.Blocks.Add(new Paragraph(new Run(ex.Message + " (" + DateTime.Now.ToString() + ")")));
                                    InsertingLogTextToLogFile(ex.Message + " (" + DateTime.Now.ToString() + ")");
                                    SentrySdk.CaptureException(ex);
                                }
                            }
                            UpdateScreen(text, true, null, null);
                        }
                        clearFields();
                        readXML(false);
                        InitializeAllPortsToSerialPorts(false);
                    }
                    BindComboBoxWithAvailablePorts();
                }
                catch (Exception ex)
                {
                    txtLogs.Document.Blocks.Add(new Paragraph(new Run(ex.Message + " (" + DateTime.Now.ToString() + ")")));
                    InsertingLogTextToLogFile(ex.Message + " (" + DateTime.Now.ToString() + ")");
                    SentrySdk.CaptureException(ex);
                }
            }
        }
        private void btn_CancelEdit_Click(object sender, RoutedEventArgs e)
        {
            using (SentrySdk.Init(conn))
            {
                try
                {
                    txt_DeviceName.Text = "";
                    txt_DevicePort.Text = "";
                    comboBox.SelectedItem = null;
                    btn_AddNewRow.Content = "Add";
                    BindComboBoxWithAvailablePorts();
                }
                catch (Exception ex)
                {
                    txtLogs.Document.Blocks.Add(new Paragraph(new Run(ex.Message + " (" + DateTime.Now.ToString() + ")")));
                    InsertingLogTextToLogFile(ex.Message + " (" + DateTime.Now.ToString() + ")");
                    SentrySdk.CaptureException(ex);
                }
            }
        }

        #endregion

        #region OTHER BUTTONS (SAVE, QUIT, CHANGE PASSWORD) (EVENTS)

        //------------------------------------------ OTHER BUTTONS (SAVE, QUIT, CHANGE PASSWORD) (EVENTS) --------------------------------------------//
        private void btn_Save_Click(object sender, RoutedEventArgs e)
        {
            using (SentrySdk.Init(conn))
            {
                JsonSerializerSettings jss = new JsonSerializerSettings();
                jss.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                MessageBoxResult messageBoxResult = System.Windows.MessageBox.Show("Are you sure to Update?", "Update", System.Windows.MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (messageBoxResult == MessageBoxResult.Yes)
                {
                    var ipAddress = Helper.Crypto.Encrypt(txt_IPAddress.Text);
                    var portNumber = Helper.Crypto.Encrypt(txt_PortNumber.Text);
                    var serverName = Helper.Crypto.Encrypt(txt_ServerName.Text);
                    var databaseName = Helper.Crypto.Encrypt(txt_DatabaseName.Text);
                    var username = Helper.Crypto.Encrypt(txt_Username.Text);
                    var password = Helper.Crypto.Encrypt(txt_Password.Password);
                    var sentryKey = Helper.Crypto.Encrypt("");

                    string Respone = JsonConvert.SerializeObject(allPortData, jss);

                    List<Devices> deviceData = JsonConvert.DeserializeObject<List<Devices>>(Respone);
                    if (deviceData != null)
                    {
                        if (deviceData.Count > 0)
                        {
                            SaveXML(ipAddress, portNumber, serverName, databaseName, username, password, sentryKey, deviceData);
                            CompareXML();
                            vtCommon.dtChanges = vtCommon.CompareData();
                            if (vtCommon.dtChanges != null && vtCommon.dtChanges.Rows.Count > 0)
                            {
                                var deviceName = "";
                                foreach (DataRow dr in vtCommon.dtChanges.Rows)
                                {
                                    try
                                    {
                                        string text = "";
                                        if (dr["OldValue"].ToString() == null || dr["OldValue"].ToString().Length < 1)
                                        {
                                            if (dr["FieldName"].ToString() == "Password")
                                            {
                                                text = "" + dr["FieldName"].ToString() + " : *******, has been added successfully." + DateTime.Now.ToString();
                                            }
                                            else if (dr["FieldName"].ToString() == "DeviceName")
                                            {
                                                // Check New Record is added
                                                var getAllPortsData = allPortData.AsEnumerable().ToList();
                                                var newValueDecrypt = Crypto.Decrypt(dr["NewValue"].ToString());
                                                for (int a = 0; a < getAllPortsData.Count; a++)
                                                {
                                                    var lst_DeviceNames = getAllPortsData[a]["DeviceName"].ToString();
                                                    var lst_PortNames = getAllPortsData[a]["DevicePort"].ToString();
                                                    deviceName = newValueDecrypt;
                                                    if (lst_DeviceNames == newValueDecrypt)
                                                    {
                                                        text = "Table Name : " + lst_DeviceNames + " with Port " + lst_PortNames + ", has been added successfully.";
                                                    }
                                                }
                                            }
                                            else if (dr["FieldName"].ToString() == "DevicePort")
                                            {
                                                var getAllPortsData = allPortData.AsEnumerable().ToList();
                                                var newValueDecrypt = Crypto.Decrypt(dr["NewValue"].ToString());
                                                for (int a = 0; a < getAllPortsData.Count; a++)
                                                {
                                                    var lst_DeviceNames = getAllPortsData[a]["DeviceName"].ToString();
                                                    var lst_PortNames = getAllPortsData[a]["DevicePort"].ToString();
                                                    if (lst_PortNames == newValueDecrypt && lst_DeviceNames == deviceName)
                                                    { }
                                                }
                                            }
                                            else
                                            {
                                                text = "" + dr["FieldName"].ToString() + " : " + Helper.Crypto.Decrypt(dr["NewValue"].ToString()) + ", has been added successfully.";
                                            }
                                        }
                                        else if (dr["NewValue"].ToString() == null || dr["NewValue"].ToString().Length < 1)
                                        {
                                            if (dr["FieldName"].ToString() == "Password")
                                            {
                                                text = "" + dr["FieldName"].ToString() + " has been changed ******* to empty successfully." + DateTime.Now.ToString();
                                            }
                                            else
                                            {
                                                text = "" + dr["FieldName"].ToString() + " has been changed " + Helper.Crypto.Decrypt(dr["OldValue"].ToString()) + " to empty successfully.";
                                            }
                                        }
                                        else
                                        {
                                            if (dr["FieldName"].ToString() == "Password")
                                            {
                                                text = "" + dr["FieldName"].ToString() + " has been changed ******* to ******* successfully" + DateTime.Now.ToString();
                                            }
                                            else
                                            {
                                                text = "" + dr["FieldName"].ToString() + " has been changed " + Helper.Crypto.Decrypt(dr["OldValue"].ToString()) + " to " + Helper.Crypto.Decrypt(dr["NewValue"].ToString()) + " successfully";
                                            }
                                        }
                                        UpdateScreen(text, true, null, null);
                                    }
                                    catch (Exception ex)
                                    {
                                        txtLogs.Document.Blocks.Add(new Paragraph(new Run(ex.Message + " (" + DateTime.Now.ToString() + ")")));
                                        InsertingLogTextToLogFile(ex.Message + " (" + DateTime.Now.ToString() + ")");
                                        SentrySdk.CaptureException(ex);
                                    }
                                }
                            }
                            clearFields();
                            readXML(false);
                            InitializeAllPortsToSerialPorts(false);
                        }
                        else
                        {
                            MessageBox.Show("Please enter Device Name and Device Port in Device Configuration.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            txtLogs.Document.Blocks.Add(new Paragraph(new Run("Please enter Device Name and Device Port in Device Configuration. (" + DateTime.Now.ToString() + ")")));
                            InsertingLogTextToLogFile("Please enter Device Name and Device Port in Device Configuration. (" + DateTime.Now.ToString() + ")");
                            SentrySdk.CaptureMessage("Please enter Device Name and Device Port in Device Configuration. (" + DateTime.Now.ToString() + ")");
                        }
                    }
                    else
                    {
                        MessageBox.Show("Please enter Device Name and Device Port in Device Configuration.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        txtLogs.Document.Blocks.Add(new Paragraph(new Run("Please enter Device Name and Device Port in Device Configuration. (" + DateTime.Now.ToString() + ")")));
                        InsertingLogTextToLogFile("Please enter Device Name and Device Port in Device Configuration. (" + DateTime.Now.ToString() + ")");
                        SentrySdk.CaptureMessage("Please enter Device Name and Device Port in Device Configuration. (" + DateTime.Now.ToString() + ")");
                    }
                }
            }
        }
        private void btn_Quit_Click(object sender, RoutedEventArgs e)
        {
            using (SentrySdk.Init(conn))
            {
                try
                {
                    MessageBoxResult messageBoxResult = System.Windows.MessageBox.Show("Are you sure to logout?", "Exit", System.Windows.MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (messageBoxResult == MessageBoxResult.Yes)
                    {
                        Application.Current.Shutdown();
                    }
                }
                catch (Exception ex)
                {
                    txtLogs.Document.Blocks.Add(new Paragraph(new Run(ex.Message + "(" + DateTime.Now.ToString() + ")")));
                    InsertingLogTextToLogFile(ex.Message + " (" + DateTime.Now.ToString() + ")");
                    SentrySdk.CaptureMessage(ex.Message + " (" + DateTime.Now.ToString() + ")");
                }
            }
        }
        private void btn_ChangePassword_Click(object sender, RoutedEventArgs e)
        {
            using (SentrySdk.Init(conn))
            {
                try
                {
                    win_PasswordChange win_Password = new win_PasswordChange();
                    win_Password.Show();
                }
                catch (Exception ex)
                {
                    txtLogs.Document.Blocks.Add(new Paragraph(new Run(ex.Message + "(" + DateTime.Now.ToString() + ")")));
                    InsertingLogTextToLogFile(ex.Message + " (" + DateTime.Now.ToString() + ")");
                    SentrySdk.CaptureMessage(ex.Message + " (" + DateTime.Now.ToString() + ")");
                }
            }
        }

        #endregion
    }
}
