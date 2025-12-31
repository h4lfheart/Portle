using Avalonia.Controls;
using Lucdem.Avalonia.SourceGenerators.Attributes;

namespace Portle.Controls.Navigation.Sidebar;

public partial class SidebarItemContent : UserControl, ISidebarItem
{
    [AvaDirectProperty] private Control? _content;
    
    public SidebarItemContent()
    {
        InitializeComponent();
    }
}