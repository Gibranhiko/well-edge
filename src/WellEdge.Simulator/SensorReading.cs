namespace WellEdge.Simulator;

public record SensorReading(
    string WellId,
    DateTime Timestamp,
    double PressurePsi,
    double FlowRateBblPerMin,
    double TemperatureCelsius
);
