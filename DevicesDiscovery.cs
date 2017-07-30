using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Diagnostics.Contracts;
using System.Threading.Tasks;

// SSDP reference : https://searchcode.com/codesearch/view/8152089/ /DeviceFinder.SSDP.cs 

namespace YeelightController
{
    class DevicesDiscovery
    {
        //List of all discovered bulbs
        private List<Device> m_Bulbs;
                
        //Socket for SSDP 
        private Socket _ssdpSocket;
        private byte[] _ssdpReceiveBuffer;

        //Udp port used during the ssdp discovers
        private static readonly int m_Port = 1982;

        //MultiCastEndPoint 
        private static readonly IPEndPoint LocalEndPoint = new IPEndPoint(Utils.GetLocalIPAddress(), 0);
        private static readonly IPEndPoint MulticastEndPoint = new IPEndPoint(IPAddress.Parse("239.255.255.250"), 1982);
        private static readonly IPEndPoint AnyEndPoint = new IPEndPoint(IPAddress.Any, 0);

        //SSDP Diagram Message
        private static readonly string ssdpMessage = "M-SEARCH * HTTP/1.1\r\nHOST: 239.255.255.250:1982\r\nMAN: \"ssdp:discover\"\r\nST: wifi_bulb";
        private static readonly byte[] dgram = Encoding.ASCII.GetBytes(ssdpMessage);
        
        public DevicesDiscovery()
        {
            m_Bulbs = new List<Device>();
        }

        public List<Device> GetDiscoveredDevices()
        {
            return m_Bulbs;
        }

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
           
        /// <summary>
        /// Function asynchrone to discovers all buls in the network
        /// Discovered bulbs are added to the list on the MainWindow
        /// </summary>
        public void StartListening()
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

            IPAddress localIpAddress = Utils.GetLocalIPAddress();

            Console.WriteLine("Mon ip: " + localIpAddress);

            _ssdpSocket.Bind(new IPEndPoint(localIpAddress, 0));

            _ssdpSocket.SetSocketOption(
              SocketOptionLevel.IP,
              SocketOptionName.AddMembership,
              new MulticastOption(MulticastEndPoint.Address)
            );

            // start listening
            ReceiveResponseRecursive();
        }


        private void HandleResponse(EndPoint sender, string response)
        {
            Contract.Requires(sender != null);
            Contract.Requires(response != null);

            string ip = "";
            Utils.GetSubString(response, "Location: yeelight://", ":", ref ip);

            Console.WriteLine(response);

            lock (m_Bulbs)
            {
                //if list already contains this bulb skip
                bool alreadyExisting = m_Bulbs.Any(item => item.Ip == ip);
                if (alreadyExisting == false)
                {
                    string id = "";
                    Utils.GetSubString(response, "id: ", "\r\n", ref id);

                    string bright = "";
                    Utils.GetSubString(response, "bright: ", "\r\n", ref bright);

                    string power = "";
                    Utils.GetSubString(response, "power: ", "\r\n", ref power);
                    bool isOn = power.Contains("on");

                    string model = "";
                    Utils.GetSubString(response, "model: ", "\r\n", ref model);

                    m_Bulbs.Add(new Device(ip, id, isOn, Convert.ToInt32(bright), model));
                }
            }
        }
    }
}
