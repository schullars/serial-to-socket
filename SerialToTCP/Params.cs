using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

// add by schullar
using System.IO.Ports;
using System.Windows.Media;
using System.Threading;

namespace SerialToTCP
{
    class Params
    {
    }

    public enum Protocol
    {
        UDP = 1001,
        TCP_Client,
        TCP_Server,
    }

    public enum UDPType
    {
        Unicast = 1101,
        Broadcast,
    }
    public class NetParams:System.ComponentModel.INotifyPropertyChanged
    {
        #region // InotifyPropertyChanged member
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(System.ComponentModel.PropertyChangedEventArgs e)
        {
            if(null != PropertyChanged)
            {
                PropertyChanged(this, e);
            }
        }
        #endregion

        private Protocol _protocol=Protocol.UDP;
        private string _targetIp;
        private int _targetPort=3100;
        private string _localIp;
        private int _localPort=3000;
        public UDPType udpType = UDPType.Unicast;// 单播，广播选择
        public bool autostart;


        public Protocol protocol { get { return _protocol; } set { _protocol = value; OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs("protocol")); } }
        public string targetIp { get { return _targetIp; } set { _targetIp = value; OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs("targetIp")); } }
        public int targetPort { get { return _targetPort; } set { _targetPort = value; OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs("targetPort")); } }
        public string localIp { get { return _localIp; } set { _localIp = value; OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs("localIp")); } }
        public int localPort { get { return _localPort; } set { _localPort = value; OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs("localPort")); } }
    }

    public class SerialParams:System.ComponentModel.INotifyPropertyChanged
    {
        #region // InotifyPropertyChanged member
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (null != PropertyChanged)
            {
                PropertyChanged(this, e);
            }
        }
        #endregion

        private string _comNum;
        private int _baud;
        private Parity _parity;
        private int _databits;
        private StopBits _stopbits;
        public bool autostart;

        public string comNum { get { return _comNum; } set { _comNum = value; OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs("comNum")); } }
        public int baud { get { return _baud; } set { _baud = value; OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs("baud")); } }
        public Parity parity { get { return _parity; } set { _parity = value; OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs("parity")); } }
        public int databits { get { return _databits; } set { _databits = value; OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs("databits")); } }
        public StopBits stopbits { get { return _stopbits; } set { _stopbits = value; OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs("stopbits")); } }
    }

    // 串口和网口互发数据是的中间缓存
    class DataTransForm : System.ComponentModel.INotifyPropertyChanged
    {
        #region // InotifyPropertyChanged member
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (null != PropertyChanged)
            {
                PropertyChanged(this, e);
            }
        }
        #endregion
        private int Limit = 50000;// 上限50000个字符

        private string _DataSocket="";// 用于绑定界面显示
        private string _DataSerial="";// 用于绑定界面显示

        public string _DataFromSerial="";// 用于暂存从串口收到的数据
        public string _DataFromSocket="";// 用于暂存从网口收到的数据

        public ReaderWriterLock DataFromSerial_Lock = new ReaderWriterLock();
        public ReaderWriterLock DataFromSocket_Lock = new ReaderWriterLock();


        public string DataSocket { get { return _DataSocket; } set { if (null != _DataSocket && Limit < _DataSocket.Length) { _DataSocket = ""; } _DataSocket = value; OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs("DataSocket")); } }
        public string DataSerial { get { return _DataSerial; } set { if (null != _DataSerial && Limit < _DataSerial.Length) { _DataSerial = ""; } _DataSerial = value; OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs("DataSerial")); } }
        public string DataFromSerial { get { return _DataFromSerial; } set { if (null != _DataFromSerial && Limit < _DataFromSerial.Length) { _DataFromSerial = ""; } _DataFromSerial = value; OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs("DataFromSerial")); } }
        public string DataFromSocket { get { return _DataFromSocket; } set { if (null != _DataFromSocket && Limit < _DataFromSocket.Length) { _DataFromSocket = ""; } _DataFromSocket = value; OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs("DataFromSocket")); } }

    }

    /// <summary>
    /// 用于获取串口列表时给串口排序
    /// </summary>
    class ComString:IComparable<ComString>
    {
        public string comNum { get; set; }
        public int CompareTo(ComString comStr)// 自定义排序功能
        {
            int result;

            if(null == comStr || null == comStr.comNum || 4 > comStr.comNum.Length){return 0;}
            
            if(!string.Equals(comStr.comNum.Remove(3),"com",StringComparison.CurrentCultureIgnoreCase)){return 0;}

            try
            {
                int local = int.Parse(this.comNum.Remove(0,3));
                int param = int.Parse(comStr.comNum.Remove(0,3));

                if( local == param){result = 0;}
                else if (local > param) { result = 1; }
                else { result = -1; }
            }
            catch { return 0; }

            return result;

        }
    }

    /// <summary>
    /// 界面元素绑定变量
    /// </summary>
    class MyUIElement : System.ComponentModel.INotifyPropertyChanged
    {
        #region // InotifyPropertyChanged member
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (null != PropertyChanged)
            {
                PropertyChanged(this, e);
            }
        }
        #endregion

        private SolidColorBrush _serialbgcolor;// 串口打开按钮背景色
        private SolidColorBrush _socketbgcolor;// 网口打开按钮背景色

        private string _serialbtntext;// 串口按钮字符串
        private string _socketbtntext;// 网口按钮字符串

        private bool _serialControlEnable = true;
        private bool _socketControlEnable = true;

        public SolidColorBrush serialbgcolor { get { return _serialbgcolor; } set { _serialbgcolor = value; OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs("serialbgcolor")); } }
        public SolidColorBrush socketbgcolor { get { return _socketbgcolor; } set { _socketbgcolor = value; OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs("socketbgcolor")); } }
        public string serialbtntext { get { return _serialbtntext; } set { _serialbtntext = value; OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs("serialbtntext")); } }
        public string socketbtntext { get { return _socketbtntext; } set { _socketbtntext = value; OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs("socketbtntext")); } }

        public bool serialControlEnable { get { return _serialControlEnable; } set { _serialControlEnable = value; OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs("serialControlEnable")); } }
        public bool socketControlEnable { get { return _socketControlEnable; } set { _socketControlEnable = value; OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs("socketControlEnable")); } }
    }
}
