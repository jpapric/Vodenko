using Server_vodenko.Domain;

public class PlcDataCache
{
    private Vodenko? _latestData;
    private readonly object _lock = new();

    public void Update(Vodenko data)
    {
        lock (_lock) _latestData = data;
    }

    public Vodenko? Get()
    {
        lock (_lock) return _latestData;
    }
}