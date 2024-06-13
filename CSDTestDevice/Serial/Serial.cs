using CSDTestDevice.LogData;
using System;
using System.IO.Ports;
using System.Threading.Tasks;

namespace CSDTestDevice.Serial
{
    public class SerialCom
    {
        private SerialPort serialPort = null;
        public string errorMessage {get; set;}

        public delegate void DataReceived(string Data);
        public event DataReceived OnDataReceived;

        public SerialCom(string strComPort,int baudRate, int byteSize)
        {
            serialPort = new SerialPort()
            {
                PortName  = strComPort,
                BaudRate  = baudRate,
                DataBits  = byteSize,
                Parity    = Parity.None,
                StopBits  = StopBits.One,
                Handshake = Handshake.None
            };
        }

        public bool OpenPort()
        {
            bool bIsSucess;
            if (serialPort.IsOpen)
            {
                serialPort.Close();
                serialPort.Dispose();
            }
            try
            {
                serialPort.Open();
                bIsSucess = serialPort.IsOpen;
                if (!bIsSucess)
                    errorMessage = "Serial port connection error.";
                else
                    this.serialPort.DataReceived += new SerialDataReceivedEventHandler(OnReceived);
            }
            catch (Exception e)
            {
                bIsSucess = false;
                errorMessage = "exception occure while connecting device." + e.Message;
            }
            return bIsSucess;
        }

        public bool IsOpen()
        {
            return serialPort.IsOpen;
        }

        private void OnReceived(object sender, SerialDataReceivedEventArgs e)
        {
            Task.Delay(200).Wait();
            string strReceivedData = serialPort.ReadExisting();
            //LogAction.LogDebug("Serial Data : "+ strReceivedData);
            OnDataReceived?.BeginInvoke(strReceivedData, null, null);
            return;
        }

        public bool ClosePort()
        {
            bool bIsSucess = true;
            try
            {
                if (serialPort.IsOpen)
                {
                    serialPort.Close();
                    serialPort.Dispose();
                }
            }
            catch(Exception e)
            {
                bIsSucess = false;
                errorMessage = "exception occure while close device." + e.Message;
            }
            return bIsSucess;
        }
        
    }
}
