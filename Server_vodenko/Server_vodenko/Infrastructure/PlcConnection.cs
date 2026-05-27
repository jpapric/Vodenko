using S7.Net;
using Server_vodenko.Application.DTOs;
using Server_vodenko.Application.Interfaces;
using Server_vodenko.Domain;
using Server_vodenko.Infrastructure.Controllers;
using Server_vodenko.Infrastructure.Repository;
using static System.Net.Mime.MediaTypeNames;

namespace Server_vodenko.Infrastructure.BackgroundServices
{
    public class PlcConnection : BackgroundService
    {
        private readonly ILogger<PlcConnection> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private S7.Net.Plc? _plc;
        private readonly PlcDataCache _cache;
        public PlcConnection(ILogger<PlcConnection> logger, IServiceScopeFactory scopeFactory, PlcDataCache cache)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
            _cache = cache;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {

            Domain.Plc plcConfig = await FetchPlcConfigurationAsync(stoppingToken);
            if (plcConfig == null) return;

            if (!Enum.TryParse(plcConfig.Cpu, true, out CpuType cpuType))
            {
                _logger.LogWarning("Invalid CPU type '{Cpu}', defaulting to S71500.", plcConfig.Cpu);
                cpuType = CpuType.S71500;
            }

            _plc = new S7.Net.Plc(cpuType, plcConfig.Ip, (short)plcConfig.Rack, (short)plcConfig.Slot);

            await ConnectUntilSuccessAsync(stoppingToken);

            await MonitorConnectionAsync(stoppingToken);

            _plc.Close();
        }

        private async Task ConnectUntilSuccessAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("Attempting PLC connection...");
                    await _plc!.OpenAsync(stoppingToken);

                    if (_plc.IsConnected)
                    {
                        _logger.LogInformation("PLC connected successfully.");
                        return;
                    }
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogError("Connection failed: {Message}. Retrying in 3s...", ex.Message);
                }

                await Task.Delay(3000, stoppingToken);
            }
        }

        private async Task MonitorConnectionAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                if (!_plc!.IsConnected)
                {
                    _logger.LogWarning("PLC connection lost. Reconnecting...");
                    await ConnectUntilSuccessAsync(stoppingToken);
                }
                else
                {
                    ReadFromPlc();



                }

                await Task.Delay(1000, stoppingToken);
            }
        }

        private void ReadFromPlc()
        {
            try
            {
                try
                {
                    var vodenkoData = new Vodenko(0, 0, DateTime.Now);
                    _plc.ReadClass(vodenkoData, 101, 0);
                    _cache.Update(vodenkoData);

                    var alarmsData = new Alarms(false, false, false, DateTime.Now);
                    _plc.ReadClass(alarmsData, 101, 8);
                    _cache.UpdateAlarms(alarmsData);
                    bool test1 = (bool)_plc.Read("DB101.DBX8.0");
                    bool test2= (bool)_plc.Read("DB101.DBX8.1");
                    bool test3= (bool)_plc.Read("DB101.DBX8.2");
                    



                    using var scope = _scopeFactory.CreateScope();
                    var repository = scope.ServiceProvider.GetRequiredService<IVodenkoRepository>();

                    var vodenkoDto = new VodenkoDto
                    {
                        Actual_Level = vodenkoData.Actual_Level,
                        Valve_Position = vodenkoData.Valve_Position,
                        Time_Saved = vodenkoData.Time_Saved
                    };

                    var alarmsDto = new AlarmsDto
                    {
                        Setpoint_Invalid = alarmsData.Setpoint_Invalid,
                        Manual_Valve_Invalid = alarmsData.Manual_Valve_Invalid,
                        Tank_Overfill = alarmsData.Tank_Overfill,
                        Time_Saved = alarmsData.Time_Saved
                    };

                    Task.Run(async () =>
                    {
                        await repository.SaveTrendAsync(vodenkoDto);
                        await repository.SaveAlarmAsync(alarmsDto);
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("DB not readable: {Message}", ex.Message);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Read failed: {Message}", ex.Message);
                _plc!.Close();
            }
        }

        public void WriteBool(string variableName, bool state)
        {
            try
            {

                try
                {
                    bool test = false;
                    string variableNameLower = variableName.ToLower();
                    if (variableNameLower == "automatic_manual")
                    {
                        _plc.Write("DB102.DBD8", state);
                        test = (bool)_plc.Read("DB102.DBD8");
                    }
                    else if (variableNameLower == "start_pump")
                    {
                        bool result2 = (bool)_plc.Read("DB102.DBX8.1");
                        _plc.Write("DB102.DBX8.1", !result2);

                    }
                    else if (variableNameLower == "reset")
                    {
                        _plc.Write("DB102.DBX8.2", state);
                        test = (bool)_plc.Read("DB102.DBX8.2");
                    }
                }

                //_logger.LogInformation($"Write {variableName} to value {test}");


                catch (Exception ex)
                {
                    _logger.LogWarning("DB{Db} not readable: {Message}", 301, ex.Message);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Read failed: {Message}", ex.Message);
                _plc!.Close();
            }
        }

        public void WriteReal(string variableName, float value)
        {
            try
            {

                try
                {
                    float test = 0;
                    string variableNameLower = variableName.ToLower();
                    if (variableNameLower == "level_setpoint")
                    {
                        _plc.Write("DB102.DBD0", (float)value);
                        object table1 = _plc.Read("DB102.DBD0");

                    }
                    else if (variableNameLower == "manual_valve_value")
                    {
                        _plc.Write("DB102.DBD4", (float)value);

                        object table1 = _plc.Read("DB102.DBD4");

                    }

                    //_logger.LogInformation($"Write {variableName} to value {value}");

                }
                catch (Exception ex)
                {
                    _logger.LogWarning("DB{Db} not readable: {Message}", 301, ex.Message);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Read failed: {Message}", ex.Message);
                _plc!.Close();
            }

        }
        private async Task<Domain.Plc> FetchPlcConfigurationAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var repository = scope.ServiceProvider.GetRequiredService<IVodenkoRepository>();
                    Domain.Plc config = repository.GetPlc();

                    if (config != null && !string.IsNullOrEmpty(config.Ip))
                    {
                        _logger.LogInformation("PLC config loaded — IP: {Ip}, CPU: {Cpu}", config.Ip, config.Cpu);
                        return config;
                    }

                    _logger.LogWarning("Empty PLC config in DB, retrying in 3s...");
                }
                catch (Exception ex)
                {
                    _logger.LogError("Failed to fetch PLC config: {Message}. Retrying in 3s...", ex.Message);
                }

                await Task.Delay(3000, cancellationToken);
            }
            return null;
        }
    }
}
