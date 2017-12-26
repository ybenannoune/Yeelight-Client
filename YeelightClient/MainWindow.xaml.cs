using System;
using System.Windows.Controls;
using System.Windows.Input;
using MahApps.Metro.Controls;

namespace YeelightClient
{
    public partial class MainWindow : MetroWindow
    {
        private DeviceScanner deviceScanner;    
        private DeviceIO deviceIO;
      
        public MainWindow()
        {
            InitializeComponent();

            this.deviceIO = new DeviceIO();     
                 
            this.deviceScanner = new DeviceScanner();
            this.deviceScanner.StartListening();

            //Send Discovery Message
            this.deviceScanner.SendDiscoveryMessage();

            //Bind the list to the ListView
            lstBulbs.ItemsSource = this.deviceScanner.DiscoveredDevices;
        }

        private void lstBulbs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {         
            var device = this.deviceScanner.DiscoveredDevices[lstBulbs.SelectedIndex];         
            panelBulbControl.IsEnabled = false;

            if (this.deviceIO.Connect(device))
            {
                //Apply current device values to controls
                btnToggle.IsChecked = device.State;
                sliderBrightness.Value = device.Brightness;

                //Change panel state -> allow modification
                panelBulbControl.IsEnabled = true;
            }       
        }

        private void btnToggle_IsCheckedChanged(object sender, EventArgs e)
        {
            if (panelBulbControl.IsEnabled)
            {
                this.deviceIO.Toggle();
            }
        }

        //LostCapture event is used, we don't want to spam the bulb at each change, just the final one
        private void sliderBrightness_LostMouseCapture(object sender, MouseEventArgs e)
        {
            if (panelBulbControl.IsEnabled)
            {
                var value = Convert.ToInt32(sliderBrightness.Value);
                var smooth = Convert.ToInt32(sliderSmooth.Value);
                this.deviceIO.SetBrightness(value, smooth);
            }
        }

        //LostCapture event is used, we don't want to spam the bulb at each change, just the final one
        private void ColorCanvas_LostMouseCapture(object sender, MouseEventArgs e)
        {
            if (panelBulbControl.IsEnabled)
            {            
                var smooth = Convert.ToInt32(sliderSmooth.Value);
                this.deviceIO.SetColor(colorCanvas.R, colorCanvas.G, colorCanvas.B, smooth);
            }
        }
    }
}
