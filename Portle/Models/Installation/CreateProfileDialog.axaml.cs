using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using Portle.Application;
using Portle.Models.Downloads;

namespace Portle.Models.Installation;

public partial class CreateProfileDialog : UserControl
{
    public CreateProfileDialog()
    {
        InitializeComponent();
        DataContext = new CreateProfileDialogContext();
    }
}

public partial class CreateProfileDialogContext : ObservableObject
{
    [ObservableProperty] private EProfileType _profileType;

    // repository
    [ObservableProperty] private DownloadRepository _selectedRepository = RepositoriesVM.Repositories.FirstOrDefault()!;
    [ObservableProperty] private ObservableCollection<DownloadRepository> _repositories = RepositoriesVM.Repositories;
    
    // custom
    [ObservableProperty] private InstallationVersion _selectedVersion = AppSettings.Current.DownloadedVersions.FirstOrDefault()!;
    [ObservableProperty] private ObservableCollection<InstallationVersion> _versions = AppSettings.Current.DownloadedVersions;
    
}