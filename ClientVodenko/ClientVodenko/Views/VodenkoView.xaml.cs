using ClientVodenko.Models;
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
    public partial class VodenkoView : UserControl
    {
        private readonly VodenkoViewModel _vm;
        private readonly DispatcherTimer _animTimer;

        private const double MinWaterTop = 300.0;
        private const double MaxWaterHeight = 220.0;

        public VodenkoView()
        {
            InitializeComponent();

            if (DesignerProperties.GetIsInDesignMode(this)) return;

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
            // Sigurnosna provjera XAML elemenata
            if (WaterFill == null || WaterLevelText == null || ValveGate == null || OutflowStream == null)
                return;

            DrawWaterLevel();
            DrawValveAndOutflow();
            DrawLeds();
            DrawAlarmBanners();

            if (CurrentTimeText != null) CurrentTimeText.Text = DateTime.Now.ToString("HH:mm:ss");
            if (CurrentDateText != null) CurrentDateText.Text = DateTime.Now.ToString("dd.MM.yyyy");
        }

        private void DrawWaterLevel()
        {
            double percentage = _vm.ActualLevel;

            // POPRAVLJENO: Filtriranje ludih i ogromnih negativnih brojeva (smetnji)
            if (double.IsNaN(percentage) || double.IsInfinity(percentage) || percentage < 0 || percentage > 100000)
            {
                percentage = 0;
            }
            if (percentage > 100) percentage = 100;

            double targetHeight = (percentage / 100.0) * MaxWaterHeight;
            WaterFill.Height = targetHeight;

            double newTop = MinWaterTop - targetHeight;
            Canvas.SetTop(WaterFill, newTop);

            WaterLevelText.Text = $"{percentage:F1}%";
            Canvas.SetTop(WaterLevelText, Math.Max(newTop - 25, 85));
        }

        private void DrawValveAndOutflow()
        {
            double valvePct = _vm.ValvePosition;

            // POPRAVLJENO: Filtriranje ludih i ogromnih negativnih brojeva za ventil
            if (double.IsNaN(valvePct) || double.IsInfinity(valvePct) || valvePct < 0 || valvePct > 100000)
            {
                valvePct = 0;
            }
            if (valvePct > 100) valvePct = 100;

            double valveOffset = (valvePct / 100.0) * 22.0;
            Canvas.SetTop(ValveGate, 277 - valveOffset);

            if (valvePct > 1.0)
            {
                OutflowStream.Opacity = 0.8;
            }
            else
            {
                OutflowStream.Opacity = 0;
            }
        }

        private void DrawLeds()
        {
            var green = new SolidColorBrush(Color.FromRgb(76, 175, 80));
            var red = new SolidColorBrush(Color.FromRgb(244, 67, 54));

            if (LedPlc != null) LedPlc.Fill = _vm.IsConnected ? green : red;
            if (LedBackend != null) LedBackend.Fill = green;
            if (LedDatabase != null) LedDatabase.Fill = green;

            if (ConnStatusLed != null) ConnStatusLed.Fill = _vm.IsConnected ? green : red;
            if (ConnStatusText != null) ConnStatusText.Text = _vm.IsConnected ? "Connected" : "Not connected";
        }

        private void DrawAlarmBanners()
        {
            if (OverfillBanner != null)
                OverfillBanner.Visibility = _vm.TankOverfill ? Visibility.Visible : Visibility.Collapsed;

            if (InvalidSetpointBanner != null)
                InvalidSetpointBanner.Visibility = _vm.SetpointInvalid ? Visibility.Visible : Visibility.Collapsed;

            if (InvalidValveBanner != null)
                InvalidValveBanner.Visibility = _vm.ManualValveInvalid ? Visibility.Visible : Visibility.Collapsed;
        }

        private void ResetBtn_Click(object sender, RoutedEventArgs e)
        {
            _vm.ResetCommand.Execute(null);
            if (ValveSlider != null) ValveSlider.Value = 0;
            if (LevelSetpointInput != null) LevelSetpointInput.Text = "0";
        }

        private void StartBtn_Click(object sender, RoutedEventArgs e)
        {
            _vm.StartCommand.Execute(null);
        }

        private void SetLevelBtn_Click(object sender, RoutedEventArgs e)
        {
            if (LevelSetpointInput != null && float.TryParse(LevelSetpointInput.Text, out float val))
                _vm.LevelSetpoint = Math.Max(0, val);

            _vm.SetSetpointCommand.Execute(null);
        }

        private void ValveSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (TapAngleLabel == null) return;
            TapAngleLabel.Text = $"{ValveSlider.Value:F1}%";

            _vm.ValvePositionSetpoint = (float)ValveSlider.Value;
            _vm.SetValvePositionCommand.Execute(null);
        }

        private async void ModeToggle_Checked(object sender, RoutedEventArgs e)
        {
            try { await _vm.WriteBoolModeAsync("System_Auto_Mode", true); }
            catch (Exception ex) { _vm.ConnectionStatus = $"Error: {ex.Message}"; }
        }

        private async void ModeToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            try { await _vm.WriteBoolModeAsync("System_Auto_Mode", false); }
            catch (Exception ex) { _vm.ConnectionStatus = $"Error: {ex.Message}"; }
        }

        // POPRAVLJENO I OPTIMIZIRANO: Spajanje sada uredno čeka odgovor s PLC-a
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
            ConnStatusLed.Fill = new SolidColorBrush(Color.FromRgb(255, 152, 0)); // Narančasto (spajanje u tijeku)

            string cpuString = (CpuTypeInput.SelectedItem as ComboBoxItem)?.Content?.ToString();
            PlcDto.CpuType cpu = cpuString == "S7-1200" ? PlcDto.CpuType.S71200 :
                                 cpuString == "S7-400" ? PlcDto.CpuType.S7400 :
                                 cpuString == "S7-300" ? PlcDto.CpuType.S7300 :
                                                          PlcDto.CpuType.S71500;

            var plcDto = new PlcDto
            {
                Ip = ip,
                Rack = int.TryParse(rack, out int r) ? r : 0,
                Slot = int.TryParse(slot, out int s) ? s : 1,
                Cpu = cpu
            };

            try
            {
                _vm.ManuallyDisconnected = false;

                // 1. Šaljemo nove postavke na API
                _vm.UpdatePlcCommand.Execute(plcDto);

                // 2. DAJEMO SUSTAVU 1.5 SEKUNDU DA ODRADI SPAJANJE NA PLC U POZADINI
                await Task.Delay(1500);

                var green = new SolidColorBrush(Color.FromRgb(76, 175, 80));
                var red = new SolidColorBrush(Color.FromRgb(244, 67, 54));

                // 3. Tek sada provjeravamo je li pozadinski tajmer uspio pročitati podatke
                if (_vm.IsConnected)
                {
                    ConnStatusLed.Fill = green;
                    ConnStatusText.Text = "Connected";
                    PlcAddressText.Text = $"{ip} | Rack {rack} | Slot {slot}";
                    CpuText.Text = $"CPU: {cpuString}";
                    ConnectBtn.IsEnabled = false;
                    DisconnectBtn.IsEnabled = true;
                }
                else
                {
                    // Ako nakon 1.5s i dalje nema podataka, javi da PLC nije dostupan
                    ConnStatusLed.Fill = red;
                    ConnStatusText.Text = "PLC not reachable";
                    ConnectBtn.IsEnabled = true;
                }
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
                PlcDto plc = await proxy.GetPlcAsync();
                if (plc == null) return;

                IpInput.Text = plc.Ip;
                RackInput.Text = plc.Rack.ToString();
                SlotInput.Text = plc.Slot.ToString();

                string cpuName = plc.Cpu == PlcDto.CpuType.S71200 ? "S7-1200" :
                                 plc.Cpu == PlcDto.CpuType.S7400 ? "S7-400" :
                                 plc.Cpu == PlcDto.CpuType.S7300 ? "S7-300" : "S7-1500";

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
            if (ProcessPage != null) ProcessPage.Visibility = Visibility.Collapsed;
            if (ConnectionPage != null) ConnectionPage.Visibility = Visibility.Collapsed;
            if (HistoryPage != null) HistoryPage.Visibility = Visibility.Collapsed;
            if (AlarmsPage != null) AlarmsPage.Visibility = Visibility.Collapsed;

            if (page != null) page.Visibility = Visibility.Visible;

            var gray = (Brush)new BrushConverter().ConvertFrom("#222222");
            if (ProcessBtn != null) ProcessBtn.Background = gray;
            if (ConnectionBtn != null) ConnectionBtn.Background = gray;
            if (HistoryBtn != null) HistoryBtn.Background = gray;
            if (AlarmsBtn != null) AlarmsBtn.Background = gray;
        }

        private void ProcessBtn_Click(object sender, RoutedEventArgs e)
        { ShowPage(ProcessPage); if (ProcessBtn != null) ProcessBtn.Background = (Brush)new BrushConverter().ConvertFrom("DimGray"); }

        private void ConnectionBtn_Click(object sender, RoutedEventArgs e)
        { ShowPage(ConnectionPage); if (ConnectionBtn != null) ConnectionBtn.Background = (Brush)new BrushConverter().ConvertFrom("DimGray"); }

        private void HistoryBtn_Click(object sender, RoutedEventArgs e)
        { ShowPage(HistoryPage); if (HistoryBtn != null) HistoryBtn.Background = (Brush)new BrushConverter().ConvertFrom("DimGray"); }

        private void AlarmsBtn_Click(object sender, RoutedEventArgs e)
        { ShowPage(AlarmsPage); if (AlarmsBtn != null) AlarmsBtn.Background = (Brush)new BrushConverter().ConvertFrom("DimGray"); }
    }
}