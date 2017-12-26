using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Controls;
using System.Windows.Input;
using MahApps.Metro.Controls;

namespace YeelightController
{
    /// <summary>
    /// Logique d'interaction pour MainWindow.xaml
    /// </summary>
    /// 
    public partial class MainWindow : MetroWindow
    {
        private DevicesDiscovery m_DevicesDiscovery;    
        private DeviceIO m_DeviceIO;
      
        public MainWindow()
        {
            InitializeComponent();

            m_DeviceIO = new DeviceIO();     
                 
            m_DevicesDiscovery = new DevicesDiscovery();
            m_DevicesDiscovery.StartListening();

            //Send Discovery Message
            m_DevicesDiscovery.SendDiscoveryMessage();

            //Bind the list to the ListView
            lstBulbs.ItemsSource = m_DevicesDiscovery.GetDiscoveredDevices();
        }

        private void lstBulbs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {         
            Device device = m_DevicesDiscovery.GetDiscoveredDevices()[lstBulbs.SelectedIndex];         
            panelBulbControl.IsEnabled = false;

            if (m_DeviceIO.Connect(device) == true)
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
                m_DeviceIO.Toggle();
            }
        }

        //LostCapture event is used, we don't want to spam the bulb at each change, just the final one
        private void sliderBrightness_LostMouseCapture(object sender, MouseEventArgs e)
        {
            if (panelBulbControl.IsEnabled)
            {
                int value = Convert.ToInt32(sliderBrightness.Value);
                int smooth = Convert.ToInt32(sliderSmooth.Value);
                m_DeviceIO.SetBrightness(value, smooth);
            }
        }

        //LostCapture event is used, we don't want to spam the bulb at each change, just the final one
        private void ColorCanvas_LostMouseCapture(object sender, MouseEventArgs e)
        {
            if (panelBulbControl.IsEnabled)
            {            
                int smooth = Convert.ToInt32(sliderSmooth.Value);
                m_DeviceIO.SetColor(colorCanvas.R, colorCanvas.G, colorCanvas.B, smooth);
            }
        }
    }
}
