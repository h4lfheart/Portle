using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DynamicData;
using FluentAvalonia.UI.Controls;
using Portle.Application;
using Portle.Extensions;
using Portle.Framework;
using Portle.Models.Downloads;
using Portle.Models.Information;
using Portle.Models.Installation;
using Portle.Services;
using ReactiveUI;

namespace Portle.ViewModels;

public partial class ProfilesViewModel : ViewModelBase
{
    [ObservableProperty] private RepositoryService _repositoryService;

    public ProfilesViewModel(RepositoryService repositoryService) : this()
    {
        RepositoryService = repositoryService;
    } 
    
    [ObservableProperty] private string _searchFilter = string.Empty;
    
    [ObservableProperty] private ReadOnlyObservableCollection<InstallationProfile> _profiles = new([]);
    
    [JsonIgnore] public bool CanCreateProfile => AppSettings.Application.DownloadedVersions.Count > 0 || AppSettings.Application.Repositories.Count > 0;
    
    public readonly SourceList<InstallationProfile> ProfilesSource = new();

    public ProfilesViewModel()
    {
        var filter = this
            .WhenAnyValue(viewModel => viewModel.SearchFilter)
            .Select(searchFilter =>
            {
                return new Func<InstallationProfile, bool>(profile => 
                    MiscExtensions.Filter(profile.Name, searchFilter) || MiscExtensions.Filter(profile.Version.ToString(), searchFilter));
            });
        
        ProfilesSource.Connect()
            .Filter(filter)
            .Bind(out var collection)
            .Subscribe();
        
        Profiles = collection;

        foreach (var profile in AppSettings.Application.Profiles.ToArray())
        {
            if (!File.Exists(profile.ExecutablePath))
            {
                AppSettings.Application.Profiles.Remove(profile);
            } 
        }
        
        ProfilesSource.AddRange(AppSettings.Application.Profiles);
    }

    public override async Task Initialize()
    {
        
        await UpdateRepositoryProfiles();
    }

    public async Task UpdateRepositoryProfiles()
    {
        var repositoryProfiles = Profiles
            .Where(profile => profile.ProfileType == EProfileType.Repository)
            .ToArray();

        foreach (var profile in repositoryProfiles)
        {
            await profile.Update(verbose: false);
        }
    }
    

    public override async Task OnViewOpened()
    {
        OnPropertyChanged(nameof(CanCreateProfile));
    }

    public async Task CreateProfile()
    {
        var content = new CreateProfileDialog();
        
        Info.Dialog("Create New Profile", content: content, buttons: [
            new DialogButton
            {
                Text = "Create",
                Action = () => TaskService.Run(async () =>
                {
                    CreateProfileDialogContext? dialogContext = null;
                    await TaskService.RunDispatcherAsync(() => dialogContext = content.DataContext as CreateProfileDialogContext);
                    if (dialogContext is null) return;
                
                    InstallationVersion targetVersion;
                    if (dialogContext.ProfileType == EProfileType.Repository)
                    {
                        var targetDownloadVersion = dialogContext.SelectedRepository.Versions
                            .MaxBy(version => version.UploadTime);
                        if (targetDownloadVersion is null)
                            return;

                        targetVersion = await targetDownloadVersion.DownloadInstallationVersion();
                    }
                    else
                    {
                        targetVersion = dialogContext.SelectedVersion;
                    }
                
                    var id = Guid.NewGuid();
                    var profilePath = Path.Combine(AppSettings.Application.InstallationPath, id.ToString());
                    Directory.CreateDirectory(profilePath);
                
                    var profile = new InstallationProfile
                    {
                        ProfileType = dialogContext.ProfileType,
                        Name = dialogContext.ProfileType == EProfileType.Repository ? targetVersion.RepositoryName : targetVersion.Name,
                        Version = targetVersion.Version,
                        Directory = profilePath,
                        ExecutableName = Path.GetFileName((string?)targetVersion.ExecutablePath),
                        Id = id,
                        IconUrl = targetVersion.IconUrl,
                        RepositoryUrl = targetVersion.RepositoryUrl
                    };
                
                    File.Copy(targetVersion.ExecutablePath, profile.ExecutablePath);
                
                    ProfilesSource.Add(profile);
                })
            }
        ]);
    }

    public async Task ImportInstallation()
    {
        if (await App.BrowseFileDialog(fileTypes: [Globals.ExecutableFileType]) is not { } executablePath) return;
        
        var content = new CreateProfileDialog();
        
        Info.Dialog("Import New Profile", content: content, buttons: [
            new DialogButton
            {
                Text = "Create",
                Action = () => TaskService.Run(async () =>
                {
                    CreateProfileDialogContext? dialogContext = null;
                    await TaskService.RunDispatcherAsync(() => dialogContext = content.DataContext as CreateProfileDialogContext);
                    if (dialogContext is null) return;
                
                    InstallationVersion targetVersion;
                    if (dialogContext.ProfileType == EProfileType.Repository)
                    {
                        var targetDownloadVersion = dialogContext.SelectedRepository.Versions
                            .MaxBy(version => version.UploadTime)!;

                        targetVersion = await targetDownloadVersion.DownloadInstallationVersion();
                    }
                    else
                    {
                        targetVersion = dialogContext.SelectedVersion;
                    }
                
                    var id = Guid.NewGuid();
                
                    var profile = new InstallationProfile
                    {
                        ProfileType = dialogContext.ProfileType,
                        Name = Path.GetFileNameWithoutExtension(executablePath),
                        Version = targetVersion.Version,
                        Directory = Path.GetDirectoryName(executablePath)!,
                        ExecutableName = Path.GetFileName(executablePath),
                        Id = id,
                        IconUrl = targetVersion.IconUrl,
                        RepositoryUrl = targetVersion.RepositoryUrl
                    };
                
                    File.Copy(targetVersion.ExecutablePath, profile.ExecutablePath, true);
                
                    ProfilesSource.Add(profile);
                })
            }
        ]);
    }
    

    public async Task Delete(InstallationProfile profile)
    {
        Info.Dialog($"Delete \"{profile.Name}\"", "Are you sure you want to delete this profile? " + (profile.IsImported ? "No files will be deleted." : "The installation will be fully deleted."), buttons: [
            new DialogButton
            {
                Text = "Delete",
                Action = () => TaskService.Run(async () =>
                {
                    await profile.DeleteAndCleanup();
                    ProfilesSource.Remove(profile);
                })
            }
        ]);
    }
}