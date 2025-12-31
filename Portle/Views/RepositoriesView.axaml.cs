using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using Portle.Framework;
using Portle.Models.Downloads;
using Portle.Models.Information;
using Portle.ViewModels;

namespace Portle.Views;

public partial class RepositoriesView : ViewBase<RepositoriesViewModel>
{
    public RepositoriesView()
    {
        InitializeComponent();
    }
}