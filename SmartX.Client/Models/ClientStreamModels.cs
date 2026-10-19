namespace SmartX.Client.Models;

public class DeviceStateDto 
{
    public string DeviceId { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string LastMetric { get; set; } = string.Empty;
    public double LastValue { get; set; }
    public string Unit { get; set; } = string.Empty;
    public DateTime LastSeen { get; set; }
    public bool PumpOn { get; set; }
    public bool RelayOn { get; set; }
    public bool HasAlert { get; set; }
}

public class TelemetryEventDto
{
    public string DeviceId { get; set; } = string.Empty;
    public string Metric { get; set; } = string.Empty;
    public double Value { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool IsCritical { get; set; }
    public DateTime Timestamp { get; set; }
}

public class CommandRequestDto
{
    public string DeviceId { get; set; } = string.Empty;
    public string Command { get; set; } = string.Empty;
}

public class CommandRecordDto
{
    public string DeviceId { get; set; } = string.Empty;
    public string Command { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}

public class SuggestionDto
{
    public string Kind { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Detail { get; set; } = string.Empty;
    public string? DeviceId { get; set; }
}

public class StreamStatusDto
{
    public int StandardQueued { get; set; }
    public int CriticalQueued { get; set; }
    public int LogEntries { get; set; }
    public List<string> ActiveAlerts { get; set; } = new();
}

public class ErrorDto
{
    public string? Error { get; set; }
}
