using Microsoft.Maui.Controls;
using Microsoft.Maui.ApplicationModel;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IP_Blocker_Logger.Services;

namespace IP_Blocker_Logger.Pages;

public partial class FirewallPage : ContentPage
{
    readonly BlockedIpRepository _repo;
    readonly FirewallLogRepository _logRepo;
    readonly IFirewallController _controller;
    readonly CancellationTokenSource _refreshCts = new();

    public FirewallPage(BlockedIpRepository repo, FirewallLogRepository logRepo, IFirewallController controller)
    {
        InitializeComponent();
        _repo = repo;
        _logRepo = logRepo;
        _controller = controller;
        RefreshIps();
        LogList.ItemsSource = _logRepo.GetLatest().ToList();
        
        // Set initial button text
        StartStopButton.Text = "Start IP Blocking";
        
        _ = LoopRefreshAsync(_refreshCts.Token);
        _ = UpdateButtonStateAsync();
    }

    async Task UpdateButtonStateAsync()
    {
        try
        {
            var isRunning = await _controller.IsRunningAsync();
            MainThread.BeginInvokeOnMainThread(() =>
            {
                StartStopButton.Text = isRunning ? "Stop IP Blocking" : "Start IP Blocking";
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to update button state: {ex.Message}");
        }
    }

    void RefreshIps()
    {
        IpList.ItemsSource = _repo.GetAll().Select(x => new BlockedIpEntry { Address = x.Address, Enabled = x.Enabled }).ToList();
    }

    async Task LoopRefreshAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(4000, token);
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    LogList.ItemsSource = _logRepo.GetLatest().ToList();
                });
                
                // Update button state periodically
                _ = UpdateButtonStateAsync();
            }
            catch (TaskCanceledException) { }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in refresh loop: {ex.Message}");
            }
        }
    }

    async void OnToggleFirewall(object sender, EventArgs e)
    {
        try
        {
            // Disable button during operation
            StartStopButton.IsEnabled = false;
            
            if (!await _controller.IsRunningAsync())
            {
                // Check VPN permission first
                var hasPermission = await _controller.CheckVpnPermissionAsync();
                
                if (!hasPermission)
                {
                    await DisplayAlert("VPN Permission Required", 
                        "This app needs VPN permission to block IP addresses. Android will now ask for this permission.", 
                        "OK");
                }
                
                var started = await _controller.StartAsync();
                
                if (started)
                {
                    StartStopButton.Text = "Stop IP Blocking";
                    await DisplayAlert("Success", "IP blocking started successfully!", "OK");
                }
                else if (hasPermission)
                {
                    await DisplayAlert("Error", "Failed to start IP blocking service.", "OK");
                }
                else
                {
                    await DisplayAlert("Permission Needed", 
                        "Please grant VPN permission and try again. You may need to restart the app after granting permission.", 
                        "OK");
                }
            }
            else
            {
                await _controller.StopAsync();
                StartStopButton.Text = "Start IP Blocking";
                await DisplayAlert("Info", "IP blocking stopped.", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to toggle IP blocking: {ex.Message}", "OK");
            System.Diagnostics.Debug.WriteLine($"Toggle error: {ex}");
        }
        finally
        {
            // Re-enable button and update state
            StartStopButton.IsEnabled = true;
            _ = UpdateButtonStateAsync();
        }
    }

    async void OnAdd(object sender, EventArgs e)
    {
        try
        {
            var ip = IpEntry.Text?.Trim();
            if (string.IsNullOrWhiteSpace(ip))
            {
                await DisplayAlert("Invalid Input", "Please enter a valid IP address.", "OK");
                return;
            }

            if (_repo.Add(ip))
            {
                IpEntry.Text = string.Empty;
                RefreshIps();
                _ = _controller.RefreshRulesAsync();
                await DisplayAlert("Success", $"IP address {ip} added to block list.", "OK");
            }
            else
            {
                await DisplayAlert("Invalid IP", "Please enter a valid IP address or check if it's already added.", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to add IP: {ex.Message}", "OK");
        }
    }

    void OnDelete(object sender, EventArgs e)
    {
        try
        {
            if (sender is SwipeItem si && si.CommandParameter is string ip)
            {
                _repo.Remove(ip);
                RefreshIps();
                _ = _controller.RefreshRulesAsync();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Delete error: {ex.Message}");
        }
    }

    void OnToggleSwitch(object sender, ToggledEventArgs e)
    {
        try
        {
            if (sender is Switch s && s.BindingContext is BlockedIpEntry entry)
            {
                _repo.Toggle(entry.Address, e.Value);
                _ = _controller.RefreshRulesAsync();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Toggle switch error: {ex.Message}");
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _refreshCts.Cancel();
    }
}
