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

        // 1. POPRAVLJENE KONSTANTE: Prilagođene novom, spuštenom i povećanom spremniku
        private const double MinWaterTop = 330.0;    // Novo dno spremnika u pikselima
        private const double MaxWaterHeight = 266.0; // Nova maksimalna visina vode (100%)

        // Lokalna varijabla koja prati je li korisnik stisnuo START
        private bool _isProcessRunning = false;

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
            if (WaterFill == null || WaterLevelText == null || ValveGate == null || OutflowStream == null || InflowStream == null)
                return;

            // 1. ZAKLJUČAVANJE TOGGLE BUTTONA: Ako proces punjenja radi, onemogući klikanje na gumb za modove
            if (ModeToggle != null)
            {
                ModeToggle.IsEnabled = !_isProcessRunning;
            }

            // 2. KONTROLA UKLJUČENOSTI POLJA (READ-ONLY / ISENABLED BLOCK)
            if (ModeToggle != null)
            {
                bool isAuto = ModeToggle.IsChecked == true;

                // Ako smo u AUTO modu: 
                // -> Omogući unos setpointa nivoa
                // -> ZAKLJUČAJ slider ventila (operater ga ne može ni pomaknuti)
                if (isAuto)
                {
                    if (LevelSetpointInput != null) LevelSetpointInput.IsReadOnly = false;
                    if (SetLevelBtn != null) SetLevelBtn.IsEnabled = true;

                    if (ValveSlider != null) ValveSlider.IsEnabled = false; // Slider postaje siv i zaključan
                }
                // Ako smo u MANUAL modu:
                // -> ZAKLJUČAJ unos setpointa nivoa (TextBox stavljamo na ReadOnly, gumb gasimo)
                // -> Omogući slider ventila
                else
                {
                    if (LevelSetpointInput != null) LevelSetpointInput.IsReadOnly = true; // TextBox se ne može uređivati
                    if (SetLevelBtn != null) SetLevelBtn.IsEnabled = false;      // Gumb postaje siv

                    if (ValveSlider != null) ValveSlider.IsEnabled = true;  // Slider se može micati
                }
            }

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
            //Canvas.SetTop(WaterLevelText, Math.Max(newTop - 25, 75)); // Prilagođeno novom vrhu (70)

            // AUTOMATSKI IZLAZ VODE NA DNU: Ako u spremniku ima vode, ona sama teče van kroz donju cijev
            if (percentage > 0.1)
            {
                OutflowStream.Opacity = 0.8;
            }
            else
            {
                OutflowStream.Opacity = 0;
            }
        }

        private void DrawValveAndOutflow()
        {
            double valvePct = _vm.ValvePosition;

            if (double.IsNaN(valvePct) || double.IsInfinity(valvePct) || valvePct < 0 || valvePct > 100000)
            {
                valvePct = 0;
            }
            if (valvePct > 100) valvePct = 100;

            // Animacija zasuna ventila (Lijevo - Desno na novoj poziciji):
            double valveOffset = (valvePct / 100.0) * 24.0;
            Canvas.SetLeft(ValveGate, 120 - valveOffset);

            // KONTROLA MLAZA VODE (PIPE):
            // Voda curi samo ako je ventil otvoren (>1%) AND ako je sustav online AND ako je pritisnut START gumb
            if (valvePct > 1.0 && _vm.IsConnected && _vm.ConnectionStatus == "Connected" && _isProcessRunning)
            {
                InflowStream.Opacity = 0.85; // Otvor je slobodan, proces radi -> VODA TEČE!
            }
            else
            {
                InflowStream.Opacity = 0;    // Zaustavljeno ili zatvoreno -> NEMA VODE
            }
            // Dodaj ovo na kraj metode DrawValveAndOutflow() u xaml.cs fajlu:
            if (ValveOpeningText != null)
            {
                ValveOpeningText.Text = $"{valvePct:F1}%";
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
    // Definiramo boje za upaljeno (alarm) i ugašeno stanje lampica
    var redBrush = new SolidColorBrush(Color.FromRgb(244, 67, 54));       // Crvena za Overfill
    var orangeBrush = new SolidColorBrush(Color.FromRgb(255, 152, 0));   // Narančasta za Warninge
    var darkGrayBrush = new SolidColorBrush(Color.FromRgb(68, 68, 68));   // Tamno siva (#444444) kada je sve OK

    // 1. TANK OVERFILL ALARM
    if (OverfillBanner != null)
        OverfillBanner.Visibility = _vm.TankOverfill ? Visibility.Visible : Visibility.Collapsed;
    
    if (LedOverfill != null)
        LedOverfill.Fill = _vm.TankOverfill ? redBrush : darkGrayBrush;


    // 2. INVALID SETPOINT WARNING
    if (InvalidSetpointBanner != null)
        InvalidSetpointBanner.Visibility = _vm.SetpointInvalid ? Visibility.Visible : Visibility.Collapsed;
    
    if (LedSetpoint != null)
        LedSetpoint.Fill = _vm.SetpointInvalid ? orangeBrush : darkGrayBrush;


    // 3. MANUAL VALVE INVALID WARNING
    if (InvalidValveBanner != null)
        InvalidValveBanner.Visibility = _vm.ManualValveInvalid ? Visibility.Visible : Visibility.Collapsed;
    
    if (LedValve != null)
        LedValve.Fill = _vm.ManualValveInvalid ? orangeBrush : darkGrayBrush;
}

        // Kada se stisne RESET, gasimo proces i vraćamo kontrole na nulu
        private void ResetBtn_Click(object sender, RoutedEventArgs e)
        {
            _isProcessRunning = false; // Zaustavi grafički tok vode odmah

            // Vrati START gumb u prvobitno zeleno stanje
            if (StartStopBtn != null)
            {
                StartStopBtn.Content = "START";
                StartStopBtn.Background = new SolidColorBrush(Color.FromRgb(69, 171, 38));

                _vm.StopCommand.Execute(null);
            }

            // 1. Izvrši reset na PLC-u
            _vm.ResetCommand.Execute(null);


            // 2. KLJUČNI KORAK: Prisili ViewModel da odmah spusti interne vrijednosti na 0
            if (_vm != null)
            {
                _vm.ValvePositionSetpoint = 0;
                _vm.LevelSetpoint = 0;
                _vm.ValvePosition = 0; // Ovo će ugasiti "ludu" staru vrijednost ventila
                _vm.ActualLevel = 0;
            }

            // 3. Resetiraj elemente sučelja
            if (ValveSlider != null) ValveSlider.Value = 0;
            if (LevelSetpointInput != null) LevelSetpointInput.Text = "0";

            // 4. Pozovi odmah metodu za crtanje da postavi crveni pravokutnik na zatvorenu poziciju (120)
            DrawValveAndOutflow();
        }

        private void StartBtn_Click(object sender, RoutedEventArgs e)
        {
            var greenBrush = new SolidColorBrush(Color.FromRgb(69, 171, 38));
            var redBrush = new SolidColorBrush(Color.FromRgb(244, 67, 54));

            if (!_isProcessRunning)
            {
                _isProcessRunning = true;

                StartStopBtn.Content = "STOP";
                StartStopBtn.Background = redBrush;

                _vm.StartCommand.Execute(null);
            }
            else
            {
                _isProcessRunning = false;

                StartStopBtn.Content = "START";
                StartStopBtn.Background = greenBrush;

                _vm.StopCommand.Execute(null);
            }
        }

        private void SetLevelBtn_Click(object sender, RoutedEventArgs e)
        {
            // Sigurnosna provjera (ako operater nekako zaobiđe UI)
            if (ModeToggle != null && ModeToggle.IsChecked != true)
            {
                return;
            }

            if (LevelSetpointInput != null && float.TryParse(LevelSetpointInput.Text, out float val))
                _vm.LevelSetpoint = Math.Max(0, val);

            _vm.SetSetpointCommand.Execute(null);
        }

        private void ValveSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (TapAngleLabel == null) return;

            // Sigurnosna provjera (ako operater nekako zaobiđe UI)
            if (ModeToggle != null && ModeToggle.IsChecked == true)
            {
                if (_vm != null)
                {
                    ValveSlider.Value = _vm.ValvePosition;
                }
                return;
            }

            TapAngleLabel.Text = $"{ValveSlider.Value:F1}%";

            _vm.ValvePositionSetpoint = (float)ValveSlider.Value;
            _vm.SetValvePositionCommand.Execute(null);
        }

        private async void ModeToggle_Checked(object sender, RoutedEventArgs e)
        {
            try { await _vm.WriteBoolModeAsync("automatic_manual", false); }
            catch (Exception ex) { _vm.ConnectionStatus = $"Error: {ex.Message}"; }
        }

        private async void ModeToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            try { await _vm.WriteBoolModeAsync("automatic_manual", true); }
            catch (Exception ex) { _vm.ConnectionStatus = $"Error: {ex.Message}"; }
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
                _vm.UpdatePlcCommand.Execute(plcDto);

                await Task.Delay(1500);

                var green = new SolidColorBrush(Color.FromRgb(76, 175, 80));
                var red = new SolidColorBrush(Color.FromRgb(244, 67, 54));

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
            _isProcessRunning = false; // Zaustavi grafički proces pri diskonekciji
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

        private void BtnShowTable_Click(object sender, RoutedEventArgs e)
        {
            HistoryGrid.Visibility = Visibility.Visible;
            ChartsGrid.Visibility = Visibility.Collapsed;

            // aktivni button svijetli, neaktivni potamni
            BtnShowTable.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00BFFF"));
            BtnShowTable.Foreground = Brushes.White;
            BtnShowCharts.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2A2A2A"));
            BtnShowCharts.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#777777"));
        }

        private void BtnShowCharts_Click(object sender, RoutedEventArgs e)
        {
            HistoryGrid.Visibility = Visibility.Collapsed;
            ChartsGrid.Visibility = Visibility.Visible;

            BtnShowCharts.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00BFFF"));
            BtnShowCharts.Foreground = Brushes.White;
            BtnShowTable.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2A2A2A"));
            BtnShowTable.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#777777"));
        }

        private HistoryChartViewModel _chartVm = new HistoryChartViewModel();

        private async void BtnLoadChart_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int minutes = 60; // default
                if (ChartPeriod.SelectedItem is ComboBoxItem item && int.TryParse(item.Tag.ToString(), out int m))
                    minutes = m;

                var proxy = new ClientVodenko.Proxies.VodenkoProxy();
                List<VodenkoDto> podaci = await proxy.GetTrendsAsync(minutes);
                _chartVm.LoadData(podaci);
                TrendChart.DataContext = _chartVm;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Greška pri učitavanju: {ex.Message}");
            }
        }

    }
}