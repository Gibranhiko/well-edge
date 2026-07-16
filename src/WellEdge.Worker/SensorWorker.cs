using Microsoft.Extensions.Options;
using WellEdge.Application;

namespace WellEdge.Worker;

public class SensorWorker : BackgroundService
{
    private readonly SensorSimulator _simulator;
    private readonly SimulatorOptions _options;
    private readonly ILogger<SensorWorker> _logger;

    public SensorWorker(SensorSimulator simulator, IOptions<SimulatorOptions> options, ILogger<SensorWorker> logger)
    {
        _simulator = simulator;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalMs = 1000 / _options.FrequencyHz;

        _logger.LogInformation("SensorWorker starting. Well: {WellId}, Frequency: {FrequencyHz} Hz",
            _options.WellId, _options.FrequencyHz);

        await foreach (var reading in _simulator.StreamAsync(_options.WellId, intervalMs, stoppingToken))
        {
            _logger.LogInformation(
                "[{Timestamp:HH:mm:ss.fff}] {WellId} | Pressure: {Pressure:F1} PSI | Flow: {Flow:F2} bbl/min | Temp: {Temp:F1} °C",
                reading.Timestamp, reading.WellId, reading.PressurePsi, reading.FlowRateBblPerMin, reading.TemperatureCelsius);
        }

        _logger.LogInformation("SensorWorker stopped.");
    }
}
