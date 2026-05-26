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
using ClientVodenko.Models;

namespace ClientVodenko.Views
{
    public partial class VodenkoView : UserControl
    {
        private readonly VodenkoViewModel _vm;
        private readonly DispatcherTimer _animTimer;

        // Dimenzije našeg spremnika u XAML-u (Usklađeno s tvojim dizajnom)
        private const double MinWaterTop = 300.0;    // Dno posude
        private const double MaxWaterHeight = 220.0; // Maksimalna visina vode (100%)

        public VodenkoView()
        {
            InitializeComponent();

            // Zaštita za XAML dizajner da se Visual Studio ne ruši
            if (DesignerProperties.GetIsInDesignMode(this)) return;

            _vm = new VodenkoViewModel();
            DataContext = _vm;

            _vm.StartPolling();
            Loaded += async (s, e) => await LoadPlcConfig();

            // Pokretanje tajmera za iscrtavanje grafike (svakih 50ms)
            _animTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) };
            _animTimer.Tick += (s, e) => DrawFrame();
            _animTimer.Start();
        }

        private void DrawFrame()
        {
            // Osiguraj se da su XAML elementi spremni prije crtanja po njima
            if (WaterFill == null || WaterLevelText == null || ValveGate == null || OutflowStream == null)
                return;

            // 1. Nacrtaj grafiku koristeći točne podatke iz tvog ViewModela
            DrawWaterLevel();
            DrawValveAndOutflow();

            // 2. Nacrtaj LED-ice i Bannere
            DrawLeds();
            DrawAlarmBanners();

            // Provjera i ispis sata i datuma (ako ti TextBlock-ovi postoje u XAML-u)
            if (CurrentTimeText != null) CurrentTimeText.Text = DateTime.Now.ToString("HH:mm:ss");
            if (CurrentDateText != null) CurrentDateText.Text = DateTime.Now.ToString("dd.MM.yyyy");
        }

        private void DrawWaterLevel()
        {
            // Tvoj ViewModel šalje razinu u ActualLevel (pretpostavka je postotak 0 - 100%)
            double percentage = Math.Max(0, Math.Min(100, _vm.ActualLevel));

            // Izračunaj visinu plavog pravokutnika u pikselima
            double targetHeight = (percentage / 100.0) * MaxWaterHeight;

            // Postavi visinu vode
            WaterFill.Height = targetHeight;

            // Pomakni Top koordinatu (jer WPF crta odozgo prema dolje, pa drži dno fiksno na 300)
            double newTop = MinWaterTop - targetHeight;
            Canvas.SetTop(WaterFill, newTop);

            // Osvježi čisti postotak na ekranu
            WaterLevelText.Text = $"{percentage:F0}%";

            // Pomakni tekstualni postotak da uvijek lebdi točno iznad površine vode
            Canvas.SetTop(WaterLevelText, Math.Max(newTop - 25, 85));
        }

        private void DrawValveAndOutflow()
        {
            // Tvoj ViewModel drži poziciju ventila u ValvePosition (0 - 100%)
            double valvePct = Math.Max(0, Math.Min(100, _vm.ValvePosition));

            // Crveni zasun se pomiče maksimalno za 22 piksela
            double valveOffset = (valvePct / 100.0) * 22.0;

            // Pomakni crveni zasun ventila (Zatvoreno = dno cijevi na 277, Otvoreno = ide prema gore)
            Canvas.SetTop(ValveGate, 277 - valveOffset);

            // Ako je ventil imalo otvoren (više od 1%), prikaži izlazni slap vode, inače ga sakrij
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
            // Povezivanje alarma iz tvog ViewModela na XAML Bannere
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

            // Šaljemo vrijednost klizača u ViewModel (koji će okinuti SetValvePosition na proxyju)
            _vm.ValvePositionSetpoint = (float)ValveSlider.Value;
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
            PLCDto.CpuType cpu = cpuString == "S7-1200" ? PLCDto.CpuType.S71200 :
                                 cpuString == "S7-400" ? PLCDto.CpuType.S7400 :
                                 cpuString == "S7-300" ? PLCDto.CpuType.S7300 :
                                                          PLCDto.CpuType.S71500;

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

                var green = new SolidColorBrush(Color.FromRgb(76, 175, 80));
                var red = new SolidColorBrush(Color.FromRgb(244, 67, 54));

                ConnStatusLed.Fill = _vm.IsConnected ? green : red;
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

                string cpuName = plc.Cpu == PLCDto.CpuType.S71200 ? "S7-1200" :
                                 plc.Cpu == PLCDto.CpuType.S7400 ? "S7-400" :
                                 plc.Cpu == PLCDto.CpuType.S7300 ? "S7-300" : "S7-1500";

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