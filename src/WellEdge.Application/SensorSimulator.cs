using WellEdge.Domain;

namespace WellEdge.Application;

public class SensorSimulator
{
    private readonly Random _random = new();

    public async IAsyncEnumerable<SensorReading> StreamAsync(
        string wellId,
        int intervalMs,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            yield return new SensorReading(
                WellId: wellId,
                Timestamp: DateTime.UtcNow,
                PressurePsi: 2500 + _random.NextDouble() * 500,
                FlowRateBblPerMin: 3.0 + _random.NextDouble() * 1.5,
                TemperatureCelsius: 65 + _random.NextDouble() * 10
            );

            await Task.Delay(intervalMs, ct);
        }
    }
}
