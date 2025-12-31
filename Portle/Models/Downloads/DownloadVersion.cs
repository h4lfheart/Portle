using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DynamicData;
using FluentAvalonia.UI.Controls;
using Newtonsoft.Json;
using Portle.Application;
using Portle.Extensions;
using Portle.Models.Information;
using Portle.Models.Installation;
using Portle.Services;

namespace Portle.Models.Downloads;

public partial class DownloadVersion : ObservableObject
{
    [ObservableProperty] private DownloadRepository _parentRepository;
    
    [ObservableProperty, NotifyPropertyChangedFor(nameof(DisplayString))] private FPVersion _version;
    [ObservableProperty] private string _executableUrl;
    [ObservableProperty] private DateTime _uploadTime;
    [ObservableProperty] private bool _isCurrentlyDownloading;

    [ObservableProperty, JsonIgnore] private float _downloadProgressFraction;

    public string DisplayString => $"{ParentRepository.Title} {Version}";

    public bool IsDownloaded => File.Exists(ExecutableDownloadPath) && !IsCurrentlyDownloading;

    public string ExecutableDownloadPath => Path.Combine(AppSettings.Application.DownloadsPath, ParentRepository.Title, Version.ToString(), ExecutableUrl.SubstringAfterLast("/"));

    public InstallationVersion CreateInstallationVersion()
    {
        return new InstallationVersion
        {
            RepositoryName = ParentRepository.Title,
            Version = Version,
            ExecutablePath = ExecutableDownloadPath,
            RepositoryUrl = ParentRepository.RepositoryUrl,
            IconUrl = ParentRepository.IconUrl
        };
    }

    
    [RelayCommand]
    public async Task Download()
    {
        await DownloadInstallationVersion();
    }
    
    public async Task<InstallationVersion> DownloadInstallationVersion()
    {
        if (IsDownloaded)
        {
            return CreateInstallationVersion();
        }
        
        IsCurrentlyDownloading = true;
        var downloadedFile = await Api.DownloadFileAsync(ExecutableUrl, ExecutableDownloadPath, progress => DownloadProgressFraction = progress);
        IsCurrentlyDownloading = false;
        DownloadProgressFraction = 0;
        
        if (!downloadedFile.Exists)
        {
            Info.Message("Downloads", $"Failed to download {ExecutableUrl}", InfoBarSeverity.Error);
            return null;
        }
        
        OnPropertyChanged(nameof(IsDownloaded));

        var installationVersion = CreateInstallationVersion();

        AppSettings.Application.DownloadedVersions.Add(installationVersion);

        return installationVersion;
    }
    
    [RelayCommand]
    public async Task Delete()
    {
        var profilesUsingVersion = ProfilesVM.ProfilesSource.Items
            .Where(profile => profile.Version.Equals(Version))
            .ToArray();

        var dialogText = profilesUsingVersion.Length > 0 
            ? $"Are you sure you would like to delete {DisplayString}? There {(profilesUsingVersion.Length == 1 ? $"is {profilesUsingVersion.Length} profile that relies" : $"are {profilesUsingVersion.Length} profiles that rely")} on this version."
            : $"Are you sure you would like to delete {DisplayString}?";
        
        Info.Dialog($"Delete \"{DisplayString}\"", dialogText, buttons: [
            new DialogButton
            {
                Text = "Delete",
                Action = () => TaskService.Run(async () =>
                {
                    // remove associated profiles
                    foreach (var profile in profilesUsingVersion)
                    {
                        await profile.DeleteAndCleanup();
                        ProfilesVM.ProfilesSource.Remove(profile);
                    }
                    
                    File.Delete(ExecutableDownloadPath);
                    Directory.Delete(Path.Combine(AppSettings.Application.DownloadsPath, ParentRepository.Title, Version.ToString()));

                    AppSettings.Application.DownloadedVersions.RemoveAll(version => version.ExecutablePath == ExecutableDownloadPath);
        
                    OnPropertyChanged(nameof(IsDownloaded));
                })
            }
        ]);
        
        
    }
}