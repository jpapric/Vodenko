using ClientVodenko.Helpers;
using ClientVodenko.Models;
using ClientVodenko.Proxies;
using Client;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Remoting.Proxies;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;
using Client.Models;

namespace ClientVodenko.ViewModel
{
    public class VodenkoViewModel : INotifyPropertyChanged
    {
        private readonly VodenkoProxy _proxy = new VodenkoProxy();
        private readonly DispatcherTimer _timer;

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        #region PLC Data Fields

        private float _actualLevel;
        private DateTime _timeSaved;
        private float _valvePosition;
        private bool _setpointInvalid;
        private bool _manualValveInvalid;
        private bool _tankOverfill;

        private bool _manuallyDisconnected = false;

        #endregion

        #region PLC Data Properties

        public float ActualLevel
        {
            get => _actualLevel;
            set
            {
                _actualLevel = value;
                OnPropertyChanged();
            }
        }

        public DateTime TimeSaved
        {
            get => _timeSaved;
            set
            {
                _timeSaved = value;
                OnPropertyChanged();
            }
        }

        public float ValvePosition
        {
            get => _valvePosition;
            set
            {
                _valvePosition = value;
                OnPropertyChanged();
            }
        }

        public bool SetpointInvalid
        {
            get => _setpointInvalid;
            set
            {
                _setpointInvalid = value;
                OnPropertyChanged();
            }
        }

        public bool ManualValveInvalid
        {
            get => _manualValveInvalid;
            set
            {
                _manualValveInvalid = value;
                OnPropertyChanged();
            }
        }

        public bool TankOverfill
        {
            get => _tankOverfill;
            set
            {
                _tankOverfill = value;
                OnPropertyChanged();
            }
        }

        public bool ManuallyDisconnected
        {
            get => _manuallyDisconnected;
            set
            {
                _manuallyDisconnected = value;
                OnPropertyChanged();
            }
        }

        #endregion

        #region Setpoints

        private float _levelSetpoint;
        private float _valvePositionSetpoint;

        public float LevelSetpoint
        {
            get => _levelSetpoint;
            set
            {
                _levelSetpoint = value;
                OnPropertyChanged();
            }
        }

        public float ValvePositionSetpoint
        {
            get => _valvePositionSetpoint;
            set
            {
                _valvePositionSetpoint = value;
                OnPropertyChanged();
            }
        }

        #endregion

        #region Connection

        private bool _isConnected;
        private string _connectionStatus = "Disconnected";

        public bool IsConnected
        {
            get => _isConnected;
            set
            {
                _isConnected = value;
                OnPropertyChanged();
            }
        }

        public string ConnectionStatus
        {
            get => _connectionStatus;
            set
            {
                _connectionStatus = value;
                OnPropertyChanged();
            }
        }

        #endregion

        #region History

        private ObservableCollection<EventDto> _events = new ObservableCollection<EventDto>();
        public EventDto lastEvent;
        public string LastEventText => lastEvent == null ? "—" :
                                        $"{lastEvent.Name}";

        public string LastEventTime => lastEvent == null ? "—" :
                                        $"{lastEvent.Type} — {lastEvent.Time:dd.MM.yyyy HH:mm:ss}";

        public ObservableCollection<EventDto> Events
        {
            get => _events;
            set { _events = value; OnPropertyChanged(); }
        }

        #endregion

        #region Commands

        public ICommand ResetCommand { get; }
        public ICommand StartCommand { get; }
        public ICommand UpdatePlcCommand { get; }
        public ICommand SetValvePositionCommand { get; }
        public ICommand SetSetpointCommand { get; }

        #endregion

        #region Constructor

        public VodenkoViewModel()
        {
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(150)
            };

            _timer.Tick += async (s, e) => await PollAsync();



            ResetCommand = new AsyncCommand(Reset);
            StartCommand = new AsyncCommand(Start);
            SetValvePositionCommand = new AsyncCommand(SetValvePosition);
            SetSetpointCommand = new AsyncCommand(SetSetpoint);
            UpdatePlcCommand = new AsyncCommand<L2ToPlcDto>(UpdatePlc);


        }

        #endregion

        #region Polling



        #endregion

        #region Command Methods






        /* VALJDA VALJA !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!! */
        public void StartPolling()
        {
            _timer.Start();
        }

        public void StopPolling()
        {
            _timer.Stop();
        }

        private int _loopCounter = 0; // Dodaj ovu varijablu na vrh klase ViewModel, izvan metode

        private async Task PollAsync()
        {
            if (_manuallyDisconnected)
            {
                IsConnected = false;
                return;
            }

            try
            {
                // 1. Povuci podatke asinkrono u pozadini (ovo radi super)
                VodenkoDto data = await _proxy.GetVodenkoDataFromPlcAsync();

                if (data == null)
                {
                    IsConnected = false;
                    ConnectionStatus = "No data";
                    return;
                }

                // 2. KLJUČNI POPRAVAK: Prebacujemo upisivanje podataka na UI Thread!
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    // Filtriramo lude brojeve i odmah punimo svojstva na glavnom threadu
                    ActualLevel = (data.Actual_Level < 0 || float.IsNaN(data.Actual_Level) || data.Actual_Level > 100000) ? 0 : data.Actual_Level;
                    ValvePosition = (data.Valve_Position < 0 || float.IsNaN(data.Valve_Position) || data.Valve_Position > 100000) ? 0 : data.Valve_Position;
                    TimeSaved = data.Time_Saved;
                });

                // 3. Teški podaci iz baze (alarmi i trendovi) svakih 3 sekunde
                _loopCounter++;
                if (_loopCounter >= 20)
                {
                    _loopCounter = 0;

                    List<AlarmsDto> alarmsList = await _proxy.GetAlarmsAsync(30);

                    // Ponovno koristimo Dispatcher za alarmne lampice
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        if (alarmsList != null && alarmsList.Count > 0)
                        {
                            var zadnjiAlarm = alarmsList.First();
                            SetpointInvalid = zadnjiAlarm.Setpoint_Invalid;
                            ManualValveInvalid = zadnjiAlarm.Manual_Valve_Invalid;
                            TankOverfill = zadnjiAlarm.Tank_Overfill;
                        }
                        else
                        {
                            SetpointInvalid = false;
                            ManualValveInvalid = false;
                            TankOverfill = false;
                        }
                    });

                    await RefreshEventsAsync();
                }

                // Sve je prošlo u redu
                IsConnected = true;
                ConnectionStatus = "Connected";
            }
            catch (Exception ex)
            {
                IsConnected = false;
                ConnectionStatus = $"Error: {ex.Message}";
            }
        }

        private async Task UpdatePlc(L2ToPlcDto controlDto)
        {
            try
            {
                await _proxy.UpdateControlRowAsync(controlDto);
            }
            catch (Exception ex)
            {
                ConnectionStatus = $"Error: {ex.Message}";
            }
        }

        private async Task RefreshEventsAsync()
        {
            try
            {
                var trends = await _proxy.GetTrendsAsync(20);
                if (trends == null || !trends.Any()) return;

                var mappedEvents = trends.Select(t => new EventDto
                {
                    Name = $"Level: {t.Actual_Level} | Valve: {t.Valve_Position}",
                    Time = t.Time_Saved,
                    Type = "Trend"
                }).ToList();

                Events = new ObservableCollection<EventDto>(mappedEvents);

                lastEvent = Events.First();
                OnPropertyChanged(nameof(LastEventText));
                OnPropertyChanged(nameof(LastEventTime));
            }
            catch
            {
            }
        }







        private async Task Start()
        {
            try
            {
                await _proxy.WriteBoolToPlcAsync("Start_pump", true);
            }
            catch (Exception ex)
            {
                ConnectionStatus = $"Error: {ex.Message}";
            }
        }

        private async Task Reset()
        {
            try
            {
                await _proxy.SetResetPulseAsync();

                ConnectionStatus = "Connected";
            }
            catch (Exception ex)
            {
                ConnectionStatus = $"Error: {ex.Message}";
            }
        }

        private async Task SetSetpoint()
        {
            try
            {
                L2ToPlcDto controlDto = new L2ToPlcDto
                {
                    Level_Setpoint = LevelSetpoint
                };

                await _proxy.UpdateControlRowAsync(controlDto);
            }
            catch (Exception ex)
            {
                ConnectionStatus = $"Error: {ex.Message}";
            }
        }

        private async Task SetValvePosition()
        {
            try
            {
                // Šaljemo naziv taga za poziciju ventila na PLC-u i vrijednost setpointa
                await _proxy.WriteRealToPlcAsync("Manual_valve_value", ValvePositionSetpoint);
            }
            catch (Exception ex)
            {
                ConnectionStatus = $"Error: {ex.Message}";
            }
        }

        public async Task WriteBoolModeAsync(string variable, bool state)
        {
            await _proxy.WriteBoolToPlcAsync(variable, state);
        }

        /* VALJDA VALJA !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!! */




        
        #endregion
    }
}
