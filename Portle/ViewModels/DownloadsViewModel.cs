using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DynamicData;
using DynamicData.Binding;
using Portle.Application;
using Portle.Extensions;
using Portle.Framework;
using Portle.Models.Downloads;
using Portle.Services;
using ReactiveUI;

namespace Portle.ViewModels;


public partial class RepositoryFilter : ObservableObject
{
    [ObservableProperty] private bool _isEnabled = true;
    [ObservableProperty] private DownloadRepository _repository;

    public RepositoryFilter(DownloadRepository repository)
    {
        Repository = repository;
    }
}

public partial class DownloadsViewModel() : ViewModelBase
{
    [ObservableProperty] private RepositoryService _repositoryService;

    public DownloadsViewModel(RepositoryService repositoryService) : this()
    {
        RepositoryService = repositoryService;
        
        var repositoryFiltersObservable = RepositoryService.Repositories.Connect()
            .Transform(repo => new RepositoryFilter(repo))
            .AutoRefresh(filter => filter.IsEnabled)
            .Publish();
        
        repositoryFiltersObservable
            .ObserveOn(RxApp.MainThreadScheduler)
            .Bind(out var repositoryFilters)
            .Subscribe();
        
        RepositoryFilters = repositoryFilters;
        
        var downloadFilter = this
            .WhenAnyValue(viewModel => viewModel.SearchFilter)
            .CombineLatest(repositoryFiltersObservable.ToCollection())
            .Select(filterData =>
            {
                var (searchFilter, filters) = filterData;
                var enabledRepoIds = filters
                    .Where(f => f.IsEnabled)
                    .Select(f => f.Repository.Id)
                    .ToHashSet();
                
                return new Func<DownloadVersion, bool>(version =>
                    MiscExtensions.Filter(version.DisplayString, searchFilter) &&
                    enabledRepoIds.Contains(version.ParentRepository.Id));
            });
        
        RepositoryService.Versions.Connect()
            .Filter(downloadFilter)
            .Sort(SortExpressionComparer<DownloadVersion>.Descending(x => x.UploadTime))
            .ObserveOn(RxApp.MainThreadScheduler)
            .Bind(out var versionCollection)
            .Subscribe();
        
        Versions = versionCollection;

        repositoryFiltersObservable.Connect();
    }
    
    [ObservableProperty] private string _searchFilter = string.Empty;
    [ObservableProperty] private ReadOnlyObservableCollection<DownloadVersion> _versions = new([]);
    [ObservableProperty] private ReadOnlyObservableCollection<RepositoryFilter> _repositoryFilters = new([]);

    
    [RelayCommand]
    public async Task Refresh()
    {
        await RepositoryService.Refresh();
    }
}