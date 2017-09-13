using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

//add by shcullar
using System.Threading;

namespace SerialToTCP
{
    class MySocket
    {
        public NetParams netParams;

        bool socketInitFlag = false;
        
        System.Net.Sockets.Socket _socket = null;
        System.Net.Sockets.Socket serverSocket = null;
        System.Net.Sockets.Socket _udpSocket = null;

        System.Net.Sockets.Socket socket
        {
            get
            {
                if (null != netParams && netParams.protocol == Protocol.UDP) { return _udpSocket; }

                return _socket;
            }
            set
            {
                if (null != netParams && netParams.protocol == Protocol.UDP) { _udpSocket = value; }

                _socket = value;
            }
        }

        public System.Net.Sockets.Socket getSocket()
        {
            return this.socket;
        }

        // 带参数构造函数
        public MySocket(NetParams param)
        {
            this.netParams = param;
            this.InitSocket();
        }

        // 不带参数构造函数
        public MySocket() { }

        // 开启初始化线程
        public void InitSocket()
        {
            Thread thread = new Thread(new ThreadStart(this.InitSocketThread));// 启动连接
            try { thread.Start(); }
            catch { }
        }

        private void InitSocketThread()
        {
             socketInitFlag = true; // 开始标记

            if (null == this.netParams) { return; }

            if(this.netParams.protocol == Protocol.TCP_Client)
            {
                this.InitClient();
            }
            else if(this.netParams.protocol == Protocol.TCP_Server)
            {
                this.InitServer();
            }
            else if(this.netParams.protocol == Protocol.UDP)
            {
                this.InitUdp();
            }

            socketInitFlag = false;// 初始化结束
        }

        /// <summary>
        /// socket 发送数据
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public int SocketSendData(string data)
        {
            if (null == socket || null == data) { return 0; }

            int len=0;
            byte[] sendBytes = Encoding.ASCII.GetBytes(data);
            try
            {
                if (null != netParams && netParams.protocol == Protocol.UDP) 
                { 
                    len = socket.SendTo(sendBytes,Target_EndPoint); 
                }
                else { len = socket.Send(sendBytes); }
                
            }
            catch { return 0; }
            return len;
        }

        // socket 接收数据
        public string SocketGetData()
        {
            if (null == socket) { return ""; }

            string str = "";
            try
            {
                if (0 >= socket.Available) { return str; }

                byte[] recvBytes = new byte[socket.Available];

                int len = 0;
                if (null != netParams && netParams.protocol == Protocol.UDP) { len = socket.ReceiveFrom(recvBytes, ref Receive_EndPoint); }
                else { len = socket.Receive(recvBytes, recvBytes.Length, System.Net.Sockets.SocketFlags.None); }

                str = Encoding.ASCII.GetString(recvBytes, 0, len);
            }
            catch
            {
                return "";
            }
            return str;
            
        }

        // 以socket服务端方式初始化
        public System.Net.Sockets.Socket InitServer()
        {

            System.Net.IPAddress ip;
            System.Net.IPEndPoint ipe;

            try
            {
                //ip = System.Net.IPAddress.Parse(netParams.localIp);

                ipe = new System.Net.IPEndPoint(System.Net.IPAddress.Any, netParams.localPort);

                serverSocket = new System.Net.Sockets.Socket(System.Net.Sockets.AddressFamily.InterNetwork, System.Net.Sockets.SocketType.Stream, System.Net.Sockets.ProtocolType.Tcp);
                serverSocket.Bind(ipe);
                serverSocket.Listen(1);// 开始监听
                socket = serverSocket.Accept();
            }
            catch { return null; }


            return socket;

        }


        /// <summary>
        /// 以socket客户端方式初始化
        /// </summary>
        /// <returns></returns>
        public System.Net.Sockets.Socket InitClient()
        {
            System.Net.IPAddress ip;
            System.Net.IPEndPoint ipe;

            try
            {
                ip = System.Net.IPAddress.Parse(netParams.targetIp);
                ipe = new System.Net.IPEndPoint(ip, netParams.targetPort);
                socket = new System.Net.Sockets.Socket(System.Net.Sockets.AddressFamily.InterNetwork, System.Net.Sockets.SocketType.Stream, System.Net.Sockets.ProtocolType.Tcp);
                socket.Connect(ipe);
            }
            catch
            {
                return null;
            }

            return socket;
        }


        System.Net.IPEndPoint Target_EndPoint = null;
        System.Net.EndPoint Receive_EndPoint = null;
        /// <summary>
        /// 以udp方式创建连接
        /// </summary>
        /// <returns></returns>
        public System.Net.Sockets.Socket InitUdp()
        {
            System.Net.IPAddress ip;
            System.Net.IPEndPoint ipe;

            try
            {
                

                ip = System.Net.IPAddress.Parse(netParams.localIp);
                ipe = new System.Net.IPEndPoint(ip, netParams.localPort);
                socket = new System.Net.Sockets.Socket(System.Net.Sockets.AddressFamily.InterNetwork, System.Net.Sockets.SocketType.Dgram, System.Net.Sockets.ProtocolType.Udp);
                socket.Bind(ipe);

                if (null == Target_EndPoint) 
                {
                    if (null == netParams || netParams.udpType == UDPType.Broadcast)// 广播
                    {
                        Target_EndPoint = new System.Net.IPEndPoint(System.Net.IPAddress.Broadcast, netParams.targetPort);
                        socket.SetSocketOption(System.Net.Sockets.SocketOptionLevel.Socket, System.Net.Sockets.SocketOptionName.Broadcast, true);
                    }
                    else// 单播
                    {
                        Target_EndPoint = new System.Net.IPEndPoint(System.Net.IPAddress.Parse(netParams.targetIp), netParams.targetPort);
                    }

                }// 目标IP及端口号
                if (null == Receive_EndPoint) { Receive_EndPoint = new System.Net.IPEndPoint(System.Net.IPAddress.Any, 0); }// 接收参数，用于接收发送方ip及端口号，不用于指定参数

            }
            catch { return null; }

            return socket;
        }

        /// <summary>
        /// 判断socket是否已连接，返回null代表正在连接中或等待连接
        /// </summary>
        /// <returns></returns>
        public bool? IsConnected()
        {
            if (socketInitFlag) { return null; }

            if (null == socket) { return false; }

            if (netParams.protocol == Protocol.UDP) { return true; }
            return socket.Connected;
        }


        public void CloseSocket()
        {
            if (null != socket) { socket.Close(); socket = null; }
            if (null != serverSocket) { serverSocket.Close(); socket = null; }
        }

    }
}
