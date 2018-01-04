using System.Net.Sockets;
using System.Text;

namespace YeelightClient
{
    internal class DeviceIO
    {
        private TcpClient tcpClient;

        private Device connectedDevice;

        public bool Connect(Device device)
        {
            if (this.tcpClient != null)
            {
                this.tcpClient.Close();
            }

            this.tcpClient = new TcpClient();
            this.tcpClient.Connect(device.GetEndPoint());

            if (!this.tcpClient.Connected)
            {
                this.connectedDevice = null;
                return false;
            }
            else
            {
                this.connectedDevice = device;
            }

            return true;
        }

        public void Toggle()
        {
            var command = new StringBuilder();
            command.Append("{\"id\":");
            command.Append(this.connectedDevice.Id);
            command.Append(",\"method\":\"toggle\",\"params\":[]}\r\n");

            var data = Encoding.ASCII.GetBytes(command.ToString());
            this.tcpClient.Client.Send(data);

            this.connectedDevice.State = !this.connectedDevice.State;

            /* 
             * Receive Bulb Reponse
             * byte[] buffer = new Byte[256];
             *  m_TcpClient.Client.Receive(buffer);
             *   Console.WriteLine(Encoding.ASCII.GetString(buffer));
             */
        }

        public void SetBrightness(int value, int smooth)
        {
            var command = new StringBuilder();
            command.Append("{\"id\":");
            command.Append(this.connectedDevice.Id);
            command.Append(",\"method\":\"set_bright\",\"params\":[");
            command.Append(value);
            command.Append(", \"smooth\", " + smooth + "]}\r\n");

            var data = Encoding.ASCII.GetBytes(command.ToString());
            this.tcpClient.Client.Send(data);

            this.connectedDevice.Brightness = value;
        }


        public void SetColor(int r, int g, int b, int smooth)
        {
            //Convert RGB into integer 0x00RRGGBB
            int value = ((r) << 16) | ((g) << 8) | (b);

            var command = new StringBuilder();
            command.Append("{\"id\":");
            command.Append(this.connectedDevice.Id);
            command.Append(",\"method\":\"set_rgb\",\"params\":[");
            command.Append(value);
            command.Append(", \"smooth\", " + smooth + "]}\r\n");

            var data = Encoding.ASCII.GetBytes(command.ToString());
            this.tcpClient.Client.Send(data);
        }

        public void ExecCommand(string method, string param, int smooth)
        {
            var command = new StringBuilder();
            command.Append("{\"id\":");
            command.Append(this.connectedDevice.Id);
            command.Append(",\"method\":\"" + method + "\",\"params\":[");
            command.Append(param);
            command.Append(", \"smooth\", " + smooth + "]}\r\n");

            var data = Encoding.ASCII.GetBytes(command.ToString());
            this.tcpClient.Client.Send(data);
        }
    }
}