using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Net;
using System.Windows.Media.Imaging;

namespace YeelightController
{
    class Device : INotifyPropertyChanged
    {
        //Default port
        private static int port = 55443;

        private static string icon_ledstrip = "/YeelightController;component/Ressources/ledstrip.png";
        private static string icon_bulb = "/YeelightController;component/Ressources/bulb.jpg";

        /// <summary>
        /// Bulb Ip
        /// </summary>
        private string m_Ip;

        /// <summary>
        /// Bulb Id used for send command
        /// </summary>
        private string m_Id;

        /// <summary>
        /// Correspond to the state of the lamp : On/Off
        /// </summary>
        private bool m_State;
          
        /// <summary>
        /// Brightness of the bulb
        /// </summary>
        private int m_Brightness;

        private bool m_IsBulb;

        public Device(string ip, string id, bool state, int bright, bool isBulb)
        {
            m_Id = id;
            m_Ip = ip;
            m_State = state;
            m_Brightness = bright;
            m_IsBulb = isBulb;
        }

        public string Ip
        {
            get { return m_Ip; }
            set { m_Ip = value;
                OnPropertyChanged();
            }       
        }

        public string Id
        {
            get { return m_Id; }
            set { m_Id = value;
                OnPropertyChanged();
            }     
        }
        
        public int Brightness
        {
            get { return m_Brightness; }
            set
            {
                m_Brightness = value;
                OnPropertyChanged();
            }
        }
        
        public bool State
        {
            get { return m_State; }
            set
            {
                m_State = value;
                OnPropertyChanged();
            }
        }

        public string ModelIcon
        {            
            get {
                if (m_IsBulb)
                    return "/YeelightController;component/Ressources/ledstrip.png";
                else return "/YeelightController;component/Ressources/bulb.jpg";
                }
        }

        public IPEndPoint getEndPoint()
        {
            return new IPEndPoint(IPAddress.Parse(m_Ip), port);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
