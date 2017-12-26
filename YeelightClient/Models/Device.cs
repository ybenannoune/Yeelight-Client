using System.ComponentModel;
using System.Net;
using System.Runtime.CompilerServices;

namespace YeelightClient
{
    internal class Device : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private const int DefaultPort = 55443;

        private string ip;
        private string id;
        private bool state;
        private int brightness;
        private string model;

        public Device(string ip, string id, bool state, int brightness, string model)
        {
            this.id = id;
            this.ip = ip;
            this.state = state;
            this.brightness = brightness;
            this.model = model;
        }

        public string Ip
        {
            get
            {
                return this.ip;
            }
            set
            {
                this.ip = value;
                OnPropertyChanged();
            }
        }

        public string Id
        {
            get
            {
                return this.id;
            }
            set
            {
                this.id = value;
                OnPropertyChanged();
            }
        }

        public int Brightness
        {
            get
            {
                return this.brightness;
            }
            set
            {
                this.brightness = value;
                OnPropertyChanged();
            }
        }

        public bool State
        {
            get
            {
                return this.state;
            }
            set
            {
                this.state = value;
                OnPropertyChanged();
            }
        }

        public string ModelIcon
        {
            get
            {
                switch (model)
                {
                    case "color":
                        return "/YeelightClient;component/Ressources/bulb.jpg";
                    case "stripe":
                        return "/YeelightClient;component/Ressources/ledstripe.png";
                    default:
                        return null;
                }
            }
        }

        public IPEndPoint GetEndPoint()
        {
            return new IPEndPoint(IPAddress.Parse(this.ip), DefaultPort);
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}