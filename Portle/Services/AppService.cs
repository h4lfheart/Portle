using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input.Platform;
using Avalonia.Platform.Storage;
using DynamicData;
using FluentAvalonia.UI.Controls;
using Microsoft.Win32;
using Portle.Application;
using Portle.Extensions;
using Portle.Framework;
using Portle.Models.Information;
using Portle.Models.Installation;
using Portle.ViewModels;
using Portle.Views;
using Portle.Windows;
using RestSharp;
using Serilog;

namespace Portle.Services;

public class AppService : IService
{
    public IClassicDesktopStyleApplicationLifetime Lifetime;
    public IStorageProvider StorageProvider => Lifetime.MainWindow!.StorageProvider;
    public IClipboard Clipboard => Lifetime.MainWindow!.Clipboard!;

    public DirectoryInfo ApplicationDataFolder = new(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Portle"));
    
    private const string SCHEME_NAME = "portle";
    
    public void InitializeDesktop(IClassicDesktopStyleApplicationLifetime desktop)
    {
        Lifetime = desktop;
        
        Initialize();
    }

    public void Initialize()
    {
        AppSettings.Load();
        
        Info.CreateLogger();

        Lifetime.Startup += OnAppStart;
        Lifetime.Exit += OnAppExit;
    }
    
    public void OpenWindow()
    {
        if (Lifetime.MainWindow is null)
        {
            Lifetime.MainWindow = new AppWindow();
            Lifetime.MainWindow.Loaded += (sender, args) =>
            {
                if (AppSettings.Application.FinishedSetup)
                {
                    Navigation.App.Open<ProfilesView>();
                }
                else
                {
                    Navigation.App.Open<SetupView>();
                }
            };
            
            Lifetime.MainWindow.Show();
            return;
        }
        
        Lifetime.MainWindow.WindowState = WindowState.Normal;
        Lifetime.MainWindow.Show();
        Lifetime.MainWindow.BringToTop();
    }
    
    private async void OnAppStart(object? sender, ControlledApplicationLifetimeStartupEventArgs e)
    {
        ExecuteArguments(Lifetime.Args ?? []);
    }

    private void OnAppExit(object? sender, ControlledApplicationLifetimeExitEventArgs e)
    {
        if (AppSettings.ShouldSaveOnExit)
            AppSettings.Save();
    }
    
    public async void ExecuteArguments(string[] args)
    {
        if (args.IndexOf("--skip-setup") is not -1 && !AppSettings.Application.FinishedSetup)
        {
            AppSettings.Application.FinishedSetup = true;
        }
        
        if (args.IndexOf("--startup") is -1 && args.IndexOf("--silent") is -1)
        {
            OpenWindow();
        }

        if (args.IndexOf("--add-repository") is var addRepoIndex and not -1)
        {
            var repositoryUrl = args[addRepoIndex + 1].Trim('"');
            await Repositories.AddRepository(repositoryUrl, verbose: false);
        }

        if (args.IndexOf("--import-profile") is var importProfileCommandIndex and not -1)
        {
            var profileName = args[importProfileCommandIndex + 1].Trim('"');
            var executablePath = args[importProfileCommandIndex + 2].Trim('"');
            var repositoryId = args[importProfileCommandIndex + 3].Trim('"');

            var existingProfile = ProfilesVM.ProfilesSource.Items.FirstOrDefault(profile => profile.Name.Equals(profileName));
            var targetRepository = Repositories.Repositories.Items.FirstOrDefault(repo => repo.Id.Equals(repositoryId));
            if (existingProfile is not null)
            {
                existingProfile.Directory = Path.GetDirectoryName(executablePath)!;
                existingProfile.ExecutableName = Path.GetFileName(executablePath);
            }
            else if (existingProfile is null && targetRepository is not null)
            {
                var targetDownloadVersion = targetRepository.Versions.MaxBy(version => version.UploadTime)!;
                var targetVersion = await targetDownloadVersion.DownloadInstallationVersion();

                var id = Guid.NewGuid();

                var profile = new InstallationProfile
                {
                    ProfileType = EProfileType.Repository,
                    Name = profileName,
                    Version = targetVersion.Version,
                    Directory = Path.GetDirectoryName(executablePath)!,
                    ExecutableName = Path.GetFileName(executablePath),
                    Id = id,
                    IconUrl = targetVersion.IconUrl,
                    RepositoryUrl = targetVersion.RepositoryUrl
                };

                Directory.CreateDirectory(Path.GetDirectoryName(profile.ExecutablePath)!);
                
                if (MiscExtensions.GetRunningProcess(profile.ExecutablePath) is { } runningProcess)
                {
                    runningProcess.Kill(entireProcessTree: true);
                    Log.Information($"Killed {profile.ExecutablePath}");

                    var tries = 0;
                    while (MiscExtensions.IsProcessRunning(profile.ExecutablePath) && tries < 20)
                    {
                        await Task.Delay(100);
                        tries++;
                    }

                    if (tries >= 20)
                    {
                        return;
                    }
                }
                
                File.Copy(targetVersion.ExecutablePath, profile.ExecutablePath, true);

                ProfilesVM.ProfilesSource.Add(profile);
                
                Log.Information($"Added new profile named \"{profileName}\" at {profile.ExecutablePath}");
            }
        }
        
        if (args.IndexOf("--update-profile") is var updateProfileIndex and not -1)
        {
            var profileName = args[updateProfileIndex + 1].Trim('"');
            var isForcedIndex = updateProfileIndex + 2;
            var isForced = isForcedIndex < args.Length && args[isForcedIndex].Trim('"').Equals("-force");
            if (ProfilesVM.ProfilesSource.Items.FirstOrDefault(profile => profile.Name.Equals(profileName)) is
                { } existingProfile)
            {
                if (isForced)
                    await existingProfile.UpdateForce();
                else
                    await existingProfile.Update(verbose: false);
            }
        }
        
        if (args.IndexOf("--launch-profile") is var launchProfileIndex and not -1)
        {
            var profileName = args[launchProfileIndex + 1].Trim('"');
            if (ProfilesVM.ProfilesSource.Items.FirstOrDefault(profile => profile.Name.Equals(profileName)) is
                { } existingProfile)
            {
                await existingProfile.Launch();
            }
        }
    }
    
    public void Launch(string location, bool shellExecute = true)
    {
        Process.Start(new ProcessStartInfo { FileName = location, UseShellExecute = shellExecute });
    }
    
    public void LaunchSelected(string location)
    {
        var argument = "/select, \"" + location +"\"";
        Process.Start("explorer", argument);
    }
    
    public async Task<string?> BrowseFolderDialog(string startLocation = "")
    {
        var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions { AllowMultiple = false, SuggestedStartLocation = await StorageProvider.TryGetFolderFromPathAsync(startLocation)});
        var folder = folders.ToArray().FirstOrDefault();

        return folder?.Path.AbsolutePath.Replace("%20", " ");
    }

    public async Task<string?> BrowseFileDialog(string suggestedFileName = "", params FilePickerFileType[] fileTypes)
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions { AllowMultiple = false, FileTypeFilter = fileTypes, SuggestedFileName = suggestedFileName});
        var file = files.ToArray().FirstOrDefault();

        return file?.Path.AbsolutePath.Replace("%20", " ");
    }

    public async Task<string?> SaveFileDialog(string suggestedFileName = "", params FilePickerFileType[] fileTypes)
    {
        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions {FileTypeChoices = fileTypes, SuggestedFileName = suggestedFileName});
        return file?.Path.AbsolutePath.Replace("%20", " ");
    }

    public void RestartWithMessage(string title, string content, Action? onRestart = null, bool mandatory = false)
    {
        Info.Dialog(title, content, canClose: !mandatory, buttons:
        [
            new DialogButton
            {
                Text = "Restart",
                Action = () =>
                {
                    onRestart?.Invoke();
                    Restart();
                }
            }
        ]);
    }
    
    public void Restart()
    {
        Launch(AppDomain.CurrentDomain.FriendlyName, false);
        Shutdown();
    }

    public void Shutdown()
    {
        Lifetime.Shutdown();
    }
}