namespace SmartX.Shared.Telemetry;

/// <summary>
/// Manages sequential historical batches of raw telemetry readings using a
/// jagged array (<c>T[][]</c>) before those batches are flattened into an
/// optimised <see cref="List{T}"/> of <see cref="TelemetryPacket{T}"/>
/// instances. A jagged array is used rather than a rectangular
/// <c>T[,]</c> because each ingestion batch from a sensor can legitimately
/// contain a different number of raw readings (e.g. a burst of 12 samples
/// followed by a burst of 3), which a rectangular array cannot represent
/// without wasting padding cells.
/// </summary>
/// <typeparam name="T">The raw value type of each reading, e.g. <c>double</c>.</typeparam>
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

    /// <summary>
    /// Appends one raw ingestion batch. When the buffer is full, the oldest
    /// batch is evicted (ring-buffer behaviour over the jagged array) so
    /// memory use stays bounded regardless of how long a sensor has been
    /// streaming for.
    /// </summary>
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

    /// <summary>
    /// Flattens every buffered batch (the jagged array) into a single,
    /// time-ordered <see cref="List{T}"/> of <see cref="TelemetryPacket{T}"/>,
    /// which is the collection type the rest of the ingestion pipeline and
    /// the dashboard actually consume.
    /// </summary>
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

    /// <summary>Read-only snapshot of the raw jagged array, mostly for diagnostics/tests.</summary>
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
