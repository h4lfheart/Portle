using System;
using Avalonia.Controls;
using Avalonia.Input;
using FluentAvalonia.UI.Controls;
using Portle.Application;
using Portle.Framework;
using Portle.WindowModels;

namespace Portle.Windows;

public partial class AppWindow : WindowBase<AppWindowModel>
{
    public AppWindow() : base(AppWM, initializeWindowModel: false)
    {
        InitializeComponent();
        DataContext = WindowModel;
        WindowModel.ContentFrame = ContentFrame;
        WindowModel.NavigationView = NavigationView;
    }

    private void OnItemInvoked(object? sender, NavigationViewItemInvokedEventArgs e)
    {
        var viewName = $"Portle.Views.{e.InvokedItemContainer.Tag}View";
        
        var type = Type.GetType(viewName);
        if (type is null) return;
        
        WindowModel.Navigate(type);
    }

    private void OnPointerPressedUpperBar(object? sender, PointerPressedEventArgs e)
    {
        BeginMoveDrag(e);
    }
    
    private void OnClosing(object? sender, WindowClosingEventArgs e)
    {
        if (AppSettings.Current.MinimizeToTray)
        {
            e.Cancel = true;
            
            AppSettings.Save();
            Hide();
        }

    }
}