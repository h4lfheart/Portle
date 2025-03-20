using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentAvalonia.UI.Controls;
using Portle.Application;
using Portle.Extensions;
using Portle.Framework;
using Portle.Models.Downloads;
using Portle.Models.Repository;

namespace Portle.ViewModels;

public partial class RepositoriesViewModel : ViewModelBase
{
    [ObservableProperty] private ObservableCollection<DownloadRepository> _repositories = [];

    public override async Task Initialize()
    {
        await Refresh();

        await ProfilesVM.UpdateRepositoryProfiles();
    }

    [RelayCommand]
    public async Task Refresh()
    {
        Repositories.Clear();
        
        foreach (var repositoryUrlContainer in AppSettings.Current.Repositories)
        {
            if (await ApiVM.Misc.GetRepositoryAsync(repositoryUrlContainer.RepositoryUrl) is not { } repositoryResponse) continue;

            Repositories.Add(new DownloadRepository(repositoryResponse, repositoryUrlContainer.RepositoryUrl));
        }
    }

    public async Task AddRepository()
    {
        var textBox = new TextBox
        {
            Watermark = "Repository URL"
        };
        
        var dialog = new ContentDialog
        {
            Title = "Add New Repository",
            Content = textBox,
            CloseButtonText = "Cancel",
            PrimaryButtonText = "Add",
            PrimaryButtonCommand = new RelayCommand(async () =>
            {
                var newUrl = textBox.Text;
                if (string.IsNullOrWhiteSpace(newUrl)) return;

                await AddRepository(newUrl, verbose: true);
            })
        };

        await dialog.ShowAsync();
    }

    public async Task AddRepository(string url, bool verbose = false)
    {
        if (AppSettings.Current.Repositories.Any(repo => repo.RepositoryUrl.Equals(url)))
        {
            if (verbose)
                AppWM.Message("Repositories", $"A repository already exists with the url \"{url}\"");
            return;
        }
                
        AppSettings.Current.Repositories.Add(new RepositoryUrlContainer(url));
        await Refresh();
        await DownloadsVM.Refresh();
    }
    
    public async Task Delete(DownloadRepository repository)
    {
        var dialog = new ContentDialog
        {
            Title = $"Remove \"{repository.Title}\"",
            Content = "Are you sure you want to remove this repository?",
            CloseButtonText = "No",
            PrimaryButtonText = "Yes",
            PrimaryButtonCommand = new RelayCommand(async () =>
            {
                AppSettings.Current.Repositories.RemoveAll(repo => repo.RepositoryUrl.Equals(repository.RepositoryUrl));
                await Refresh();
                await DownloadsVM.Refresh();
            })
        };

        await dialog.ShowAsync();
    }
    
}