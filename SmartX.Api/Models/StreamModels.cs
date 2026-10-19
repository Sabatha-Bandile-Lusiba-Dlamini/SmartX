namespace SmartX.Api.Models;

//Body a device (or the simulator) POSTs to the API
public class IncomingTelemetryDto
{
    public string DeviceId { get; set; } = string.Empty;   // MAC address or node id
    public string Category { get; set; } = "Climate";
    public string Metric { get; set; } = "value";
    public double Value { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string Status { get; set; } = "Online";          // Online | Disconnected | Error
    public bool IsCritical { get; set; }
}

//One telemetry event as it moves through queue -> registry -> log
public class TelemetryEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string DeviceId { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Metric { get; set; } = string.Empty;
    public double Value { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string Status { get; set; } = "Online";
    public bool IsCritical { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

//Latest known state of a device (value stored in the Dictionary)
public class DeviceState
{
    public string DeviceId { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Status { get; set; } = "Online";
    public string LastMetric { get; set; } = string.Empty;
    public double LastValue { get; set; }
    public string Unit { get; set; } = string.Empty;
    public DateTime LastSeen { get; set; } = DateTime.UtcNow;
    public bool PumpOn { get; set; }
    public bool RelayOn { get; set; }
    public bool HasAlert { get; set; }

    public DeviceState Clone() => (DeviceState)MemberwiseClone();
}

public class CommandRequest
{
    public string DeviceId { get; set; } = string.Empty;
    public string Command { get; set; } = string.Empty;     // PumpOn | PumpOff | RelayOn | RelayOff
}

//An entry on the override-command Stack (stores previous state so Undo can restore it)
public class CommandRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string DeviceId { get; set; } = string.Empty;
    public string Command { get; set; } = string.Empty;
    public bool PreviousPumpOn { get; set; }
    public bool PreviousRelayOn { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class Suggestion
{
    public string Kind { get; set; } = string.Empty;        // Action | Insight | Problem
    public string Title { get; set; } = string.Empty;
    public string Detail { get; set; } = string.Empty;
    public string? DeviceId { get; set; }
}