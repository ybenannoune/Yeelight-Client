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
        public static int port = 55443;
        private string m_Ip;
        private string m_Id;
        private bool m_State;
        private int m_Bright;

        public Bulb(string ip,string id)
        {
            m_Id = id;
            m_Ip = ip;
        }

        public Bulb(string ip, string id, bool state, int bright)
        {
            m_Id = id;
            m_Ip = ip;
            m_State = state;
            m_Bright = bright;
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
            get { return m_Bright; }
            set
            {
                m_Bright = value;
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
