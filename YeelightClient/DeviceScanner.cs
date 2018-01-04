using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

// SSDP reference : https://searchcode.com/codesearch/view/8152089/ /DeviceFinder.SSDP.cs 

namespace YeelightClient
{
    internal class DeviceScanner
    {
        private const int SsdpUdpPort = 1982;
        private const string SsdpMessage = "M-SEARCH * HTTP/1.1\r\nHOST: 239.255.255.250:1982\r\nMAN: \"ssdp:discover\"\r\nST: wifi_bulb";

        private static readonly byte[] ssdpDiagram = Encoding.ASCII.GetBytes(SsdpMessage);

        //MultiCastEndPoint 
        private static readonly IPEndPoint LocalEndPoint = new IPEndPoint(Utils.GetLocalIPAddress(), 0);
        private static readonly IPEndPoint MulticastEndPoint = new IPEndPoint(IPAddress.Parse("239.255.255.250"), 1982);
        private static readonly IPEndPoint AnyEndPoint = new IPEndPoint(IPAddress.Any, 0);

        //Socket for SSDP 
        private Socket ssdpSocket;
        private byte[] ssdpReceiveBuffer;

        public DeviceScanner()
        {
            this.DiscoveredDevices = new List<Device>();
        }

        public IList<Device> DiscoveredDevices { get; }

        public void SendDiscoveryMessage()
        {
            for (int i = 0; i < 3; i++)
            {
                if (i > 0)
                {
                    Thread.Sleep(50);
                }

                var async = this.ssdpSocket.BeginSendTo(
                    ssdpDiagram,
                    0,
                    ssdpDiagram.Length,
                    SocketFlags.None,
                    MulticastEndPoint,
                    o =>
                    {
                        var result = this.ssdpSocket.EndSendTo(o);

                        if (result != ssdpDiagram.Length)
                        {
                            Console.Write("Sent SSDP discovery request length mismatch: {0} != {1} (expected)", result, ssdpDiagram.Length);
                        }

                    },
                    null);

                async.AsyncWaitHandle.WaitOne();
            }
        }

        private void ReceiveResponseRecursive(IAsyncResult response = null)
        {
            Contract.Requires(response == null || this.ssdpReceiveBuffer != null);
            Contract.Ensures(this.ssdpReceiveBuffer != null);

            // check if there is a response
            // or this is the first call
            if (response != null)
            {
                // complete read
                EndPoint senderEndPoint = AnyEndPoint;

                var read = this.ssdpSocket.EndReceiveFrom(response, ref senderEndPoint);

                // make sure we don't reuse the reference
                var resBuf = this.ssdpReceiveBuffer;

                new Task(() => HandleResponse(senderEndPoint, Encoding.ASCII.GetString(resBuf, 0, read))).Start();
            }

            // trigger the next cycle
            // tail recursion
            EndPoint recvEndPoint = LocalEndPoint;
            this.ssdpReceiveBuffer = new byte[4096];

            this.ssdpSocket.BeginReceiveFrom(
                this.ssdpReceiveBuffer,
                0,
                this.ssdpReceiveBuffer.Length,
                SocketFlags.None,
                ref recvEndPoint,
                ReceiveResponseRecursive,
                null);
        }

        /// <summary>
        /// Function asynchrone to discovers all buls in the network
        /// Discovered bulbs are added to the list on the MainWindow
        /// </summary>
        public void StartListening()
        {
            this.ssdpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp)
            {
                Blocking = false,
                Ttl = 1,
                UseOnlyOverlappedIO = true,
                MulticastLoopback = false,
            };

            IPAddress localIpAddress = Utils.GetLocalIPAddress();

            Console.WriteLine("Mon ip: " + localIpAddress);

            this.ssdpSocket.Bind(new IPEndPoint(localIpAddress, 0));

            this.ssdpSocket.SetSocketOption(
              SocketOptionLevel.IP,
              SocketOptionName.AddMembership,
              new MulticastOption(MulticastEndPoint.Address));

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

            lock (DiscoveredDevices)
            {
                //if list already contains this bulb skip
                bool alreadyExisting = DiscoveredDevices.Any(item => item.Ip == ip);
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

                    DiscoveredDevices.Add(new Device(ip, id, isOn, Convert.ToInt32(bright), model));
                }
            }
        }
    }
}
