using Portle.Application;
using Portle.Framework;
using Portle.ViewModels;

namespace Portle.Views;

public partial class SettingsView : ViewBase<SettingsViewModel>
{
    public SettingsView() : base(AppSettings.Application, initializeViewModel: false)
    {
        InitializeComponent();
    }
}