using Portle.Framework;
using Portle.ViewModels;

namespace Portle.Views;

public partial class ProfilesView : ViewBase<ProfilesViewModel>
{
    public ProfilesView() : base(ProfilesVM, initializeViewModel: false)
    {
        InitializeComponent();
    }
}