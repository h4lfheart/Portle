using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using DynamicData;
using Portle.Extensions;
using Portle.Models.Downloads;

namespace Portle.Services;

public partial class RepositoryService : ObservableObject, IService
{
    public readonly SourceList<DownloadRepository> Repositories = new();
    public readonly SourceList<DownloadVersion> Versions = new();

    public RepositoryService()
    {
        TaskService.Run(Refresh);
    }

    public async Task Refresh()
    {
        Repositories.Clear();
        Versions.Clear();
        
        foreach (var repositoryUrl in AppSettings.Application.Repositories.ToArray())
        {
            await LoadRepository(repositoryUrl);
        }
    }
    
    public async Task AddRepository(string url, bool verbose = true)
    {
        if (Repositories.Items.Any(repo => repo.RepositoryUrl.Equals(url, StringComparison.OrdinalIgnoreCase)))
        {
            if (verbose)
                Info.Message("Repository", $"A repository already exists with the url \"{url}\"");
            return;
        }
        
        var loaded = await LoadRepository(url);
        if (loaded)
        {
            AppSettings.Application.Repositories.Add(url);
        }
        else
        {
            Info.Message("Repository", $"Failed to load a repository from the url \"{url}\"");
        }
    }
    
    public void RemoveRepository(DownloadRepository repository)
    {
        if (!AppSettings.Application.Repositories.Remove(repository.RepositoryUrl))
            return;

        Repositories.Remove(repository);
    }
    
    private async Task<bool> LoadRepository(string repoUrl)
    {
        if (await Api.General.Repository(repoUrl) is not { } repositoryResponse) 
            return false;

        var repo = new DownloadRepository(repositoryResponse, repoUrl);
        Repositories.Add(repo);

        foreach (var version in repo.Versions)
        {
            Versions.Add(version);
        }

        return true;
    }

}