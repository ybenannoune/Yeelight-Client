using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Net;

namespace YeelightController
{
    class Bulb : INotifyPropertyChanged
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

        public Bulb(string ip, string id, bool state, int bright)
        {
            m_Id = id;
            m_Ip = ip;
            m_State = state;
            m_Brightness = bright;
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
