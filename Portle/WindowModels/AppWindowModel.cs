using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using FluentAvalonia.UI.Controls;
using Portle.Extensions;
using Portle.Framework;
using Portle.Models.App;
using Portle.Services;

namespace Portle.WindowModels;

public partial class AppWindowModel() : WindowModelBase
{
    [ObservableProperty] private SettingsService _appSettings;
    [ObservableProperty] private InfoService _info;

    public AppWindowModel(SettingsService appSettings, InfoService info) : this()
    {
        AppSettings = appSettings;
        Info = info;
    }

    [ObservableProperty] private string _versionString = Globals.VersionString;
    
    public Action HideCommand;
}