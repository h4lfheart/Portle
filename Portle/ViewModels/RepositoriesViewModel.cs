using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DynamicData;
using DynamicData.Binding;
using FluentAvalonia.UI.Controls;
using Portle.Application;
using Portle.Extensions;
using Portle.Framework;
using Portle.Models.Downloads;
using Portle.Models.Information;
using Portle.Models.Repository;
using Portle.Services;
using ReactiveUI;
using Serilog;

namespace Portle.ViewModels;

public partial class RepositoriesViewModel : ViewModelBase
{
    [ObservableProperty] private RepositoryService _repositoryService;

    public RepositoriesViewModel(RepositoryService repositoryService)
    {
        RepositoryService = repositoryService;
        
        var repoFilter = this
            .WhenAnyValue(viewModel => viewModel.SearchFilter)
            .Select(searchFilter =>
            {
                return new Func<DownloadRepository, bool>(repository => MiscExtensions.Filter(repository.Title, searchFilter));
            });
        
        RepositoryService.Repositories.Connect()
            .Filter(repoFilter)
            .Sort(SortExpressionComparer<DownloadRepository>.Descending(x => x.Title))
            .ObserveOn(RxApp.MainThreadScheduler)
            .Bind(out var repoCollection)
            .Subscribe();
        
        Repositories = repoCollection;
    }
    
    [ObservableProperty] private string _searchFilter = string.Empty;
    [ObservableProperty] private ReadOnlyObservableCollection<DownloadRepository> _repositories = new([]);
    
    [RelayCommand]
    public async Task Refresh()
    {
        await RepositoryService.Refresh();
    }

    [RelayCommand]
    public async Task AddRepository()
    {
        var textBox = new TextBox
        {
            Watermark = "Repository URL"
        };
        
        Info.Dialog("Add Repository", content: textBox, buttons: [
            new DialogButton
            {
                Text = "Add",
                Action = () =>
                {
                    var repositoryUrl = string.Empty;
                    TaskService.RunDispatcher(() => repositoryUrl = textBox.Text);
                    
                    if (string.IsNullOrWhiteSpace(repositoryUrl))
                        return;

                    TaskService.Run(async () => await RepositoryService.AddRepository(repositoryUrl));
                }
            }
        ]);
    }
    
    [RelayCommand]
    public async Task Delete(DownloadRepository repository)
    {
        Info.Dialog($"Remove \"{repository.Title}\"", "Are you sure you want to remove this repository?", buttons: [
            new DialogButton
            {
                Text = "Remove",
                Action = () =>
                {
                    RepositoryService.RemoveRepository(repository);
                }
            }
        ]);
    }
    
}