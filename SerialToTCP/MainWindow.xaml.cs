using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

// add by schullar
using SerialToTCP.Util;
//using System.Reflection;
using System.IO.Ports;
using System.Net.Sockets;
using System.Threading;

namespace SerialToTCP
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private DataTransForm dataTransForm;

        public MainWindow()
        {
            MySettings.Default = new MySettings();// 读取配置

            try { Thread.Sleep((MySettings.Default.myGlobalInfo.delay * 1000) > int.MaxValue ? int.MaxValue : (MySettings.Default.myGlobalInfo.delay * 1000)); }// 延时启动
            catch (Exception ex) { MessageBox.Show(ex.Message); }

            // 缩放
            WindowBehaviorHelper wh = new WindowBehaviorHelper(this);
            wh.RepairBehavior();

            InitializeComponent();
            this.maingrid.AddHandler(Button.ClickEvent, new RoutedEventHandler(this.GridButtonClicked));
            this.MouseLeftButtonDown += new System.Windows.Input.MouseButtonEventHandler(Drag_Window);

            InitialTray();// 托盘图标

            InitComParams();
            InitSocketParams();

            if (MySettings.Default.myGlobalInfo.serialParams.autostart) { this.OpenCloseCom(); }// 是否自动连接
            if (MySettings.Default.myGlobalInfo.netParams.autostart) { this.OpenCloseSocket(); }

            this.DataContext = this;

            StartUIElementInspect();

            if (MySettings.Default.myGlobalInfo.hidenWindow) { this.Visibility = System.Windows.Visibility.Hidden; }
        }




        private void GridButtonClicked(object sender, RoutedEventArgs e)
        {
            var btn = e.OriginalSource as Button;

            if (null == btn || null == btn.Name) { return; }

            switch (btn.Name)
            {

                case "maxmum":
                    if (this.WindowState.Equals(System.Windows.WindowState.Normal))
                    {
                        this.WindowState = System.Windows.WindowState.Maximized;
                    }
                    else
                    {
                        this.WindowState = System.Windows.WindowState.Normal;
                    }
                    break;

                case "minmum":
                    this.WindowState = System.Windows.WindowState.Minimized;
                    break;
                case "close":
                    this.Close();
                    break;

                case "openCom":
                    OpenCloseCom();
                    break;

                case "openSocket":
                    OpenCloseSocket();
                    break;

                case "clearComDisp":
                    if (null != this.dataTransForm && null != this.dataTransForm.DataSerial) { this.dataTransForm.DataSerial = ""; }
                    break;
                case "clearSocketDisp":
                    if (null != this.dataTransForm && null != this.dataTransForm.DataSocket) { this.dataTransForm.DataSocket = ""; }
                    break;
                default:
                    break;
            }
        }

        void Drag_Window(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        List<string> comList;// 用于绑定到comNum combobox
        List<int> baudList;// 用于绑定到baud combobox
        List<int> databitsList;// 用于绑定到databits combobox
        List<StopBits> stopbitsList; // 用于绑定到stopbits combobox
        List<Parity> checkbitsList; // 用于绑定到checkbits combobox

        private void InitComParams()
        {
            #region // start init comNum combobox display
            if (null == MySettings.Default.myGlobalInfo.serialParams) { MySettings.Default.myGlobalInfo.serialParams = new SerialParams(); }

            List<string> comList_tmp = System.IO.Ports.SerialPort.GetPortNames().ToList();

            if (null == comList_tmp) { comList_tmp = new List<string>(); }

            if (null == comList) { comList = new List<string>(); }
            comList.Clear();
            List<ComString> comstrList = new List<ComString>();

            foreach (string com in comList_tmp)// 过滤掉232以外的串口
            {
                if (null != com && 3 < com.Length && com.Remove(3).Equals("com",StringComparison.CurrentCultureIgnoreCase)) 
                {
                    comstrList.Add(new ComString { comNum = com });
                }

            }
            comstrList.Sort();// 调用自定义排序功能对串口排序
            foreach (ComString comstr in comstrList) { comList.Add(comstr.comNum); }

            this.comNum_combo.ItemsSource = this.comList;
            //this.comNum_combo.SelectedIndex = this.comNum_combo.Items.Count > 0 ? 0 : -1;// 默认值
            #endregion

            #region // start init baud combobox display
            if (null == baudList) { baudList = new List<int>(); }
            baudList.Clear();

            baudList.Add(1200);
            baudList.Add(2400);
            baudList.Add(4800);
            baudList.Add(9600);
            baudList.Add(19200);
            baudList.Add(38400);
            baudList.Add(57600);
            baudList.Add(115200);
            baudList.Add(204800);
            baudList.Add(230400);
            baudList.Add(460800);
            baudList.Add(921600);

            //this.baud_combo.DataContext = this.baudList;
            this.baud_combo.ItemsSource = this.baudList;
            //this.baud_combo.SelectedIndex = this.baud_combo.Items.Count > 0 ? 0 : -1;// 默认值                
            #endregion

            #region // start init databits combobox display
            if (null == databitsList) { databitsList = new List<int>(); }
            databitsList.Clear();

            databitsList.Add(5);
            databitsList.Add(6);
            databitsList.Add(7);
            databitsList.Add(8);

           // this.databit_combo.DataContext = this.databitsList;
            this.databit_combo.ItemsSource = this.databitsList;
            //this.databit_combo.SelectedIndex = this.databit_combo.Items.Count > 0 ? 0 : -1;// 默认值

            #endregion

            #region // start init stopbits combobox display
            if (null == stopbitsList) { stopbitsList = new List<StopBits>(); }
            stopbitsList.Clear();

            stopbitsList.Add(StopBits.None);
            stopbitsList.Add(StopBits.One);
            stopbitsList.Add(StopBits.OnePointFive);
            stopbitsList.Add(StopBits.Two);

            //this.stopbit_combo.DataContext = this.stopbitsList;
            this.stopbit_combo.ItemsSource = this.stopbitsList;
            //this.stopbit_combo.SelectedIndex = this.stopbit_combo.Items.Count > 0 ? 0 : -1;// 设置默认值
            #endregion

            #region // start init checkbits combobox display
            if (null == checkbitsList) { checkbitsList = new List<Parity>(); }
            checkbitsList.Clear();

            checkbitsList.Add(Parity.None);
            checkbitsList.Add(Parity.Odd);
            checkbitsList.Add(Parity.Even);
            checkbitsList.Add(Parity.Mark);
            checkbitsList.Add(Parity.Space);

            //this.checkbit_combo.DataContext = this.checkbitsList;
            this.checkbit_combo.ItemsSource = this.checkbitsList;
            //this.checkbit_combo.SelectedIndex = this.checkbit_combo.Items.Count > 0 ? 0 : -1;// 设置默认值
            #endregion


            #region // 设置最外层datacontext
            this.DataContext = this;

            // 此绑定用于将显示的参数回传给变量，方便存储
            this.comNum_combo.DataContext = MySettings.Default.myGlobalInfo.serialParams;
            this.baud_combo.DataContext = MySettings.Default.myGlobalInfo.serialParams;
            this.checkbit_combo.DataContext = MySettings.Default.myGlobalInfo.serialParams;
            this.databit_combo.DataContext = MySettings.Default.myGlobalInfo.serialParams;
            this.stopbit_combo.DataContext = MySettings.Default.myGlobalInfo.serialParams;
            #endregion


            // init comdata display
            if (null == dataTransForm) { dataTransForm = new DataTransForm(); }
            this.comData_textblock.DataContext = dataTransForm;


            // 默认显示参数
            if (null != MySettings.Default.myGlobalInfo.serialParams && null == MySettings.Default.myGlobalInfo.serialParams.comNum) 
            {
                try
                {
                    MySettings.Default.myGlobalInfo.serialParams.comNum = this.comList.ElementAt(0);
                    MySettings.Default.myGlobalInfo.serialParams.baud = baudList.ElementAt(3);
                    MySettings.Default.myGlobalInfo.serialParams.parity = Parity.None;
                    MySettings.Default.myGlobalInfo.serialParams.databits = this.databitsList.ElementAt(3);
                    MySettings.Default.myGlobalInfo.serialParams.stopbits = StopBits.One;
                }
                catch(Exception ex) { MessageBox.Show(ex.Message); }

            }
        }

        /// <summary>
        /// 支持标题栏双击放大缩小窗口
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void softwareTitle_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (null == sender) { return; }

            var obj = sender as Label;
            if (null == obj || null == obj.Name || "softwareTitle" != obj.Name) { return; }

            if (System.Windows.WindowState.Maximized == this.WindowState) { this.WindowState = System.Windows.WindowState.Normal; }
            else if (System.Windows.WindowState.Normal == this.WindowState) { this.WindowState = System.Windows.WindowState.Maximized; }
        }


        System.IO.Ports.SerialPort mySerialPort;
        /// <summary>
        /// 打开关闭串口
        /// </summary>
        private void OpenCloseCom()
        {
            if (null == mySerialPort) { mySerialPort = new System.IO.Ports.SerialPort(); }

            if (mySerialPort.IsOpen) { mySerialPort.Close(); goto COM_END; }

            if (null == MySettings.Default.myGlobalInfo.serialParams && null == MySettings.Default.myGlobalInfo.serialParams.comNum) { MessageBox.Show("串口参数不能为空！"); }
            try
            {
                mySerialPort.PortName = MySettings.Default.myGlobalInfo.serialParams.comNum;
                mySerialPort.BaudRate = MySettings.Default.myGlobalInfo.serialParams.baud;
                mySerialPort.Parity = MySettings.Default.myGlobalInfo.serialParams.parity;
                mySerialPort.StopBits = MySettings.Default.myGlobalInfo.serialParams.stopbits;
                mySerialPort.DataBits = MySettings.Default.myGlobalInfo.serialParams.databits;
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }


           // mySerialPort.DataReceived += new SerialDataReceivedEventHandler(this.myDataReceived);

            try
            {
                if (!mySerialPort.IsOpen) { mySerialPort.Open(); }
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }

            this.StartSerial();

            MySettings.Default.SaveDataToXml(MySettings.Default.myGlobalInfo);

        COM_END:
            //if (mySerialPort.IsOpen) { this.openCom.Background = new SolidColorBrush(Colors.Green); this.openCom.Content = "关闭串口"; }
            //else { this.openCom.Background = new SolidColorBrush(Colors.DodgerBlue); this.openCom.Content = "打开串口"; }

            return;
        }

        bool hasdata = false;// 线程循环扫描串口时，容易在字符接收过程中读取缓存，将完整的字符串截断，增加此项，在第一次检测到有数据时，置位此位，并等到第二次扫描串口时读取缓存
        /// <summary>
        /// 从串口接收数据
        /// </summary>
        /// <returns></returns>
        private string RecvSerialData()
        {
            string str = "";
            
           try
            {
                int n = mySerialPort.BytesToRead;

                if (n <= 0) { return str; }

                if (!hasdata) { hasdata = true; return str; }// 线程循环扫描串口时，容易在字符接收过程中读取缓存，将完整的字符串截断，增加此项，在第一次检测到有数据时，置位此位，并等到第二次扫描串口时读取缓存
                hasdata = false;

                byte[] buf = new byte[n];

                int len = mySerialPort.Read(buf, 0, n);

                str = Encoding.ASCII.GetString(buf,0,len);
            }
           catch (Exception ex) { return ""; }

           return str;

        }

        /// <summary>
        /// 向串口发送数据
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private int SendSerialData(string data)
        {
            if (null == data || 0 >= data.Length || !mySerialPort.IsOpen) { return 0; }

            string str = "";

            byte[] sendBytes = new byte[data.Length];
            sendBytes = Encoding.ASCII.GetBytes(data);
            try {mySerialPort.Write(sendBytes,0,sendBytes.Length); }
            catch { return 0; }

            return sendBytes.Length;
        }

        MySocket mySocket;

        /// <summary>
        /// 初始化网口相关数据绑定
        /// </summary>
        private void InitSocketParams()
        {
            if (null == MySettings.Default.myGlobalInfo.netParams) { MySettings.Default.myGlobalInfo.netParams = new NetParams(); }


            this.localPort_text.DataContext = MySettings.Default.myGlobalInfo.netParams;
            this.localip_text.DataContext = MySettings.Default.myGlobalInfo.netParams;
            this.tartetPort_text.DataContext = MySettings.Default.myGlobalInfo.netParams;
            this.targetip_text.DataContext = MySettings.Default.myGlobalInfo.netParams;
            this.protocol_combo.DataContext = MySettings.Default.myGlobalInfo.netParams;

            this.socketData_textblock.DataContext = this.dataTransForm;

            // 设置默认值
            //MySettings.Default.myGlobalInfo.netParams.protocol = Protocol.TCP_Client;
            //MySettings.Default.myGlobalInfo.netParams.targetIp = "192.168.3.120";
            //MySettings.Default.myGlobalInfo.netParams.targetPort = 3000;
            //MySettings.Default.myGlobalInfo.netParams.localIp = "192.168.3.120";
            //MySettings.Default.myGlobalInfo.netParams.localPort = 3100;
            //MySettings.Default.myGlobalInfo.netParams.udpType = UDPType.Broadcast;
        }

        private void OpenCloseSocket()
        {
            if (null == MySettings.Default.myGlobalInfo.netParams) { MySettings.Default.myGlobalInfo.netParams = new NetParams(); }

            if (null == mySocket) { mySocket = new MySocket(); }

            if (mySocket.IsConnected() != false) { mySocket.CloseSocket(); goto SOCKET_END; }

            if (string.IsNullOrWhiteSpace(MySettings.Default.myGlobalInfo.netParams.targetIp) || MySettings.Default.myGlobalInfo.netParams.targetPort <= 0 ||
                string.IsNullOrWhiteSpace(MySettings.Default.myGlobalInfo.netParams.localIp) || MySettings.Default.myGlobalInfo.netParams.localPort <= 0 ||
                !System.Text.RegularExpressions.Regex.IsMatch(MySettings.Default.myGlobalInfo.netParams.targetIp, @"^((\d{1,2}|1\d\d|2[0-4]\d|25[0-5])\.){3}(\d{1,2}|1\d\d|2[0-4]\d|25[0-5])$") ||
                !System.Text.RegularExpressions.Regex.IsMatch(MySettings.Default.myGlobalInfo.netParams.localIp, @"^((\d{1,2}|1\d\d|2[0-4]\d|25[0-5])\.){3}(\d{1,2}|1\d\d|2[0-4]\d|25[0-5])$")// 检测ip是否合法
                ) { MessageBox.Show("网络参数设置错误，请检查参数。"); return; }

            if (null == mySocket) { mySocket = new MySocket(); }

            mySocket.netParams = MySettings.Default.myGlobalInfo.netParams;
            try
            {
                mySocket.InitSocket();// 初始化socket连接
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }

            this.StartSocket();// 启动数据收发线程
            MySettings.Default.SaveDataToXml(MySettings.Default.myGlobalInfo);

        SOCKET_END:

            return;
 
        }

        /// <summary>
        /// 开启网口收发线程
        /// </summary>
        private void StartSocket()
        {
            if (this.sockeRunFlag) { return; }

            this.sockeRunFlag = true;

            Thread thread = new Thread(new ThreadStart(SocketThread));
            try { thread.Start(); }
            catch { }

        }

        private bool sockeRunFlag = false;
        private void SocketThread()// socket 收发线程
        {
            int sendCount = 0;
            int recvCount = 0;
            string str = "";

            while (sockeRunFlag)
            {
                try
                {
                    if (null == mySocket) { sockeRunFlag = false; continue; }// 实例是否为空

                    if (mySocket.IsConnected() != true) { continue; }// socket 是否已连接

                    if (null != dataTransForm && null != dataTransForm.DataFromSerial && 0 < dataTransForm.DataFromSerial.Length)// 发送数据到socket
                    {
                        try
                        {
                            dataTransForm.DataFromSerial_Lock.AcquireWriterLock(100);// 100ms超时

                            sendCount = mySocket.SocketSendData(dataTransForm.DataFromSerial);
                            dataTransForm.DataSocket += "send to socket:\r\n" + dataTransForm.DataFromSerial.Substring(0, sendCount) + "\r\n";
                            dataTransForm.DataFromSerial = dataTransForm.DataFromSerial.Remove(0, sendCount);

                            dataTransForm.DataFromSerial_Lock.ReleaseLock();
                        }
                        catch { }// 获取锁超时

                    }

                    if (null != dataTransForm && null != dataTransForm.DataFromSocket)// 从socket接收数据
                    {
                        try
                        {
                            dataTransForm.DataFromSocket_Lock.AcquireWriterLock(100);// 100ms超时

                            str = mySocket.SocketGetData();

                            if (0 < str.Length)
                            {
                                dataTransForm.DataSocket += "receive from socket:\r\n" + str + "\r\n";                               
                                dataTransForm.DataFromSocket += str;
                            }

                            dataTransForm.DataFromSocket_Lock.ReleaseLock();
                        }
                        catch { }// 获取锁失败

                    }

                }
                catch { }

                Thread.Sleep(100);// 降低cpu占用率

            }

        }

        /// <summary>
        /// 开启串口收发线程
        /// </summary>
        private void StartSerial()
        {
            this.serialRunFlag = true;

            Thread thread = new Thread(new ThreadStart(this.SerialThread));
            try { thread.Start(); }
            catch { }
        }

        private bool serialRunFlag = false;
        private void SerialThread()// serial收发线程
        {
            int sendCount = 0;
            int recvCount = 0;
            string strTmp = "";

            while(this.serialRunFlag)
            {
                try
                {
                    if (null == mySerialPort) { serialRunFlag = false; continue; }// 实例不能为空

                    if (!mySerialPort.IsOpen) { continue; }// 串口必须打开

                    if (null != dataTransForm && null != dataTransForm.DataFromSerial)// 从串口接收数据
                    {
                        try
                        {
                            dataTransForm.DataFromSerial_Lock.AcquireWriterLock(100);// 100ms超时

                            strTmp = this.RecvSerialData();
                            if (0 < strTmp.Length)// 接收到数据才写入缓存
                            {
                                dataTransForm.DataSerial += "receive from serial:\r\n" + strTmp + "\r\n";
                                dataTransForm.DataFromSerial += strTmp;
                            }

                            dataTransForm.DataFromSerial_Lock.ReleaseLock();
                        }
                        catch { }// 获取锁失败



                    }

                    if (null != dataTransForm && null != dataTransForm.DataFromSocket && 0 < dataTransForm.DataFromSocket.Length)// 发送数据到串口
                    {
                        try
                        {
                            dataTransForm.DataFromSocket_Lock.AcquireWriterLock(100);// 100ms超时

                            sendCount = this.SendSerialData(dataTransForm.DataFromSocket);
                            dataTransForm.DataSerial += "send to serial:\r\n" + dataTransForm._DataFromSocket.Substring(0, sendCount) + "\r\n";
                            dataTransForm.DataFromSocket = dataTransForm.DataFromSocket.Remove(0, sendCount);

                            dataTransForm.DataFromSocket_Lock.ReleaseLock();
                        }
                        catch { }// 获取锁失败

                    }

                }
                catch { }

                Thread.Sleep(50);// 降低cpu占用率

            }
        }


        private MyUIElement myUIElement;
        private bool uiRunflag = false;
        private bool preSerialStatus = false;// false表示连接断开
        private bool? preSocketStatus = false;// null表示连接中或等待连接
        private void StartUIElementInspect()// 根据串口及网络连接状态，改变按钮的颜色和字符
        {

            if (null == myUIElement) { myUIElement = new MyUIElement(); }

            this.openCom.DataContext = this.myUIElement;// 绑定按钮颜色和字符显示
            this.openSocket.DataContext = this.myUIElement;

            this.uiRunflag = true;

            Thread thread = new Thread(new ThreadStart(this.UIThread));
            try { thread.Start(); }
            catch { }

        }

        SolidColorBrush green = new SolidColorBrush(Colors.Green);
        SolidColorBrush dodgerBlue = new SolidColorBrush(Colors.DodgerBlue);
        SolidColorBrush orange = new SolidColorBrush(Colors.Orange); 
        /// <summary>
        /// UI监测线程
        /// </summary>
        private void UIThread()
        {
            preSerialStatus = false;
            myUIElement.serialbtntext = "打开串口";
            myUIElement.serialbgcolor = this.dodgerBlue;
            preSocketStatus = false;
            myUIElement.socketbtntext = "启动通信";
            myUIElement.socketbgcolor = this.dodgerBlue;

            while(uiRunflag)
            {
                // 串口打开按钮颜色和字符
                if(null == mySerialPort || !mySerialPort.IsOpen )
                {
                    if(true == preSerialStatus)
                    {
                        myUIElement.serialbtntext = "打开串口";
                        myUIElement.serialbgcolor = this.dodgerBlue;
                    }
                    preSerialStatus = false;
                }
                else
                {
                    if(false == preSerialStatus)
                    {
                        myUIElement.serialbtntext = "关闭串口";
                        myUIElement.serialbgcolor = this.green;
                        //EnableSerialControl(false);
                    }
                    preSerialStatus = true;
                }


                // 网口打开按钮颜色和字符
                if(null == mySocket || mySocket.IsConnected() == false)// 未连接
                {
                    if(false != preSocketStatus)
                    {
                        myUIElement.socketbtntext = "启动通信";
                        myUIElement.socketbgcolor = this.dodgerBlue;
                        //EnableSocketControl(true);
                    }
                    preSocketStatus = false;
                }
                else if(null == mySocket.IsConnected())// 连接中或等待连接中
                {
                    if(null != preSocketStatus)
                    {
                        myUIElement.socketbtntext = "连接中...";
                        myUIElement.socketbgcolor = this.orange;
                       // EnableSocketControl(false);
                    }
                    preSocketStatus = null;
                }
                else// 已连接
                {
                    if(true != preSocketStatus)
                    {
                        myUIElement.socketbtntext = "停止通信";
                        myUIElement.socketbgcolor = this.green;
                        //EnableSocketControl(false);
                    }
                    preSocketStatus = true;
                }

                Thread.Sleep(50);

            }
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            this.serialRunFlag = false;
            this.sockeRunFlag = false;
            this.uiRunflag = false;

            if(null != mySerialPort && mySerialPort.IsOpen)
            {
                mySerialPort.Close();
            }

            if (null != mySocket) { mySocket.CloseSocket(); }

            if (null != notifyIcon) { notifyIcon.Dispose(); }
        }

        #region // 托盘图标

        private System.Windows.Forms.NotifyIcon notifyIcon = null;
        System.IO.Stream iconStream = Application.GetResourceStream(new Uri("pack://application:,,,/skin/tray.ico")).Stream;//new System.Drawing.Icon(System.Windows.Forms.Application.StartupPath + "./skin/tray.ico");
        private void InitialTray()
        {

            //设置托盘的各个属性
            notifyIcon = new System.Windows.Forms.NotifyIcon();
            notifyIcon.BalloonTipText = "程序开始运行";
            notifyIcon.Text = "托盘图标";
            notifyIcon.Icon = new System.Drawing.Icon(iconStream);
            notifyIcon.Visible = true;
            notifyIcon.ShowBalloonTip(2000);
            notifyIcon.MouseClick += new System.Windows.Forms.MouseEventHandler(notifyIcon_MouseClick);

            //设置菜单项
            //System.Windows.Forms.MenuItem menu1 = new System.Windows.Forms.MenuItem("关于...");
            //System.Windows.Forms.MenuItem menu2 = new System.Windows.Forms.MenuItem("菜单项2");
            //System.Windows.Forms.MenuItem menu = new System.Windows.Forms.MenuItem("菜单", new System.Windows.Forms.MenuItem[] { menu1 , menu2 });

            System.Windows.Forms.MenuItem menu = new System.Windows.Forms.MenuItem("About");
            menu.Click += new EventHandler(show_about);

            //退出菜单项
            System.Windows.Forms.MenuItem exit = new System.Windows.Forms.MenuItem("Exit");
            exit.Click += new EventHandler(exit_Click);

            //关联托盘控件
            System.Windows.Forms.MenuItem[] childen = new System.Windows.Forms.MenuItem[] { menu , exit };
            notifyIcon.ContextMenu = new System.Windows.Forms.ContextMenu(childen);

            //窗体状态改变时候触发
            this.StateChanged += new EventHandler(SysTray_StateChanged);
        }
        ///
        /// 窗体状态改变时候触发
        ///
        ///

        ///

        private void SysTray_StateChanged(object sender, EventArgs e)
        {
            if (this.WindowState == WindowState.Minimized)
            {
                this.Visibility = Visibility.Hidden;
            }
        }

        ///
        /// 退出选项
        ///
        ///

        ///

        private void exit_Click(object sender, EventArgs e)
        {
            if (System.Windows.MessageBox.Show("确定要关闭吗?",
                                               "退出",
                                                MessageBoxButton.YesNo,
                                                MessageBoxImage.Question,
                                                MessageBoxResult.No) == MessageBoxResult.Yes)
            {
                //notifyIcon.Dispose();
                //System.Windows.Application.Current.Shutdown();
                this.Close();
            }
        }

        private void show_about(object sender, EventArgs e)
        {
            MessageBox.Show("版权：迅效自动化科技有限公司\r\n版本：V1.0.2\r\n发布日期：2017-09-12");
        }


        ///
        /// 鼠标单击
        ///
        ///

        ///

        private void notifyIcon_MouseClick(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                if (this.Visibility == Visibility.Visible)
                {
                    this.Visibility = Visibility.Hidden;
                }
                else
                {
                    this.Visibility = Visibility.Visible;
                    this.Activate();
                }
            }
        }

        #endregion
    }
    


}
