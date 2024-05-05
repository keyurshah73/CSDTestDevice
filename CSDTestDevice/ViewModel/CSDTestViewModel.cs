using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using CSDTestDevice.LogData;
using CSDTestDevice.Serial;

namespace CSDTestDevice.ViewModel
{

    public abstract class NotifyPropertyChanged : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;


        protected void OnPropertyChanged(string propertyName, object oldvalue = null, object newvalue = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) 
                return false;
            var oldValue = field;
            field = value;
            OnPropertyChanged(propertyName, oldValue, value);
            return true;
        }
    }

    public static class Configuration
    {
        //
        // Summary:
        //     Gets the System.Configuration.AppSettingsSection data for the current application's
        //     default configuration.
        //
        // Returns:
        //     Returns a System.Collections.Specialized.NameValueCollection object that contains
        //     the contents of the System.Configuration.AppSettingsSection object for the current
        //     application's default configuration.
        //
        // Exceptions:
        //   T:System.Configuration.ConfigurationErrorsException:
        //     Could not retrieve a System.Collections.Specialized.NameValueCollection object
        //     with the application settings data.
        public static NameValueCollection AppSettings { get; }
    }

    public class CSDTestViewModel : NotifyPropertyChanged
    {
        private string _currentTime;
        public string CurrentTime
        {
            get => _currentTime;
            set => this.SetField(ref _currentTime, value);
        }

        private string _testProgram;
        public string TestProgram
        {
            get => _testProgram;
            set => this.SetField(ref _testProgram, value);
        }

        private string _testPressure;
        public string TestPressure
        {
            get => _testPressure;
            set => this.SetField(ref _testPressure, value);
        }

        private string _testDecay;
        public string TestDecay
        {
            get => _testDecay;
            set => this.SetField(ref _testDecay, value);
        }
        private string _finalResult;
        public string FinalResult
        {
            get => _finalResult;
            set => this.SetField(ref _finalResult, value);
        }

        private string _fillingPressure;
        public string FillingPressure
        {
            get => _fillingPressure;
            set => this.SetField(ref _fillingPressure, value);
        }

        private int _productionCount;
        public int ProductionCount
        {
            get => _productionCount;
            set => this.SetField(ref _productionCount, value);
        }

        private string _deviceId;
        public string DeviceId
        {
            get => _deviceId;
            set => this.SetField(ref _deviceId, value);
        }

        private string _barcodeData;
        public string BarcodeData
        {
            get => _barcodeData;
            set => this.SetField(ref _barcodeData, value);
        }

        private bool _IsEnabled;
        public bool IsEnabled
        {
            get => _IsEnabled;
            set => this.SetField(ref _IsEnabled, value);
        }


        private bool _IsLicneseValid;
        public bool IsLicneseValid
        {
            get => _IsLicneseValid;
            set => this.SetField(ref _IsLicneseValid, value);
        }
        private bool _IsFileAvailable;
        public bool IsFileAvailable
        {
            get => _IsFileAvailable;
            set => this.SetField(ref _IsFileAvailable, value);
        }

        private ICommand _startCommand;
        public ICommand StartCommand
        {
            get { return _startCommand; }
            set { _startCommand = value; }
        }

        private ICommand _stopCommand;
        public ICommand StopCommand
        {
            get { return _stopCommand; }
            set { _stopCommand = value; }
        }
       // private bool bFileStatus = false;
        private string strComPort;
        private int nBaudrate;
        private int nByteSize;
        private int nDeviceCount = 0;
        private bool IsMonitor = false;
        private string strSerialNumber = string.Empty;
        private string strTestProgram = string.Empty;
        private string strFinalOutcome = string.Empty;
        private string strCurrentTime = string.Empty;
        private string strFillingPressure = string.Empty;
        private string strTestPressure = string.Empty;
        private string strTestDecay = string.Empty;
        private string strTesterNo = string.Empty;

        private double dFillingPressure = 0;
        private double dTestPressure = 0;
        private double dTestDecay = 0;
        private string TesterNo;
        public ICommand WindowLoaded { get; set; }
        public SerialCom _serial = null;
        
        public CSDTestViewModel()
        {
            StartCommand = new RelayCommand<object>(StartAction);
            StopCommand = new RelayCommand<object>(StopAction);
            WindowLoaded = new RelayCommand<object>(OnLoaded);
        }

       
        private bool IsValid()
        {
            bool bReturn = false;
            string strMac = GetMACAddress();
            string fileName = "Licence\\CSDTest.bin";
            if (File.Exists(fileName))
            {
                string LicenceKey = string.Empty;
                using (var stream = File.Open(fileName, FileMode.Open))
                {
                    using (var reader = new BinaryReader(stream, Encoding.ASCII, false))
                    {
                        for (int i = 0; i < 5; i++)
                        {
                            string strDecrypt = reader.ReadString();
                            if (i == 2)
                                LicenceKey = strDecrypt;
                            if(i == 4)
                            {
                                var strSplitDate = strDecrypt.Split('|');

                                DateTime EndDate = Convert.ToDateTime(strSplitDate[0]);
                                DateTime StartDate = DateTime.Now;

                                int Days = (StartDate - EndDate).Days;

                                int LicDay = Convert.ToInt32(strSplitDate[1]);

                                if(LicDay != 0)
                                {
                                    if (LicDay == Days)
                                        return true;
                                }
                            }
                        }
                    }
                }
                string cipherText = LicenceGen(strMac);
                if (string.Compare(cipherText,LicenceKey) == 0)
                    bReturn = false;
                else
                    bReturn = true;
            }
            else
            {
                bReturn = true;
            }
            return bReturn;
        }
        private bool IsConfigFileAvailable()
        {
            bool bReturn = false;
            TesterNo = ConfigurationManager.AppSettings["TesterNo"];
            strComPort = ConfigurationManager.AppSettings["ComPort"];
            nBaudrate = Convert.ToInt32(ConfigurationManager.AppSettings["Baudrate"]);
            nByteSize = Convert.ToInt32(ConfigurationManager.AppSettings["ByteSize"]);

            if(string.IsNullOrEmpty(TesterNo) || string.IsNullOrEmpty(strComPort))
                bReturn = true;
            else
                bReturn = false;
            return bReturn;
        }
        private string GetMACAddress()
        {
            NetworkInterface[] nics = NetworkInterface.GetAllNetworkInterfaces();
            string sMacAddress = string.Empty;
            foreach (NetworkInterface adapter in nics)
            {
                IPInterfaceProperties properties = adapter.GetIPProperties();
                if (adapter.Name == "Ethernet")
                    sMacAddress = adapter.GetPhysicalAddress().ToString();
            }
            return sMacAddress;
        }
        private string LicenceGen(string strMac)
        {
            string strEnc = string.Empty;
            if (!string.IsNullOrEmpty(strMac))
            {
                strMac = strMac.ToUpper();
                for (int i = 0; i < strMac.Length; i++)
                {
                    strEnc += strMac[i] + i;
                }
            }
            return strEnc;
        }
        private void OnLoaded(object obj)
        {
            IsLicneseValid = IsValid();
            IsFileAvailable = IsConfigFileAvailable();
            if(IsFileAvailable == false && IsLicneseValid == false)
            {
                IsEnabled = true;
                if (_serial == null)
                {
                    _serial = new SerialCom(strComPort, nBaudrate, nByteSize);
                    _serial.OnDataReceived -= OnReceivedData;
                    _serial.OnDataReceived += OnReceivedData;
                }
                    DeviceId = TesterNo;
            }
        }
        private void OnReceivedData(string Data)
        {
            nDeviceCount++;
            CurrentTime = TestProgram = TestPressure = TestDecay = FinalResult = FillingPressure = string.Empty;
            System.Globalization.CultureInfo _cultureInfo = new System.Globalization.CultureInfo("en-Us", false);
            strCurrentTime = DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss", _cultureInfo);
            Task.Delay(1000).Wait();
            if(!string.IsNullOrEmpty(BarcodeData))
            {
                CurrentTime = strCurrentTime;
                DataSplit(Data);
                ++ProductionCount;
                string dataCSV = DataCSV();
                dataCSV = dataCSV.Trim();
                dataCSV = dataCSV.Replace('\n',' ');
                dataCSV = dataCSV.Replace('\r', ' ');
                //MessageBox.Show(dataCSV);
                LogAction.LogDebug(dataCSV);
                LogAction.LogCSV(dataCSV);
                nDeviceCount = 0; BarcodeData = string.Empty;
            }
            else
            {
                nDeviceCount = 0;
                _serial.OnDataReceived -= OnReceivedData;
                MessageBox.Show("Barcode data is empty.", "Error");
                LogAction.LogDebug("Barcode data is empty.");
                _serial.OnDataReceived += OnReceivedData;
            }
        }
        private void DataMonitor()
        {
            while(IsMonitor)
            {
                if(nDeviceCount == 1)
                {
                    nDeviceCount =  0;
                }
                Task.Delay(100).Wait();
            }
        }
        //private string GetExecuteQuery()
        //{
        //    try
        //    {
        //        //string strFormat = String.Format("INSERT INTO Data (DateTime, ProgramID) VALUES ({0}, {1})", DateTime.Now.Date, 5);
        //        //return strFormat;
        //    }
        //    catch(Exception ex)
        //    {
        //        System.Windows.MessageBox.Show(ex.Message);
        //    }
        //        return string.Empty;
        //}
        //private string GetConnectionString()
        //{
        //    return "server = 127.0.0.1; uid = root; pwd = root; DataSource = battrixx8";
        //    //return "server=localhost:3306;Database=battrixx8;User ID=root;Password=root;";
        //}
        private void StartAction(object obj)
        {
            try
            {
                //string connectionString = GetConnectionString();
                //string ExecuteQuery = GetExecuteQuery();
                //CreateCommand(ExecuteQuery, connectionString);
                if (_serial == null)
                {
                    _serial = new SerialCom(strComPort, nBaudrate, nByteSize);
                    _serial.OnDataReceived -= OnReceivedData;
                    _serial.OnDataReceived += OnReceivedData;
                }
                if (_serial.OpenPort())
                {
                    //IsMonitor = true;
                    //thDataMoitor = new Thread(DataMonitor)
                    //{
                    //    Priority = ThreadPriority.Normal
                    //};
                    //thDataMoitor.Start();
                    IsEnabled = false;
                }
                else
                {
                    LogAction.LogDebug("Serial port not open. Check hardware connection");
                    MessageBox.Show("Serial port not open. Check hardware connection", "Error");
                }
            }
            catch (Exception ex)
            {
                LogAction.LogDebug("Start Action exception." + ex.Message);
                System.Windows.MessageBox.Show(ex.Message);
            }
        }
        private void StopAction(object obj)
        {
            try
            {
                //IsMonitor = false;
                //if (thDataMoitor.IsAlive)
                //    thDataMoitor.Abort();
                //thDataMoitor = null;

                if (_serial.IsOpen())
                {
                    _serial.OnDataReceived -= OnReceivedData;
                    _serial.ClosePort();
                }
                CurrentTime = TestProgram = TestPressure = TestDecay = FinalResult = FillingPressure = string.Empty;
                BarcodeData = string.Empty;
                IsEnabled = true;
            }
            catch (Exception ex)
            {
                LogAction.LogDebug("Stop Action exception." + ex.Message);
                MessageBox.Show(ex.Message);
            }
        }
        private void DataSplit(string strSerialData)
        {
            try
            {
                string[] str = strSerialData.Split('\n');
                for (int i = 0; i < str.Length; i++)
                {
                    switch (i)
                    {
                        case 0:
                            {
                                if (str[i].Contains(''))
                                {
                                    strSerialNumber = str[i].Replace("", "");
                                }
                                break;
                            }
                        case 1:
                            {
                                if (str[i].Contains("Test program"))
                                {
                                    var strData = str[i].Split(':');
                                    strTestProgram = strData[1].Trim(' ').ToString();
                                    strTestProgram = strTestProgram.Replace("\n", "");
                                    TestProgram = strTestProgram;
                                }
                                break;
                            }
                        case 2:
                            {
                                if (str[i].Contains("Test pressure"))
                                {
                                    var strData = str[i].Split(':');
                                    strData[1] = strData[1].Trim();
                                    TestPressure = strData[1];
                                    strTestPressure = strData[1] = strData[1].Replace("[mbar]", "");
                                    dTestPressure = Convert.ToDouble(strData[1].ToString());
                                }
                                break;
                            }
                        case 3:
                            {
                                if (str[i].Contains("Test decay"))
                                {
                                    var strData = str[i].Split(':');
                                    strData[1] = strData[1].Trim();
                                    TestDecay = strData[1];
                                    strTestDecay = strData[1] = strData[1].Replace("[mbar]", "");
                                    dTestDecay = Convert.ToDouble(strData[1].ToString());
                                }
                                break;
                            }
                        case 4:
                            {
                                if (str[i].Contains("Final outcome"))
                                {
                                    var strData = str[i].Split(':');
                                    if (strData[1].Contains("Good"))
                                        strFinalOutcome = "Good";
                                    else
                                        strFinalOutcome = "Bad";
                                    FinalResult = strFinalOutcome;
                                }
                                break;
                            }
                        case 5:
                            {
                                if (str[i].Contains("Filling press set"))
                                {
                                    var strData = str[i].Split(':');
                                    strData[1] = strData[1].Trim();
                                    FillingPressure = strData[1];
                                    strFillingPressure = strData [1] = strData[1].Replace("[mbar]", "");
                                    dFillingPressure = Convert.ToDouble(strData[1].ToString());
                                }
                                break;
                            }
                        default:
                            break;
                    }
                }

                
            }
            catch (Exception e)
            {
                LogAction.LogDebug("Data split time exception occure. " + e.Message);
            }
        }

        private string DataCSV()
        {
            //StringBuilder sbStoreCSV = new StringBuilder();
            //sbStoreCSV.Append(strCurrentTime + ", ");
            //sbStoreCSV.Append(BarcodeData + ", ");
            //sbStoreCSV.Append(strTestProgram + ", ");
            //sbStoreCSV.Append(strTestPressure + ", ");
            //sbStoreCSV.Append(strFillingPressure + ", ");
            //sbStoreCSV.Append(strTestDecay + ", ");
            //sbStoreCSV.Append(strFinalOutcome + ", ");
            //sbStoreCSV.Append(TesterNo + ",");

            return (strCurrentTime + "," + BarcodeData + "," + strTestProgram + "," + strTestPressure + "," + strFillingPressure + "," + strTestDecay + "," + strFinalOutcome + "," + TesterNo);

            //return sbStoreCSV.ToString();
        }
        //private static void CreateCommand(string queryString, string connectionString)
        //{
        //    using (SqlConnection connection = new SqlConnection(connectionString))
        //    {
        //        SqlCommand command = new SqlCommand(queryString, connection);
        //        command.Connection.Open();
        //        command.ExecuteNonQuery();
        //        command.Connection.Close();
        //    }
        //}
    }
}
