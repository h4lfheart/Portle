using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AsyncImageLoader;
using Avalonia.Controls;
using Avalonia.Layout;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentAvalonia.UI.Controls;
using Microsoft.VisualBasic.FileIO;
using Newtonsoft.Json;
using Portle.Application;
using Portle.Extensions;
using Portle.Models.Information;
using Portle.Services;
using Serilog;
using Bitmap = Avalonia.Media.Imaging.Bitmap;

namespace Portle.Models.Installation;

public partial class InstallationProfile : ObservableObject
{
    [ObservableProperty] private Guid _id;
    [ObservableProperty] private string _name;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(DescriptionString))] private FPVersion _version;
    [ObservableProperty] private string _directory;
    [ObservableProperty] private string _executableName;
    [ObservableProperty] private EProfileType _profileType;
    
    [ObservableProperty] private string? _iconUrl;
    [ObservableProperty] private string? _repositoryUrl;

    public bool IsImported => !Directory.Contains(AppSettings.Application.InstallationPath);

    [JsonIgnore] public string ExecutablePath => Path.Combine(Directory, ExecutableName);
    [JsonIgnore] public string DescriptionString => $"{Version} - {(IsImported ? "External" : "Portle")} - {Id}";

    public async Task Launch()
    {
        if (!File.Exists(ExecutablePath))
        {
            Info.Dialog($"Missing Executable for \"{Name}\"", "The executable that this profile is linked to no longer exists. Please decide whether to delete the profile, link a new executable, or cancel.", buttons: [
                new DialogButton
                {
                    Text = "Link",
                    Action = () => TaskService.Run(async () =>
                    {
                        if (await AppServices.App.BrowseFileDialog() is not { } newPath) return;
                    
                        ExecutableName = Path.GetFileName(newPath);
                        Directory = Path.GetDirectoryName(newPath)!;
                    })
                },
                new DialogButton
                {
                    Text = "Delete",
                    Action = () => TaskService.Run(async () =>
                    {
                        await ProfilesVM.Delete(this);
                    })
                }
            ]);
            
            return;
        }
        
        Info.Message(Name, $"Launching Profile for {Version}");
        Log.Information($"Launched profile \"{Name}\" at {ExecutablePath}");
        
        Process.Start(new ProcessStartInfo
        {
            FileName = ExecutablePath,
            UseShellExecute = true
        });
        
        if (AppSettings.Application.CloseOnLaunch)
            AppWM.HideCommand.Invoke();
    }
    
    public async Task Rename()
    {
        var textBox = new TextBox
        {
            Watermark = "New Profile Name"
        };
        
        Info.Dialog($"Rename \"{Name}\"", content: textBox, buttons: [
            new DialogButton
            {
                Text = "Rename",
                Action = () =>
                {
                    var newName = string.Empty;
                    TaskService.RunDispatcher(() => newName = textBox.Text);
                    
                    if (string.IsNullOrWhiteSpace(newName))
                        return;

                    Name = newName;
                }
            }
        ]);
    }
    
    public async Task OpenFolder()
    {
        AppServices.App.LaunchSelected(ExecutablePath);
    }
    
    public async Task ChangeVersionPrompt()
    {
        var comboBox = new ComboBox
        {
            ItemsSource = AppSettings.Application.DownloadedVersions,
            SelectedIndex = 0,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        
        Info.Dialog($"Change Version of \"{Name}\"", content: comboBox, buttons: [
            new DialogButton
            {
                Text = "Change",
                Action = () =>
                {
                    InstallationVersion? newVersion = null;
                    TaskService.RunDispatcher(() => newVersion = comboBox.SelectedItem as InstallationVersion);
                    if (newVersion is null) return;

                    ChangeVersion(newVersion);
                }
            }
        ]);
    }
    
    public async Task Update()
    {
        await Update(verbose: true);
    }

    public async Task Update(bool verbose)
    {
        if (ProfileType != EProfileType.Repository) return;

        await Repositories.Refresh();
        
        var targetRepository = Repositories.Repositories.Items.FirstOrDefault(repo => repo.RepositoryUrl.Equals(RepositoryUrl));
            
        var newestVersion = targetRepository?.Versions.MaxBy(version => version.Version);
        if (newestVersion is null) return;

        if (newestVersion.Version <= Version)
        {
            if (verbose)
                Info.Message("Update", $"{Name} is up to date");
            
            Log.Information($"Profile \"{Name}\" is up to date");
            return;
        }

        var oldVersion = Version;
        ChangeVersion(await newestVersion.DownloadInstallationVersion(), verbose: false);
            
        if (verbose)
            Info.Message("Update", $"{Name} was updated from \"{oldVersion}\" to \"{Version}\"");
        
        Log.Information($"{Name} was updated from \"{oldVersion}\" to \"{Version}\"");
    }
    
    public async Task UpdateForce()
    {
        if (ProfileType != EProfileType.Repository) return;
        
        var targetRepository = Repositories.Repositories.Items.FirstOrDefault(repo => repo.RepositoryUrl.Equals(RepositoryUrl));
            
        var newestVersion = targetRepository?.Versions.MaxBy(version => version.Version);
        if (newestVersion is null) return;

        ChangeVersion(await newestVersion.DownloadInstallationVersion(), verbose: false);
        
        Log.Information($"Force updated \"{Name}\" to {newestVersion.Version}");
    }

    public void ChangeVersion(InstallationVersion newVersion, bool verbose = true)
    {
        if (MiscExtensions.GetRunningProcess(ExecutablePath) is { } runningProcess)
        {
            runningProcess.Kill(entireProcessTree: true);
            Log.Information($"Killed {ExecutablePath}");
        }
        
        File.Delete(ExecutablePath);
        File.Copy(newVersion.ExecutablePath, ExecutablePath);
        ExecutableName = Path.GetFileName((string?)newVersion.ExecutablePath);
        
        Version = newVersion.Version;
        
        if (verbose)
            Info.Message("Update", $"{Name} was changed to \"{Version}\"");
    }

    public async Task DeleteAndCleanup()
    {
        if (!System.IO.Directory.Exists(Directory)) return;
        if (!Directory.Contains(Id.ToString())) return; // double check this is managed by portle
        if (IsImported) return;
        
        FileSystem.DeleteDirectory(Directory, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
    }
    
}

public enum EProfileType
{
    [Description("Repository Latest Version")]
    Repository,
    
    [Description("Downloaded Version")]
    Custom
}