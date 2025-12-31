using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AsyncImageLoader;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using Newtonsoft.Json;
using Portle.Models.API.Response;

namespace Portle.Models.Downloads;

public partial class DownloadRepository : ObservableObject
{
    [ObservableProperty] private string _id;
    [ObservableProperty] private string _title;
    [ObservableProperty] private string _description;
    [ObservableProperty] private string? _iconUrl;
    [ObservableProperty] private string _repositoryUrl;

    [ObservableProperty] private ObservableCollection<DownloadVersion> _versions = [];
    
    [ObservableProperty] private bool _isFilterEnabled = true;
    
    public DownloadRepository(RepositoryResponse response, string repositoryUrl)
    {
        RepositoryUrl = repositoryUrl;
        SetPropertiesFrom(response);
    }

    public void SetPropertiesFrom(RepositoryResponse response)
    {
        Id = response.Id;
        Title = response.Title;
        Description = response.Description;
        IconUrl = response.Icon;
        Versions = [..response.Versions.Select(version => new DownloadVersion
        {
            ParentRepository = this,
            Version = version.Version,
            ExecutableUrl = version.ExecutableURL,
            UploadTime = version.UploadTime
        })];
    }

    public async Task Refresh()
    {
        if (await Api.General.Repository(RepositoryUrl) is not { } response) return;

        SetPropertiesFrom(response);
    }
}