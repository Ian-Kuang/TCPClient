using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace TaskOneTCPClient
{
    public class AsyncTcpClient
    {
        private string ip;
        private int port;
        byte[] ReadBytes = new byte[1024 * 1024];
        public Action<string> Log;
        public Action<string, Action<string>> MESHandle;
        private TcpClient tcpClient;
        public static AsyncTcpClient Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new AsyncTcpClient();
                }
                return instance;
            }
        }

        private static AsyncTcpClient instance;

        public void ConnectServer(string ip, int port)
        {
            this.ip = ip;
            this.port = port;
            try
            {
                tcpClient = new System.Net.Sockets.TcpClient();
                tcpClient.BeginConnect(IPAddress.Parse(ip), port, Connecting, null);
            }
            catch (Exception e)
            {
                Log(e.Message);
            }
        }


        void Connecting(IAsyncResult ar)
        {
            if (!tcpClient.Connected)
            {
                Log("The server is not started. Try reconnecting...");
                IAsyncResult rest = tcpClient.BeginConnect(IPAddress.Parse(ip), port, Connecting, null);
                bool result = rest.AsyncWaitHandle.WaitOne(3000);
            }
            else
            {
                Log("Connected");
                tcpClient.EndConnect(ar);
                tcpClient.GetStream().BeginRead(ReadBytes, 0, ReadBytes.Length, ReceiveCallBack, null);
            }
        }

        void ReceiveCallBack(IAsyncResult ar)
        {
            try
            {
                int len = tcpClient.GetStream().EndRead(ar);
                if (len > 0)
                {
                    string str = Encoding.UTF8.GetString(ReadBytes, 0, len);
                    str = Uri.UnescapeDataString(str);
                    MESHandle(str, SendMsg);
                    Log(string.Format("Received a message from host :{0} |{1}", ip, str));
                    //Log(str);

                    tcpClient.GetStream().BeginRead(ReadBytes, 0, ReadBytes.Length, ReceiveCallBack, null);
                }
                else
                {
                    tcpClient = null;
                    Log("Disconnected, try reconnecting...");
                    ConnectServer(ip, port);
                }
            }
            catch (Exception e)
            {
                Log(e.Message);
            }
        }


        public void SendMsg(string msg)
        {
            byte[] msgBytes = Encoding.UTF8.GetBytes(msg);
           
            tcpClient.GetStream().BeginWrite(msgBytes, 0, msgBytes.Length, (ar) =>
            {
                tcpClient.GetStream().EndWrite(ar);
            }, null);
        }


        public void Close()
        {
            if (tcpClient != null && tcpClient.Client.Connected)
                tcpClient.Close();
        }
    }
}
