namespace SmartX.Shared.Telemetry;


public sealed class TelemetryHistoryBuffer<T> where T : struct
{
    private readonly string _sensorId;
    private readonly int _maxBatches;

    // Jagged array: _batches[i] is the i-th ingestion batch, and each batch
    // can be a different length.
    private T[][] _batches;
    private int _batchCount;

    public TelemetryHistoryBuffer(string sensorId, int maxBatches = 20)
    {
        if (maxBatches <= 0) throw new ArgumentOutOfRangeException(nameof(maxBatches));
        _sensorId = sensorId;
        _maxBatches = maxBatches;
        _batches = new T[maxBatches][];
    }

    public int BatchCount => _batchCount;

    public void AddBatch(T[] rawReadings)
    {
        ArgumentNullException.ThrowIfNull(rawReadings);

        if (_batchCount == _maxBatches)
        {
            Array.Copy(_batches, 1, _batches, 0, _maxBatches - 1);
            _batches[_maxBatches - 1] = rawReadings;
        }
        else
        {
            _batches[_batchCount] = rawReadings;
            _batchCount++;
        }
    }


    public List<TelemetryPacket<T>> FlushToList(string unit = "")
    {
        var result = new List<TelemetryPacket<T>>();
        for (int batchIndex = 0; batchIndex < _batchCount; batchIndex++)
        {
            T[] batch = _batches[batchIndex];
            for (int i = 0; i < batch.Length; i++)
            {
                result.Add(new TelemetryPacket<T>(_sensorId, batch[i], unit));
            }
        }
        return result;
    }

    public T[][] SnapshotBatches()
    {
        var copy = new T[_batchCount][];
        for (int i = 0; i < _batchCount; i++)
        {
            copy[i] = (T[])_batches[i].Clone();
        }
        return copy;
    }
}
