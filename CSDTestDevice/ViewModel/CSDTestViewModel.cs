using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Configuration;
using System.Data.SqlClient;
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

    public class CSDTestViewModel : NotifyPropertyChanged
    {
        #region property
        private string _lastBarcode;
        public string LastBarcode
        {
            get => _lastBarcode;
            set => this.SetField(ref _lastBarcode, value);
        }
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
        #endregion

        #region command
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
        public ICommand WindowLoaded { get; set; }
        #endregion

        #region private variable

        private int nBaudrate;
        private int nByteSize;
        private string strComPort;

        private string strConnectionString = string.Empty;
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

        private SerialCom _serial = null;

        #endregion
        public CSDTestViewModel()
        {
            StartCommand = new RelayCommand<object>(StartAction);
            StopCommand = new RelayCommand<object>(StopAction);
            WindowLoaded = new RelayCommand<object>(OnLoaded);
        }
        private bool IsValid()
        {
            bool bReturn;
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
                                    if (LicDay <= Days)
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
            bool bReturn;
            strTesterNo = ConfigurationManager.AppSettings["TesterNo"];
            strComPort = ConfigurationManager.AppSettings["ComPort"];
            nBaudrate = Convert.ToInt32(ConfigurationManager.AppSettings["Baudrate"]);
            nByteSize = Convert.ToInt32(ConfigurationManager.AppSettings["ByteSize"]);
            strConnectionString = ConfigurationManager.AppSettings["ConnectionString"];

            if (string.IsNullOrEmpty(strTesterNo) || string.IsNullOrEmpty(strComPort))
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
                if (adapter.Name == "LAN")
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
                    DeviceId = strTesterNo;
            }
        }
        private void OnReceivedData(string Data)
        {
            ClearProperty();
            System.Globalization.CultureInfo _cultureInfo = new System.Globalization.CultureInfo("en-Us", false);
            DateTime DT = DateTime.Now;
            strCurrentTime = DT.ToString("dd-MM-yyyy HH:mm:ss", _cultureInfo);
            Task.Delay(1500).Wait();
            if(!string.IsNullOrEmpty(BarcodeData))
            {
                CurrentTime = strCurrentTime;
                DataSplit(Data);
                ++ProductionCount;
                string dataCSV = DataCSV();
                //LogAction.LogDebug(dataCSV);
                LogAction.LogCSV(dataCSV);
                LastBarcode = BarcodeData;
                LoadDataInDB("INSERT INTO txn_battery_ip_test(Battery_barcode,ScannedDate,PROGRAM_NO,FILLING_PRESSURE,BUILDUP_PRESSURE,TEST_DECAY,RESULT,TESTER_NO) "
                    + "VALUES('" + BarcodeData + "','" + DT.ToString("MM-dd-yyyy HH:mm:ss", _cultureInfo) + "','" + strTestProgram + "','" + dFillingPressure + "','" + dTestPressure + "','" + dTestDecay + "','" + strFinalOutcome + "','" + DeviceId + "')");
                BarcodeData = string.Empty;
            }
            else
            {
                _serial.OnDataReceived -= OnReceivedData;
                MessageBox.Show("Barcode data field is empty.", "ERROR");
                LogAction.LogDebug("Barcode data field is empty.");
                _serial.OnDataReceived += OnReceivedData;
            }
        }
        private void StartAction(object obj)
        {
            try
            {
                if (_serial == null)
                {
                    _serial = new SerialCom(strComPort, nBaudrate, nByteSize);
                    _serial.OnDataReceived -= OnReceivedData;
                    _serial.OnDataReceived += OnReceivedData;
                }
                if (_serial.OpenPort())
                {
                    IsEnabled = false;
                }
                else
                {
                    LogAction.LogDebug("Serial port not open. Check hardware connection");
                    MessageBox.Show("Serial port not open. Check hardware connection", "ERROR");
                }
            }
            catch (Exception ex)
            {
                LogAction.LogDebug("While calling start action Issue is generated." + ex.Message);
                MessageBox.Show(ex.Message,"ERROR");
            }
        }
        private void StopAction(object obj)
        {
            try
            {
                IsEnabled = true;
                if (_serial.IsOpen())
                {
                    _serial.OnDataReceived -= OnReceivedData;
                    _serial.ClosePort();
                }
                ClearProperty();
                BarcodeData = string.Empty;
                ProductionCount = 0;
            }
            catch (Exception ex)
            {
                LogAction.LogDebug("While calling stop action Issue is generated." + ex.Message);
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
                                    string strSerialNumber = str[i].Replace("", "");
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
            string strReturn = strCurrentTime + "," + BarcodeData + "," + strTestProgram + "," + strTestPressure + "," + strFillingPressure + "," + strTestDecay + "," + strFinalOutcome + "," + strTesterNo;
            strReturn = strReturn.Trim();
            strReturn = strReturn.Replace("\n", "");
            strReturn = strReturn.Replace("\r", "");
            return strReturn;
        }
        private bool LoadDataInDB(string Query)
        {
            SqlConnection _SqlConnection = new SqlConnection();
            SqlCommand _SqlCommand = new SqlCommand();
            bool _IsLoad = false;

            _SqlConnection.ConnectionString = strConnectionString;

            try
            {
                _SqlConnection.Open();
            }
            catch (Exception ex)
            {
                LogAction.LogDebug("While Calling IsOpen Issue Is generated.Connection String is " + strConnectionString + Environment.NewLine + ex.Message);
                MessageBox.Show("While Calling IsOpen Issue Is generated.Connection String is " + strConnectionString + Environment.NewLine + ex.Message, "DBERROR");
            }

            try
            {
                if (_SqlConnection.State == System.Data.ConnectionState.Open)
                {
                    _SqlCommand.CommandText = Query;
                    _SqlCommand.Connection = _SqlConnection;

                    _SqlCommand.ExecuteNonQuery();
                    _IsLoad = true;
                    _SqlConnection.Close();
                }
                else
                {
                    LogAction.LogDebug("Database not open.");
                    MessageBox.Show("Database not open.","DBERROR");
                }
                      
                _SqlCommand.Dispose();
                _SqlConnection.Dispose();
            }
            catch(Exception ex)
            {
                LogAction.LogDebug("While Calling Data Insert Issue Is generated." + Environment.NewLine + ex.Message);
                MessageBox.Show("While Calling Data Insert Issue Is generated." + Environment.NewLine + ex.Message, "DBERROR");
            }
           

            return _IsLoad;
        }

        private void ClearProperty()
        {
            LastBarcode = CurrentTime = TestProgram = TestPressure = TestDecay = FinalResult = FillingPressure = string.Empty;
        }
        //private bool LoadInDatabase()
        //{
        //    try
        //    {
        //        using (SqlConnection connection = new SqlConnection(strConnectionString))
        //        {
        //            string strQueryString = "INSERT INTO data (barcode,programmeNo,testPressure,testDecay,fillingPressure,outcome) "
        //                + "VALUES (@barcode,@programmeNo,@testPressure,@testDecay,@fillingPressure,@outcome)";
        //            connection.Open();
        //            using (SqlCommand command = new SqlCommand(strQueryString, connection))
        //            {
        //                command.Parameters.AddWithValue("@barcode", BarcodeData);
        //                command.Parameters.AddWithValue("@programmeNo", strTestProgram);
        //                command.Parameters.AddWithValue("@testPressure", dTestPressure);
        //                command.Parameters.AddWithValue("@testDecay", dTestDecay);
        //                command.Parameters.AddWithValue("@fillingPressure", dFillingPressure);
        //                command.Parameters.AddWithValue("@outcome", strFinalOutcome);
        //                command.ExecuteNonQuery();
        //            }
        //            connection.Close();
        //        }
        //        return true;
        //    }
        //    catch (Exception ex)
        //    {
        //        LogAction.LogDebug("Load data in database." + ex.Message);
        //        MessageBox.Show(ex.Message);
        //        return false;
        //    }
        //}
    }
}