using System;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentAvalonia.UI.Controls;
using Portle.Application;
using Portle.Framework;
using Portle.Models.Information;
using Portle.Models.Repository;
using Portle.Services;
using Portle.Validators;
using Portle.Views;

namespace Portle.ViewModels;

public partial class SetupViewModel : ViewModelBase
{
    [ObservableProperty] 
    [DirectoryExists("Installation Path")]
    private string _installationPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Portle", "Installations");

    [ObservableProperty] 
    [DirectoryExists("Downloads Path")]
    private string _downloadsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Portle", "Downloads");
    
    [ObservableProperty] private bool _launchOnStartup = false;
    [ObservableProperty] private bool _minimizeToTray = false;
    [ObservableProperty] private bool _closeOnLaunch = false;

    [RelayCommand]
    public async Task Continue()
    {
        AppSettings.Application.InstallationPath = InstallationPath;
        AppSettings.Application.DownloadsPath = DownloadsPath;
        AppSettings.Application.Repositories = [Globals.DEFAULT_REPOSITORY];
        AppSettings.Application.LaunchOnStartup = LaunchOnStartup;
        AppSettings.Application.MinimizeToTray = MinimizeToTray;
        AppSettings.Application.CloseOnLaunch = CloseOnLaunch;
        AppSettings.Application.FinishedSetup = true;
        
        Navigation.App.Open<ProfilesView>();

        Info.Dialog("Import Installation", "Would you like to import any existing installations?", buttons:[ 
            new DialogButton
            {
                Text = "Import",
                Action = () => TaskService.Run(async () => await ProfilesVM.ImportInstallation())
            }
        ]);
    }
    
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
}