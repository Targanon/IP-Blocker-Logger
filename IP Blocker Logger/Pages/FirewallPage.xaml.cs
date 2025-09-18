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
        _ = LoopRefreshAsync(_refreshCts.Token);
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
            }
            catch (TaskCanceledException) { }
        }
    }

    async void OnToggleFirewall(object sender, EventArgs e)
    {
        if (!await _controller.IsRunningAsync())
        {
            var started = await _controller.StartAsync();
            if (started) StartStopButton.Text = "Stop Firewall";
        }
        else
        {
            await _controller.StopAsync();
            StartStopButton.Text = "Start Firewall";
        }
    }

    void OnAdd(object sender, EventArgs e)
    {
        var ip = IpEntry.Text?.Trim();
        if (string.IsNullOrWhiteSpace(ip)) return;
        if (_repo.Add(ip))
        {
            IpEntry.Text = string.Empty;
            RefreshIps();
            _ = _controller.RefreshRulesAsync();
        }
    }

    void OnDelete(object sender, EventArgs e)
    {
        if (sender is SwipeItem si && si.CommandParameter is string ip)
        {
            _repo.Remove(ip);
            RefreshIps();
            _ = _controller.RefreshRulesAsync();
        }
    }

    void OnToggleSwitch(object sender, ToggledEventArgs e)
    {
        if (sender is Switch s && s.BindingContext is BlockedIpEntry entry)
        {
            _repo.Toggle(entry.Address, e.Value);
            _ = _controller.RefreshRulesAsync();
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _refreshCts.Cancel();
    }
}
