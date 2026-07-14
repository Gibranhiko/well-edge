using WellEdge.Simulator;

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

Console.WriteLine("WellEdge Simulator starting. Press Ctrl+C to stop.");

var simulator = new SensorSimulator();

try
{
    await foreach (var reading in simulator.StreamAsync("W-001", cts.Token))
    {
        Console.WriteLine(
            $"[{reading.Timestamp:HH:mm:ss.fff}] {reading.WellId} | " +
            $"Pressure: {reading.PressurePsi:F1} PSI | " +
            $"Flow: {reading.FlowRateBblPerMin:F2} bbl/min | " +
            $"Temp: {reading.TemperatureCelsius:F1} °C");
    }
}
catch (OperationCanceledException)
{
    Console.WriteLine("Simulator stopped.");
}
