using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;

namespace YeelightController
{
    class DeviceIO
    {
        //TcpClient used to communicate with the device
        private TcpClient m_TcpClient;

        //The connected Bulb, null if no connected
        private Device m_ConnectedBulb;        

        public bool Connect(Device device)
        {
            if (m_TcpClient != null)
                m_TcpClient.Close();

            m_TcpClient = new TcpClient();
            m_TcpClient.Connect(device.getEndPoint());

            if (!m_TcpClient.Connected)
            {
                m_ConnectedBulb = null;
                return false;
            }
            else
            {
                //Save the connected bulb for easiest access
                m_ConnectedBulb = device;
            }

            return true;    
        }

        public void Toggle()
        {         
            StringBuilder cmd_str = new StringBuilder();
            cmd_str.Append("{\"id\":");
            cmd_str.Append(m_ConnectedBulb.Id);
            cmd_str.Append(",\"method\":\"toggle\",\"params\":[]}\r\n");

            byte[] data = Encoding.ASCII.GetBytes(cmd_str.ToString());
            m_TcpClient.Client.Send(data);

            //Toggle
            m_ConnectedBulb.State = !m_ConnectedBulb.State;

            /* Receive Bulb Reponse
            byte[] buffer = new Byte[256];
            m_TcpClient.Client.Receive(buffer);
            Console.WriteLine(Encoding.ASCII.GetString(buffer));
            */
        }

        public void SetBrightness(int value, int smooth)
        {
            StringBuilder cmd_str = new StringBuilder();
            cmd_str.Append("{\"id\":");
            cmd_str.Append(m_ConnectedBulb.Id);
            cmd_str.Append(",\"method\":\"set_bright\",\"params\":[");
            cmd_str.Append(value);
            cmd_str.Append(", \"smooth\", " + smooth + "]}\r\n");

            byte[] data = Encoding.ASCII.GetBytes(cmd_str.ToString());
            m_TcpClient.Client.Send(data);
               
            //Apply Value
            m_ConnectedBulb.Brightness = value;            
        }

        
        public void SetColor(int r, int g, int b, int smooth)
        {
            //Convert r,g,b into integer 0x00RRGGBB no alpha 
            int value = ((r) << 16) | ((g) << 8) | (b);

            StringBuilder cmd_str = new StringBuilder();
            cmd_str.Append("{\"id\":");
            cmd_str.Append(m_ConnectedBulb.Id);
            cmd_str.Append(",\"method\":\"set_rgb\",\"params\":[");
            cmd_str.Append(value);
            cmd_str.Append(", \"smooth\", " + smooth + "]}\r\n");

            byte[] data = Encoding.ASCII.GetBytes(cmd_str.ToString());
            m_TcpClient.Client.Send(data);
        }

        public void ExecCommand(string method, string param, int smooth)
        {
            StringBuilder cmd_str = new StringBuilder();
            cmd_str.Append("{\"id\":");
            cmd_str.Append(m_ConnectedBulb.Id);
            cmd_str.Append(",\"method\":\"" + method + "\",\"params\":[");
            cmd_str.Append(param);
            cmd_str.Append(", \"smooth\", " + smooth + "]}\r\n");

            byte[] data = Encoding.ASCII.GetBytes(cmd_str.ToString());
            m_TcpClient.Client.Send(data);  
        }
    }
}
