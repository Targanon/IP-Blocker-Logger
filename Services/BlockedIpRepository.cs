using System.Collections.Concurrent;
using System.Text.Json;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Maui.Storage;
using System;

namespace IP_Blocker_Logger.Services;

public class BlockedIpRepository
{
    const string Key = "blocked_ips_v1";
    readonly ConcurrentDictionary<string, BlockedIpEntry> _cache = new();

    public BlockedIpRepository()
    {
        Load();
    }

    void Load()
    {
        if (Preferences.ContainsKey(Key))
        {
            var json = Preferences.Get(Key, string.Empty);
            if (!string.IsNullOrWhiteSpace(json))
            {
                try
                {
                    var items = JsonSerializer.Deserialize<List<BlockedIpEntry>>(json) ?? new List<BlockedIpEntry>();
                    foreach (var i in items)
                        _cache[i.IpAddress] = i;
                }
                catch { }
            }
        }
    }

    void Persist()
    {
        var list = _cache.Values.OrderBy(v => v.IpAddress).ToList();
        Preferences.Set(Key, JsonSerializer.Serialize(list));
    }

    public IEnumerable<BlockedIpEntry> GetAll() => _cache.Values.OrderBy(v => v.IpAddress);

    public bool Add(string ip)
    {
        ip = ip.Trim();
        if (!System.Net.IPAddress.TryParse(ip, out _)) return false;
        if (_cache.ContainsKey(ip)) return false;
        _cache[ip] = new BlockedIpEntry { IpAddress = ip, Enabled = true };
        Persist();
        return true;
    }

    public void Remove(string ip)
    {
        if (_cache.TryRemove(ip, out _))
            Persist();
    }

    public void Toggle(string ip, bool enabled)
    {
        if (_cache.TryGetValue(ip, out var entry))
        {
            entry.Enabled = enabled;
            Persist();
        }
    }

    public IReadOnlySet<string> EnabledSet() => _cache.Values.Where(v => v.Enabled).Select(v => v.IpAddress).ToHashSet(StringComparer.OrdinalIgnoreCase);
}

public class BlockedIpEntry
{
    public string IpAddress { get; set; } = string.Empty;
    public bool Enabled { get; set; }
}