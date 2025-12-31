using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using DK.WshRuntime;
using Portle.Framework;
using Portle.Models.Installation;
using Portle.Models.Repository;

namespace Portle.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    [ObservableProperty] private string _installationPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Portle", "Installations");
    [ObservableProperty] private string _downloadsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Portle", "Downloads");
    [ObservableProperty] private bool _launchOnStartup = false;
    [ObservableProperty] private bool _minimizeToTray = false;
    [ObservableProperty] private bool _closeOnLaunch = false;
    
    [ObservableProperty] private ObservableCollection<string> _repositories = [];
    [ObservableProperty] private ObservableCollection<InstallationVersion> _downloadedVersions = [];
    [ObservableProperty] private ObservableCollection<InstallationProfile> _profiles = [];
    
    [ObservableProperty] private bool _finishedSetup = false;
    
    public async Task BrowseInstallationPath()
    {
        if (await App.BrowseFolderDialog() is { } path)
        {
            InstallationPath = path;
        }
    }
    
    public async Task BrowseDownloadsPath()
    {
        if (await App.BrowseFolderDialog() is { } path)
        {
            DownloadsPath = path;
        }
    }

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        switch (e.PropertyName)
        {
            case nameof(LaunchOnStartup):
            {
                var appPath = Environment.ProcessPath;
                var startupFolder = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
                var shortcutPath = Path.Combine(startupFolder, "Portle.lnk");
                if (LaunchOnStartup)
                {
                    if (!File.Exists(shortcutPath))
                        WshInterop.CreateShortcut(shortcutPath, string.Empty, appPath, "--startup", string.Empty);
                }
                else
                {
                    File.Delete(shortcutPath);
                }
                
                break;
            }
        }
    }
}