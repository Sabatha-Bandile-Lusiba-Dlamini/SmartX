using SmartX.Api.Models;

namespace SmartX.Api.Services;

// Manual override commands. A Stack (LIFO) gives Undo for free: pop the newest command and restore its saved state.
// A separate audit list keeps the full chronological history for the predictive engine.

public class CommandHistoryService
{
    private static readonly HashSet<string> Valid = new(StringComparer.OrdinalIgnoreCase)
        { "PumpOn", "PumpOff", "RelayOn", "RelayOff" };

    private readonly Stack<CommandRecord> _undoStack = new();
    private readonly List<CommandRecord> _audit = new();
    private readonly DeviceRegistry _registry;
    private readonly object _lock = new();

    public CommandHistoryService(DeviceRegistry registry) => _registry = registry;

    public (CommandRecord? Record, string? Error) Issue(string deviceId, string command)
    {
        if (!Valid.Contains(command)) return (null, $"Unknown command '{command}'.");

        lock (_lock)
        {
            var device = _registry.Get(deviceId);
            if (device == null) return (null, $"Device '{deviceId}' not found.");

            var record = new CommandRecord
            {
                DeviceId = device.DeviceId,
                Command = Valid.First(v => v.Equals(command, StringComparison.OrdinalIgnoreCase)),
                PreviousPumpOn = device.PumpOn,
                PreviousRelayOn = device.RelayOn
            };

            bool pump = device.PumpOn, relay = device.RelayOn;
            switch (record.Command)
            {
                case "PumpOn": pump = true; break;
                case "PumpOff": pump = false; break;
                case "RelayOn": relay = true; break;
                case "RelayOff": relay = false; break;
            }

            _registry.SetActuators(device.DeviceId, pump, relay);
            _undoStack.Push(record);
            _audit.Add(record);
            return (record, null);
        }
    }

    public (CommandRecord? Undone, string? Error) Undo()
    {
        lock (_lock)
        {
            if (!_undoStack.TryPop(out var record)) return (null, "Nothing to undo.");
            _registry.SetActuators(record.DeviceId, record.PreviousPumpOn, record.PreviousRelayOn);
            return (record, null);
        }
    }

    //Newest first (Stack enumerates in pop order)
    public List<CommandRecord> UndoableHistory(int take = 20)
    {
        lock (_lock) return _undoStack.Take(take).ToList();
    }

    public List<CommandRecord> Audit()
    {
        lock (_lock) return _audit.ToList();
    }
}