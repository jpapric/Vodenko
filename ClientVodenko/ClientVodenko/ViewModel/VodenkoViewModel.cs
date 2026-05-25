using Client.Helpers;
using Client.Models;
using ClientVodenko.Models;
using ClientVodenko.Proxies;
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
            SetValvePositionCommand = new AsyncCommand(SetValvePosition);
            SetSetpointCommand = new AsyncCommand(SetSetpoint);
            UpdatePlcCommand = new AsyncCommand<PLCDto>(UpdatePlc);
        }

        #endregion

        #region Polling

        public void StartPolling()
        {
            _timer.Start();
        }

        public void StopPolling()
        {
            _timer.Stop();
        }

        private async Task PollAsync()
        {

            if (_manuallyDisconnected)
            {
                IsConnected = false;
                return;
            }

            try
            {
                VodenkoDto data = await _proxy.GetEafDataFromPlcAsync();

                if (data == null)
                {
                    IsConnected = false;
                    ConnectionStatus = "No data";
                    return;
                }

                SetpointInvalid = data.Setpoint_invalid;
                ManualValveInvalid = data.Manual_valve_invalid;
                TankOverfill = data.Tank_overfill;
                ActualLevel = data.Actual_level;
                ValvePosition = data.Valve_position;

                await RefreshEventsAsync();

                IsConnected = true;
                ConnectionStatus = "Connected";
            }
            catch (Exception ex)
            {
                IsConnected = false;
                ConnectionStatus = $"Error: {ex.Message}";
            }
        }
        private async Task RefreshEventsAsync()
        {
            try
            {
                var events = await _proxy.GetEventsAsync();
                if (events == null) return;

                Events = new ObservableCollection<EventDto>(events);

                lastEvent = Events.First();
                OnPropertyChanged(nameof(LastEventText));
                OnPropertyChanged(nameof(LastEventTime));
            }
            catch { }
        }

        #endregion

        #region Command Methods

        private async Task SetSetpoint()
        {
            try
            {
                await _proxy.SetSetpointAsync(LevelSetpoint);
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
                await _proxy.SetValvePositionAsync(ValvePositionSetpoint);
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
                await _proxy.ResetAsync();
            }
            catch (Exception ex)
            {
                ConnectionStatus = $"Error: {ex.Message}";
            }
        }

        private async Task UpdatePlc(PLCDto plcDto)
        {
            try { await _proxy.UpdatePlcAsync(plcDto); }
            catch (Exception ex) { ConnectionStatus = $"Error: {ex.Message}"; }
        }
        #endregion
    }
}
