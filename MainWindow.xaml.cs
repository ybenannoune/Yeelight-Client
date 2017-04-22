using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using System.Net;
using System.Net.Sockets;
using MahApps.Metro.Controls;
using System.Diagnostics.Contracts;

// SSDP reference : https://searchcode.com/codesearch/view/8152089/ /DeviceFinder.SSDP.cs 

namespace YeelightController
{
    /// <summary>
    /// Logique d'interaction pour MainWindow.xaml
    /// </summary>
    /// 
    public partial class MainWindow : MetroWindow
    {
        //List of all discovered bulbs
        private List<Device> m_Bulbs = new List<Device>();
        //The connected Bulb, null if no connected
        private Device m_ConnectedBulb;
        //TcpClient used to communicate with the Bulb
        private TcpClient m_TcpClient;

        //Socket for SSDP 
        private Socket _ssdpSocket;
        private byte[] _ssdpReceiveBuffer;

        //Udp port used during the ssdp discovers
        private static readonly int m_Port = 1982;

        //MultiCastEndPoint 
        private static readonly IPEndPoint LocalEndPoint = new IPEndPoint(GetLocalIPAddress(), 0);
        private static readonly IPEndPoint MulticastEndPoint = new IPEndPoint(IPAddress.Parse("239.255.255.250"), 1982);
        private static readonly IPEndPoint AnyEndPoint = new IPEndPoint(IPAddress.Any, 0);

        //SSDP Diagram Message
        private static readonly string ssdpMessage = "M-SEARCH * HTTP/1.1\r\nHOST: 239.255.255.250:1982\r\nMAN: \"ssdp:discover\"\r\nST: wifi_bulb";

        private static readonly byte[] dgram = Encoding.ASCII.GetBytes(ssdpMessage);

        public void SendDiscoveryMessage()
        {
            // send message
            for (int i = 0; i < 3; i++)
            {
                if (i > 0)
                    Thread.Sleep(50);

                var async = _ssdpSocket.BeginSendTo(
                    dgram,
                    0,
                    dgram.Length,
                    SocketFlags.None,
                    MulticastEndPoint,
                    o =>
                    {
                        var r = _ssdpSocket.EndSendTo(o);
                        
                        if (r != dgram.Length)
                        {
                            Console.Write(
                                "Sent SSDP discovery request length mismatch: {0} != {1} (expected)",
                                r,
                                dgram.Length
                                );
                        }
                        
                    },
                    null
                    );

                async.AsyncWaitHandle.WaitOne();
            }
        }

        public MainWindow()
        {
            InitializeComponent();

            //Bind the list to the ListView
            lstBulbs.ItemsSource = m_Bulbs;

            //Search bulbs once at running time
            StartListening();

            //Send message
            SendDiscoveryMessage();
        }
        
        /// <summary>
        /// Function to get a sub part of a string, exemple : startexempleend, by using "start" as begin param and "end" as end param, you receive "exemple"
        /// Return false if no match
        /// </summary>
        /// <param name="str"></param>
        /// <param name="begin"></param>
        /// <param name="end"></param>
        /// <param name="ret"></param>
        /// <returns></returns>
        public static bool GetSubString(string str, string begin, string end, ref string ret)
        {
            int beginId = -1;
            int endId = -1;

            beginId = str.IndexOf(begin);
            if (beginId != -1)
            {
                ret = str.Substring(beginId + begin.Length);
                endId = ret.IndexOf(end);
                ret = ret.Substring(0, endId);
                return true;
            }
            return false;
        }


        #region HandleResponse

      
        private void HandleResponse(EndPoint sender, string response)
        {
            Contract.Requires(sender != null);
            Contract.Requires(response != null);

            string ip = "";
            GetSubString(response, "Location: yeelight://", ":", ref ip);

            Console.WriteLine(response);

            lock(m_Bulbs)
            {
                //if list already contains this bulb skip
                bool alreadyExisting = m_Bulbs.Any(item => item.Ip == ip);
                if (alreadyExisting == false)
                {
                    string id = "";
                    GetSubString(response, "id: ", "\r\n", ref id);

                    string bright = "";
                    GetSubString(response, "bright: ", "\r\n", ref bright);

                    string power = "";
                    GetSubString(response, "power: ", "\r\n", ref power);
                    bool isOn = power.Contains("on");

                    string model = "";
                    GetSubString(response, "model: ", "\r\n", ref model);

                    m_Bulbs.Add(new Device(ip, id, isOn, Convert.ToInt32(bright), !model.Contains("color")));
                }
            }      
        }
            

        private void ReceiveResponseRecursive(IAsyncResult r = null)
        {
            Contract.Requires(r == null || _ssdpReceiveBuffer != null);
            Contract.Ensures(_ssdpReceiveBuffer != null);

            // check if there is an response
            // or this is the first call
            if (r != null)
            {
                // complete read
                EndPoint senderEndPoint = AnyEndPoint;

                var read = _ssdpSocket.EndReceiveFrom(
                    r,
                    ref senderEndPoint
                    );

                // parse result
                var resBuf = _ssdpReceiveBuffer;    // make sure we don't reuse the reference

                new Task(
                    () => HandleResponse(senderEndPoint, Encoding.ASCII.GetString(resBuf, 0, read))
                    ).Start();
            }

            // trigger the next cycle
            // tail recursion
            EndPoint recvEndPoint = LocalEndPoint;
            _ssdpReceiveBuffer = new byte[4096];

            _ssdpSocket.BeginReceiveFrom(
                _ssdpReceiveBuffer,
                0,
                _ssdpReceiveBuffer.Length,
                SocketFlags.None,
                ref recvEndPoint,
                ReceiveResponseRecursive,
                null
                );
        }

        #endregion

        public static IPAddress GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip;
                }
            }
            throw new Exception("Local IP Address Not Found!");
        }

        /// <summary>
        /// Function asynchrone to discovers all buls in the network
        /// Discovered bulbs are added to the list on the MainWindow
        /// </summary>
        private void StartListening()
        {     
            // create socket
            _ssdpSocket = new Socket(
                    AddressFamily.InterNetwork,
                    SocketType.Dgram,
                    ProtocolType.Udp
                    )            
            {
                Blocking = false,
                Ttl = 1,
                UseOnlyOverlappedIO = true,
                MulticastLoopback = false,
            };

            Console.WriteLine("Mon ip: "  + GetLocalIPAddress());

            _ssdpSocket.Bind(new IPEndPoint(GetLocalIPAddress(), 0));

            _ssdpSocket.SetSocketOption(
              SocketOptionLevel.IP,
              SocketOptionName.AddMembership,
              new MulticastOption(MulticastEndPoint.Address)
            );      

            // start listening
            ReceiveResponseRecursive();         
        }

        private void lstBulbs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (m_TcpClient != null)
                m_TcpClient.Close();

            Device bulb = m_Bulbs[lstBulbs.SelectedIndex];
            
            m_TcpClient = new TcpClient();            
            m_TcpClient.Connect(bulb.getEndPoint());

            panelBulbControl.IsEnabled = false;

            if (!m_TcpClient.Connected)
            {               
                m_ConnectedBulb = null;
            }
            else
            {
                //Save the connected bulb for easiest access
                m_ConnectedBulb = bulb;        

                //Apply current bulb values to controls
                btnToggle.IsChecked = bulb.State;
                sliderBrightness.Value = bulb.Brightness;

                //Change panel state -> allow modification
                panelBulbControl.IsEnabled = true;
            }
        }       

        
        private void btnToggle_IsCheckedChanged(object sender, EventArgs e)
        {
            if(panelBulbControl.IsEnabled)
            {          
                StringBuilder cmd_str = new StringBuilder();           
                cmd_str.Append("{\"id\":");
                cmd_str.Append(m_ConnectedBulb.Id);
                cmd_str.Append(",\"method\":\"toggle\",\"params\":[]}\r\n");

                byte[] data = Encoding.ASCII.GetBytes(cmd_str.ToString());  
                m_TcpClient.Client.Send(data);

                //Toggle
                m_ConnectedBulb.State = !m_ConnectedBulb.State;
            }
        }

        //LostCapture event is used, we don't want to spam the bulb at each change, just the final one
        private void sliderBrightness_LostMouseCapture(object sender, MouseEventArgs e)
        {
            if (panelBulbControl.IsEnabled)
            {
                int value = Convert.ToInt32(sliderBrightness.Value);
                int smooth = Convert.ToInt32(sliderSmooth.Value);

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
        }

        //LostCapture event is used, we don't want to spam the bulb at each change, just the final one
        private void ColorCanvas_LostMouseCapture(object sender, MouseEventArgs e)
        {
            if (panelBulbControl.IsEnabled)
            {
                //Convert color to integer without alpha
                int value = ((colorCanvas.R) << 16) | ((colorCanvas.G) << 8) | (colorCanvas.B);       
                int smooth = Convert.ToInt32(sliderSmooth.Value);

                StringBuilder cmd_str = new StringBuilder();
                cmd_str.Append("{\"id\":");
                cmd_str.Append(m_ConnectedBulb.Id);
                cmd_str.Append(",\"method\":\"set_rgb\",\"params\":[");
                cmd_str.Append(value);
                cmd_str.Append(", \"smooth\", " + smooth + "]}\r\n");

                byte[] data = Encoding.ASCII.GetBytes(cmd_str.ToString());               
                m_TcpClient.Client.Send(data);               
            }
        }

   
    }
}
