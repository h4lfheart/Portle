using System;
using Avalonia.Controls;
using Avalonia.Input;
using FluentAvalonia.UI.Controls;
using Portle.Application;
using Portle.Controls.Navigation.Sidebar;
using Portle.Framework;
using Portle.WindowModels;

namespace Portle.Windows;

public partial class AppWindow : WindowBase<AppWindowModel>
{
    public AppWindow() : base(initializeWindowModel: false)
    {
        InitializeComponent();
        DataContext = WindowModel;

        WindowModel.HideCommand = Hide;
        
        Navigation.App.Initialize(Sidebar, ContentFrame);
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        base.OnClosing(e);
        
        if (AppSettings.Application.MinimizeToTray)
        {
            e.Cancel = true;
            
            AppSettings.Save();
            Hide();
        }
    }

    private void OnSidebarItemSelected(object? sender, SidebarItemSelectedArgs args)
    {
        Navigation.App.Open(args.Tag);
    }
}