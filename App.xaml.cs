using Microsoft.Maui;
using Microsoft.Maui.Controls;

namespace IP_Blocker_Logger;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState) => new(new AppShell());
}