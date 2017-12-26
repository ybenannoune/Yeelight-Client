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

        /// <summary>
        /// Model of the device , ex: bulb / led stripe
        /// </summary>
        private string m_Model;

        public Device(string ip, string id, bool state, int bright, string model)
        {
            m_Id = id;
            m_Ip = ip;
            m_State = state;
            m_Brightness = bright;
            m_Model = model;
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
            get
            {
                switch (m_Model)
                {
                    case "color":
                        return "/YeelightController;component/Ressources/bulb.jpg";
                  
                    case "stripe":
                        return "/YeelightController;component/Ressources/ledstripe.png";
                     
                    default:
                        return null;                
                }
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
