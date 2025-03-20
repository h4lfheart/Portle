using System;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Portle.Extensions;
using Portle.Services;

namespace Portle.Application;

public partial class App : Avalonia.Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        BindingPlugins.DataValidators.RemoveAll(validator => validator is DataAnnotationsValidationPlugin);
        
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            ApplicationService.Application = desktop;
            ApplicationService.Initialize();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void OnTrayIconOpen(object? sender, EventArgs e)
    {
        OpenAppWindow();
    }

    private void OnTrayIconQuit(object? sender, EventArgs eventArgs)
    {
        ApplicationService.Application.Shutdown();
    }
}