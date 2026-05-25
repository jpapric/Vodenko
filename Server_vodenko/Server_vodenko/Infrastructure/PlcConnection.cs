using S7.Net;
using Server_vodenko.Application.Interfaces;
using Server_vodenko.Domain;

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

                await Task.Delay(3000, stoppingToken);
            }
        }

        private void ReadFromPlc()
        {
            try
            {

                try
                {
                    //var    watch = System.Diagnostics.Stopwatch.StartNew();
                    EAF furnaceData = new EAF();
                    _plc.ReadClass(furnaceData, 301, 0);
                    _cache.Update(furnaceData);
                    using var scope = _scopeFactory.CreateScope();
                    var repository = scope.ServiceProvider.GetRequiredService<IVodenkoRepository>();
                    repository.Event_detection(furnaceData);
                    repository.PostEAF(furnaceData);


                    //watch.Stop();
                    //_logger.LogInformation($"Sending speed {watch.ElapsedMilliseconds}");

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

        public void WriteBool(string variableName, bool state)
        {
            try
            {

                try
                {
                    bool test = false;
                    string variableNameLower = variableName.ToLower();
                    if (variableNameLower == "load_scrap")
                    {
                        _plc.Write("DB302.DBX0.0", state);
                        test = (bool)_plc.Read("DB302.DBX0.0");
                    }
                    else if (variableNameLower == "tap")
                    {
                        bool result2 = (bool)_plc.Read("DB302.DBX0.1");
                        _plc.Write("DB302.DBX0.1", !result2);

                    }
                    else if (variableNameLower == "reset")
                    {
                        _plc.Write("DB302.DBD10", state);
                        test = (bool)_plc.Read("DB302.DBD10");
                    }
                    else if (variableNameLower == "electrodes")
                    {
                        bool result2 = (bool)_plc.Read("DB302.DBX0.2");
                        _plc.Write("DB302.DBX0.2", state);
                        bool result27 = (bool)_plc.Read("DB302.DBX0.2");
                        bool result3 = (bool)_plc.Read("DB301.DBX0.2");

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
                    if (variableNameLower == "current_setpoint")
                    {
                        _plc.Write("DB302.DBD2", (float)value);
                        object table1 = _plc.Read("DB302.DBD2");

                    }
                    else if (variableNameLower == "tap_angle")
                    {
                        _plc.Write("DB302.DBD6", (float)value);

                        object table1 = _plc.Read("DB302.DBD6.0");
                        var table2 = Convert.ToSingle(_plc.Read("DB301.DBD2"));


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
