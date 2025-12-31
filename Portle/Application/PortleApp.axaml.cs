using System;
using System.IO;
using System.Linq;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using FluentAvalonia.Styling;
using Portle.Extensions;
using Portle.Services;

namespace Portle.Application;

public partial class PortleApp : Avalonia.Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);
        BindingPlugins.DataValidators.RemoveAll(validator => validator is DataAnnotationsValidationPlugin);

        if (Styles.OfType<FluentAvaloniaTheme>().FirstOrDefault() is { } fluentTheme)
        {
            fluentTheme.CustomAccentColor = Color.Parse("#303030");
        }
        
        AppServices.Initialize();
        
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            App.InitializeDesktop(desktop);
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void OnTrayIconOpen(object? sender, EventArgs e)
    {
        App.OpenWindow();
    }

    private void OnTrayIconQuit(object? sender, EventArgs eventArgs)
    {
        App.Lifetime.Shutdown();
    }
}