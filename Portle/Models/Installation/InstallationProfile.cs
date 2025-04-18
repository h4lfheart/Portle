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

    public bool IsImported => !Directory.Contains(AppSettings.Current.InstallationPath);

    [JsonIgnore] public string ExecutablePath => Path.Combine(Directory, ExecutableName);
    [JsonIgnore] public string DescriptionString => $"{Version} - {(IsImported ? "Imported" : "Managed")} - {Id}}}";
    [JsonIgnore] public Task<Bitmap?> IconImage => ImageLoader.AsyncImageLoader.ProvideImageAsync(IconUrl ?? string.Empty);

    public async Task Launch()
    {
        if (!File.Exists(ExecutablePath))
        {
            var dialog = new ContentDialog
            {
                Title = $"Missing Executable for \"{Name}\"",
                Content = "The executable that this profile is linked to no longer exists. Please decide whether to delete the profile, link a new executable, or cancel.",
                CloseButtonText = "Cancel",
                PrimaryButtonText = "Link",
                PrimaryButtonCommand = new RelayCommand(async () =>
                {
                    if (await BrowseFileDialog() is { } newPath)
                    {
                        ExecutableName = Path.GetFileName(newPath);
                        Directory = Path.GetDirectoryName(newPath)!;
                    }
                }),
                SecondaryButtonText = "Delete",
                SecondaryButtonCommand = new RelayCommand(async () =>
                {
                    await ProfilesVM.Delete(this);
                })
            };

            await dialog.ShowAsync();
            
            return;
        }
        
        AppWM.Message("Launch", $"Launching {Name}");
        Log.Information($"Launched profile \"{Name}\" at {ExecutablePath}");
        
        Process.Start(new ProcessStartInfo
        {
            FileName = ExecutablePath,
            UseShellExecute = true
        });
        
        if (AppSettings.Current.CloseOnLaunch)
            AppWM.HideCommand.Invoke();
    }
    
    public async Task Rename()
    {
        var textBox = new TextBox
        {
            Watermark = "New Profile Name"
        };
        
        var dialog = new ContentDialog
        {
            Title = $"Rename \"{Name}\"",
            Content = textBox,
            CloseButtonText = "Cancel",
            PrimaryButtonText = "Rename",
            PrimaryButtonCommand = new RelayCommand(() =>
            {
                if (string.IsNullOrWhiteSpace(textBox.Text)) return;
                
                Name = textBox.Text;
            })
        };

        await dialog.ShowAsync();
    }
    
    public async Task OpenFolder()
    {
        LaunchSelected(ExecutablePath);
    }
    
    public async Task ChangeVersionPrompt()
    {
        var comboBox = new ComboBox
        {
            ItemsSource = AppSettings.Current.DownloadedVersions,
            SelectedIndex = 0,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        
        var dialog = new ContentDialog
        {
            Title = $"Change Version of \"{Name}\"",
            Content = comboBox,
            CloseButtonText = "Cancel",
            PrimaryButtonText = "Change",
            PrimaryButtonCommand = new RelayCommand(() =>
            {
                if (comboBox.SelectedItem is not InstallationVersion newVersion) return;

                ChangeVersion(newVersion);
            })
        };

        await dialog.ShowAsync();
    }
    
    public async Task Update()
    {
        await Update(verbose: true);
    }

    public async Task Update(bool verbose)
    {
        if (ProfileType != EProfileType.Repository) return;
        
        var targetRepository = RepositoriesVM.Repositories.FirstOrDefault(repo => repo.RepositoryUrl.Equals(RepositoryUrl));
            
        var newestVersion = targetRepository?.Versions.MaxBy(version => version.Version);
        if (newestVersion is null) return;

        if (newestVersion.Version <= Version)
        {
            if (verbose)
                AppWM.Message("Update", $"{Name} is up to date");
            
            Log.Information($"Profile \"{Name}\" is up to date");
            return;
        }

        var oldVersion = Version;
        ChangeVersion(await newestVersion.DownloadInstallationVersion(), verbose: false);
            
        if (verbose)
            AppWM.Message("Update", $"{Name} was updated from \"{oldVersion}\" to \"{Version}\"");
        
        Log.Information($"{Name} was updated from \"{oldVersion}\" to \"{Version}\"");
    }
    
    public async Task UpdateForce()
    {
        if (ProfileType != EProfileType.Repository) return;
        
        var targetRepository = RepositoriesVM.Repositories.FirstOrDefault(repo => repo.RepositoryUrl.Equals(RepositoryUrl));
            
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
            AppWM.Message("Update", $"{Name} was changed to \"{Version}\"");
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
    [Description("From Repository")]
    Repository,
    
    [Description("Custom Version")]
    Custom
}