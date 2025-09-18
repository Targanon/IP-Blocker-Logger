using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Maui.Storage;
using IP_Blocker_Logger.Services;
using IPBlockerLogger;

namespace IPBlockerLogger;

public static class SharedDataService
{
    private static readonly string _dataPath = Path.Combine(FileSystem.AppDataDirectory, "shared_data.json");
    
    public static HashSet<string> GetBlockedIps()
    {
        try
        {
            // Try to use repository from DI first, fallback to direct storage access
            var repo = ServiceLocator.GetService<BlockedIpRepository>();
            if (repo != null)
            {
                return new HashSet<string>(repo.GetAll().Where(e => e.Enabled).Select(e => e.IpAddress), StringComparer.OrdinalIgnoreCase);
            }

            // Fallback: Load from file/preferences directly
            var json = Preferences.Get("blocked_ips_v1", "[]");
            var entries = JsonSerializer.Deserialize<BlockedIpEntry[]>(json) ?? [];
            return new HashSet<string>(entries.Where(e => e.Enabled).Select(e => e.IpAddress), StringComparer.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"SharedDataService: Error loading blocked IPs: {ex}");
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }
    }
    
    public static void LogFirewallEntry(FirewallLogEntry entry)
    {
        try
        {
            // Try to use repository from DI first, fallback to direct storage access
            var logRepo = ServiceLocator.GetService<FirewallLogRepository>();
            if (logRepo != null)
            {
                logRepo.Add(entry);
                return;
            }

            // Fallback: Save directly to preferences
            var existing = GetFirewallLogs();
            existing.Enqueue(entry);
            
            if (existing.Count > 500)
                existing.Dequeue();
                
            var json = JsonSerializer.Serialize(existing.ToArray());
            Preferences.Set("firewall_logs_v1", json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"SharedDataService: Error logging firewall entry: {ex}");
        }
    }
    
    public static Queue<FirewallLogEntry> GetFirewallLogs()
    {
        try
        {
            // Try to use repository from DI first, fallback to direct storage access  
            var logRepo = ServiceLocator.GetService<FirewallLogRepository>();
            if (logRepo != null)
            {
                return new Queue<FirewallLogEntry>(logRepo.GetLatest(500));
            }

            // Fallback: Load from preferences directly
            var json = Preferences.Get("firewall_logs_v1", "[]");
            var entries = JsonSerializer.Deserialize<FirewallLogEntry[]>(json) ?? [];
            return new Queue<FirewallLogEntry>(entries);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"SharedDataService: Error loading firewall logs: {ex}");
            return new Queue<FirewallLogEntry>();
        }
    }
    
    public static void SaveBlockedIps(IEnumerable<BlockedIpEntry> entries)
    {
        try
        {
            var json = JsonSerializer.Serialize(entries.ToArray());
            Preferences.Set("blocked_ips_v1", json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"SharedDataService: Error saving blocked IPs: {ex}");
        }
    }
}