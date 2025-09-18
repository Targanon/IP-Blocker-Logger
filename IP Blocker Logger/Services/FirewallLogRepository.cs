using System.Collections.Concurrent;
using System.Text.Json;

namespace IP_Blocker_Logger.Services;

public class FirewallLogRepository
{
    const string Key = "firewall_logs_v1";
    readonly ConcurrentQueue<FirewallLogEntry> _entries = new();
    const int MaxEntries = 500;

    public FirewallLogRepository()
    {
        if (Preferences.ContainsKey(Key))
        {
            var json = Preferences.Get(Key, string.Empty);
            if (!string.IsNullOrWhiteSpace(json))
            {
                try
                {
                    var list = JsonSerializer.Deserialize<List<FirewallLogEntry>>(json) ?? new();
                    foreach (var e in list)
                        _entries.Enqueue(e);
                }
                catch { }
            }
        }
    }

    public IEnumerable<FirewallLogEntry> GetLatest(int count = 100) => _entries.Reverse().Take(count);

    public void Add(FirewallLogEntry entry)
    {
        _entries.Enqueue(entry);
        while (_entries.Count > MaxEntries && _entries.TryDequeue(out _)) { }
        Persist();
    }

    void Persist()
    {
        Preferences.Set(Key, JsonSerializer.Serialize(_entries));
    }
}

public record FirewallLogEntry(
    DateTime TimestampUtc,
    string Direction,
    string SourceIp,
    string DestIp,
    bool Blocked,
    string Protocol,
    int? SourcePort,
    int? DestPort,
    string? AppPackage);
