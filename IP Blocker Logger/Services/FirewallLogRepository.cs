#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.Maui.Storage; // Add this using directive

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
                    foreach (var e in JsonSerializer.Deserialize<List<FirewallLogEntry>>(json) ?? new List<FirewallLogEntry>())
                        _entries.Enqueue(e);
                }
                catch { }
            }
        }
    }

    public IEnumerable<FirewallLogEntry> GetLatest(int count = 100) => _entries.ToArray().Reverse().Take(count);

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
    System.DateTime TimestampUtc,
    string Direction,
    string SourceIp,
    string DestIp,
    bool Blocked,
    string Protocol,
    int? SourcePort,
    int? DestPort,
    string? AppPackage);
#nullable restore
