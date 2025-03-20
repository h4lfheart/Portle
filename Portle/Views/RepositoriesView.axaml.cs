using Portle.Framework;
using Portle.ViewModels;

namespace Portle.Views;

public partial class RepositoriesView : ViewBase<RepositoriesViewModel>
{
    public RepositoriesView() : base(RepositoriesVM, initializeViewModel: false)
    {
        InitializeComponent();
    }
}