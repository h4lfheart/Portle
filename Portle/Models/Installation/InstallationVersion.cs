using CommunityToolkit.Mvvm.ComponentModel;
using Newtonsoft.Json;

namespace Portle.Models.Installation;

public partial class InstallationVersion : ObservableObject
{
    [ObservableProperty, NotifyPropertyChangedFor(nameof(Name))] private FPVersion _version;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(Name))] private string _repositoryName;
    [ObservableProperty] private string _executablePath;
    
    [ObservableProperty] private string _repositoryUrl;
    [ObservableProperty] private string? _iconUrl;

    [JsonIgnore] public string Name => $"{RepositoryName} {Version}";

    public override string ToString()
    {
        return Name;
    }
}