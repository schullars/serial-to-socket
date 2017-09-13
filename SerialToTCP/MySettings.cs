using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

// add by schullar
using System.IO;
using System.Xml.Serialization;
using System.Windows;


namespace SerialToTCP
{
    public class MyGlobalInfo
    {
        public bool hidenWindow { get; set; }// 开机隐藏界面
        public int delay { get; set; }// 虚拟串口初始化需要时间，本软件启动之前虚拟串口可能未初始化完成，顾增加延时
        public NetParams netParams{get;set;}
        public SerialParams serialParams{get;set;}
    }


    class MySettings
    {
        string filePath = System.AppDomain.CurrentDomain.SetupInformation.ApplicationBase+"Settings.xml";

        public static MySettings Default{get;set;}
        public MyGlobalInfo myGlobalInfo{get; set;}

        public MySettings()
        {
            if (ConfigFileExists() && this.InitFromXml()) { return; }// 读取配置文件成功

            this.InitAsDefault();
        }


        bool ConfigFileExists()
        {
            if (!System.IO.File.Exists(this.filePath))
            {
                return false;
            }
            return true;
        }

        void InitAsDefault()
        {
            if (null == this.myGlobalInfo) { this.myGlobalInfo = new MyGlobalInfo(); }
            // 是否隐藏窗口
            this.myGlobalInfo.hidenWindow = false;
            
            // 启动前延时
            this.myGlobalInfo.delay = 0;//s

            // 串口参数初始化
            if (null == this.myGlobalInfo.serialParams) { this.myGlobalInfo.serialParams = new SerialParams(); }
            this.myGlobalInfo.serialParams.comNum = null;
            this.myGlobalInfo.serialParams.baud = 9600;
            this.myGlobalInfo.serialParams.parity = System.IO.Ports.Parity.None;
            this.myGlobalInfo.serialParams.databits = 8;
            this.myGlobalInfo.serialParams.stopbits = System.IO.Ports.StopBits.One;
            this.myGlobalInfo.serialParams.autostart = false;

            //网口参数初始化
            if (null == this.myGlobalInfo.netParams) { this.myGlobalInfo.netParams = new NetParams(); }
            this.myGlobalInfo.netParams.protocol = Protocol.UDP;
            this.myGlobalInfo.netParams.udpType = UDPType.Unicast;
            this.myGlobalInfo.netParams.targetIp = "192.168.3.120";
            this.myGlobalInfo.netParams.targetPort = 3000;
            this.myGlobalInfo.netParams.localIp = "192.168.3.120";
            this.myGlobalInfo.netParams.localPort = 3100;
            this.myGlobalInfo.netParams.autostart = false;

        }

        bool InitFromXml()
        {
            Stream fs_stream = null;
            XmlSerializer formatter = null;
            try
            {
                fs_stream = new FileStream(this.filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                formatter = new XmlSerializer(typeof(MyGlobalInfo));
                this.myGlobalInfo = (MyGlobalInfo)formatter.Deserialize(fs_stream);
            }
            catch (Exception ex)
            {
                MessageBox.Show("读取配置文件失败！");
                return false;
            }
            finally
            {
                if (null != fs_stream) { fs_stream.Close(); }
            }

            //Console.WriteLine(this.myGlobalInfo.ToString());

            return true;
        }

        bool WriteToXml(MyGlobalInfo myglobalinfo, bool cover)
        {
            Stream fs_stream = null;
            XmlSerializer formatter = null;
            FileMode filemode;

            if (cover) { filemode = FileMode.Create; }
            else { filemode = FileMode.CreateNew; }

            try
            {
                MemoryStream m_stream = new MemoryStream();
                formatter = new XmlSerializer(myglobalinfo.GetType());
                formatter.Serialize(m_stream, myglobalinfo);// 写内存stream无误后再写入文件，否则配置文件会被清空

                fs_stream = new FileStream(this.filePath, filemode, FileAccess.Write, FileShare.None);
                formatter.Serialize(fs_stream, myglobalinfo);

            }
            catch (Exception ex)
            {
                MessageBox.Show("保存配置失败！");
                return false;
            }
            finally
            {
                if (null != fs_stream) { fs_stream.Close(); }
            }

            return true;

        }

        public bool SaveDataToXml(MyGlobalInfo myinfo)
        {
            return this.WriteToXml(myinfo, true);
        }
    }
}
