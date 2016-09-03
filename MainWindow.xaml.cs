using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Net;
using System.Net.Sockets;
using MahApps.Metro.Controls;
using Xceed.Wpf.Toolkit;

namespace YeelightController
{
    /// <summary>
    /// Logique d'interaction pour MainWindow.xaml
    /// </summary>
    /// 
    public partial class MainWindow : MetroWindow
    {
        //List of all discovered bulbs
        private List<Bulb> m_Bulbs = new List<Bulb>();
        //The connected Bulb, null if no connected
        private Bulb m_ConnectedBulb;
        //TcpClient used to communicate with the Bulb
        private TcpClient m_TcpClient;
        //MultiCastAddress
        private IPAddress m_MultiCastAddress = IPAddress.Parse("239.255.255.250");    
        //Udp port used during the ssdp discovers
        private int m_Port = 1982;    

        public MainWindow()
        {
            InitializeComponent();

            //Bind the list to the ListView
            lstBulbs.ItemsSource = m_Bulbs;

            //Search bulbs once at running time
            DiscoverBulbs();                 
        }

        /// <summary>
        /// Function to get a sub part of a string, exemple : startexempleend, by using "str" as begin param and "end as end param, you receive "exemple"
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


        /// <summary>
        /// Function asynchrone to discovers all buls in the network
        /// Discovered bulbs are added to the list on the MainWindow
        /// </summary>
        private async void DiscoverBulbs()
        {     
            UdpClient client = new UdpClient();           

            //convert to byte array
            byte[] dgram = Encoding.ASCII.GetBytes("M-SEARCH * HTTP/1.1\r\n" +
                      "HOST: 239.255.255.250:1982\r\n" +
                      "MAN: \"ssdp:discover\"\r\n" +
                      "ST: wifi_bulb");

            //Remote endpoint "239.255.255.250:1982"
            IPEndPoint remoteEndPoint = new IPEndPoint(m_MultiCastAddress, m_Port);

            //Join multicastgroup
            client.JoinMulticastGroup(m_MultiCastAddress);

            //Send the message
            client.Send(dgram, dgram.Length, remoteEndPoint);

            //After 1000 ms timeout we stop research
            client.Client.ReceiveTimeout = 1000;

            IPEndPoint RemoteIpEndPoint = new IPEndPoint(IPAddress.Any, 0);   
            
                     
            await Task.Run(() =>
            {
                try
                {
                    while (true)
                    {             
                        byte[] receiveBytes = client.Receive(ref RemoteIpEndPoint);
                        string message = Encoding.ASCII.GetString(receiveBytes);
                                     
                        string ip = "";
                        GetSubString(message, "Location: yeelight://", ":", ref ip);

                        //if list already contains this bulb skip
                        bool alreadyExisting = m_Bulbs.Any(item => item.Ip == ip);
                        if (alreadyExisting == false)
                        {
                            string id = "";
                            GetSubString(message, "id: ", "\r\n", ref id);

                            string bright = "";
                            GetSubString(message, "bright: ", "\r\n", ref bright);

                            string power = "";
                            GetSubString(message, "power: ", "\r\n", ref power);
                            bool isOn = power.Contains("on");

                            m_Bulbs.Add(new Bulb(ip, id, isOn, Convert.ToInt32(bright)));
                        }              
                    }
                }
                catch
                {                 
                    //timeout
                }
                finally
                {
                    client.Close();
                }          
            });          
        }
             

        private void lstBulbs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (m_TcpClient != null)
                m_TcpClient.Close();

            Bulb bulb = m_Bulbs[lstBulbs.SelectedIndex];
            
            m_TcpClient = new TcpClient();            
            m_TcpClient.Connect(bulb.getEndPoint());

            if(!m_TcpClient.Connected)
            {
                panelBulbControl.IsEnabled = false;
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
                Console.WriteLine(cmd_str.ToString());
                m_TcpClient.Client.Send(data);               
            }
        }
    }
}
