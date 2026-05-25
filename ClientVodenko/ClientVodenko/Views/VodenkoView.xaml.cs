using Client.Models;
using ClientVodenko.ViewModel;
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
using System.Windows.Threading;
using System.ComponentModel;

namespace ClientVodenko.Views
{
    /// <summary>
    /// Interaction logic for VodenkoView.xaml
    /// </summary>
    public partial class VodenkoView : UserControl
    {
        private readonly VodenkoViewModel _vm;
        private readonly DispatcherTimer _animTimer;

        private const double ElRestY = -5;
        private const double ElActiveY = 90.0;
        private const double ElHeight = 110.0;
        private const double MaxFillH = 173.0;

        public VodenkoView()
        {
            InitializeComponent();

            _vm = new VodenkoViewModel();
            DataContext = _vm;

            _vm.StartPolling();
            Loaded += async (s, e) => await LoadPlcConfig();

            _animTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) };
            _animTimer.Tick += (s, e) => DrawFrame();
            _animTimer.Start();
        }

        private void DrawFrame()
        {
            DrawLeds();
            DrawAlarmBanners();
            CurrentTimeText.Text = DateTime.Now.ToString("HH:mm:ss");
            CurrentDateText.Text = DateTime.Now.ToString("dd.MM.yyyy");
        }

        private void DrawLeds()
        {
            var green = new SolidColorBrush(Color.FromRgb(76, 175, 80));
            var red = new SolidColorBrush(Color.FromRgb(244, 67, 54));
            var gray = new SolidColorBrush(Color.FromRgb(68, 68, 68));

            LedPlc.Fill = _vm.IsConnected ? green : red;
            LedBackend.Fill = green;
            LedDatabase.Fill = green;

            ConnStatusLed.Fill = _vm.IsConnected ? green : red;
            ConnStatusText.Text = _vm.IsConnected ? "Connected" : "Not connected";
        }

        private void DrawAlarmBanners()
        {
        }



        private void ResetBtn_Click(object sender, RoutedEventArgs e)
        {
            _vm.ResetCommand.Execute(null);
            ValveSlider.Value = 0;
            LevelSetpointInput.Text = "0";
        }

        private void SetLevelBtn_Click(object sender, RoutedEventArgs e)
        {
            if (float.TryParse(LevelSetpointInput.Text, out float val))
                _vm.LevelSetpoint = Math.Max(0, val);
            _vm.SetSetpointCommand.Execute(null);
        }


        private void ValveSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (TapAngleLabel == null) return;
            TapAngleLabel.Text = $"{ValveSlider.Value:F1}%";
            _vm.SetValvePositionCommand.Execute(null);
        }


        private async void ConnectBtn_Click(object sender, RoutedEventArgs e)
        {
            string ip = IpInput.Text.Trim();
            string rack = RackInput.Text.Trim();
            string slot = SlotInput.Text.Trim();

            if (string.IsNullOrWhiteSpace(ip))
            {
                MessageBox.Show("Please enter an IP address.");
                return;
            }

            ConnectBtn.IsEnabled = false;
            ConnStatusText.Text = "Connecting...";
            ConnStatusLed.Fill = new SolidColorBrush(Color.FromRgb(255, 152, 0));

            string cpuString = (CpuTypeInput.SelectedItem as ComboBoxItem)?.Content?.ToString();
            PLCDto.CpuType cpu;
            if (cpuString == "S7-1200") cpu = PLCDto.CpuType.S71200;
            else if (cpuString == "S7-400") cpu = PLCDto.CpuType.S7400;
            else if (cpuString == "S7-300") cpu = PLCDto.CpuType.S7300;
            else cpu = PLCDto.CpuType.S71500;

            var plcDto = new PLCDto
            {
                Ip = ip,
                Rack = int.TryParse(rack, out int r) ? r : 0,
                Slot = int.TryParse(slot, out int s) ? s : 1,
                Cpu = cpu
            };
            try
            {
                await Task.Delay(1000);
                _vm.ManuallyDisconnected = false;
                _vm.UpdatePlcCommand.Execute(plcDto);
                //_vm.StartPolling()

                ConnStatusLed.Fill = _vm.IsConnected
                    ? new SolidColorBrush(Color.FromRgb(76, 175, 80))
                    : new SolidColorBrush(Color.FromRgb(244, 67, 54));
                ConnStatusText.Text = _vm.IsConnected ? "Connected" : "PLC not reachable";
                PlcAddressText.Text = $"{ip} | Rack {rack} | Slot {slot}";
                CpuText.Text = $"CPU: {cpuString}";
                ConnectBtn.IsEnabled = false;
                DisconnectBtn.IsEnabled = true;
            }
            catch
            {
                ConnStatusLed.Fill = new SolidColorBrush(Color.FromRgb(244, 67, 54));
                ConnStatusText.Text = "Connection failed";
                ConnectBtn.IsEnabled = true;
            }
        }
        private void DisconnectBtn_Click(object sender, RoutedEventArgs e)
        {
            _vm.ManuallyDisconnected = true;
            _vm.IsConnected = false;

            ConnectBtn.IsEnabled = true;
            DisconnectBtn.IsEnabled = false;
            PlcAddressText.Text = "Not connected";
            CpuText.Text = "CPU: —";
        }

        private async Task LoadPlcConfig()
        {
            try
            {
                var proxy = new ClientVodenko.Proxies.VodenkoProxy();
                PLCDto plc = await proxy.GetPlcAsync();
                IpInput.Text = plc.Ip;
                RackInput.Text = plc.Rack.ToString();
                SlotInput.Text = plc.Slot.ToString();

                string cpuName;
                if (plc.Cpu == PLCDto.CpuType.S71200) cpuName = "S7-1200";
                else if (plc.Cpu == PLCDto.CpuType.S7400) cpuName = "S7-400";
                else if (plc.Cpu == PLCDto.CpuType.S7300) cpuName = "S7-300";
                else cpuName = "S7-1500";

                foreach (ComboBoxItem item in CpuTypeInput.Items)
                    if (item.Content.ToString() == cpuName)
                    { CpuTypeInput.SelectedItem = item; break; }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading PLC configuration: {ex.Message}");
            }
        }

        private void ShowPage(Grid page)
        {
            ProcessPage.Visibility = Visibility.Collapsed;
            ConnectionPage.Visibility = Visibility.Collapsed;
            HistoryPage.Visibility = Visibility.Collapsed;
            AlarmsPage.Visibility = Visibility.Collapsed;
            page.Visibility = Visibility.Visible;

            var gray = (Brush)new BrushConverter().ConvertFrom("#222222");
            ProcessBtn.Background = gray;
            ConnectionBtn.Background = gray;
            HistoryBtn.Background = gray;
            AlarmsBtn.Background = gray;
        }

        private void ProcessBtn_Click(object sender, RoutedEventArgs e)
        { ShowPage(ProcessPage); ProcessBtn.Background = (Brush)new BrushConverter().ConvertFrom("DimGray"); }

        private void ConnectionBtn_Click(object sender, RoutedEventArgs e)
        { ShowPage(ConnectionPage); ConnectionBtn.Background = (Brush)new BrushConverter().ConvertFrom("DimGray"); }

        private void HistoryBtn_Click(object sender, RoutedEventArgs e)
        { ShowPage(HistoryPage); HistoryBtn.Background = (Brush)new BrushConverter().ConvertFrom("DimGray"); }

        private void AlarmsBtn_Click(object sender, RoutedEventArgs e)
        { ShowPage(AlarmsPage); AlarmsBtn.Background = (Brush)new BrushConverter().ConvertFrom("DimGray"); }
    } 
}
