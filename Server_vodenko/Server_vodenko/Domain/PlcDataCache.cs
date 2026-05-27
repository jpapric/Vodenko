using Server_vodenko.Domain;

public class PlcDataCache
{
    private Vodenko? _latestData;
    private Alarms? _alarmsLatestData;
    private readonly object _lock = new();

    public void Update(Vodenko data)
    {
        lock (_lock)
        {
            _latestData = data;
        }
    }

    public Vodenko? Get()
    {
        lock (_lock)
        {
            return _latestData;
        }
    }

    public void UpdateAlarms(Alarms alarmsData)
    {
        lock (_lock)
        {
            _alarmsLatestData = alarmsData;
        }
    }

    public Alarms? GetAlarms()
    {
        lock (_lock)
        {
            return _alarmsLatestData;
        }
    }
}