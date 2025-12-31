using Avalonia.Controls;
using Avalonia.Interactivity;
using Portle.Framework;
using Portle.Models.Downloads;
using Portle.ViewModels;

namespace Portle.Views;

public partial class DownloadsView : ViewBase<DownloadsViewModel>
{
    public DownloadsView()
    {
        InitializeComponent();
    }

    private void OnRepositoryFilterChecked(object? sender, RoutedEventArgs e)
    {
        if (sender is not CheckBox checkBox) return;
        if (checkBox.DataContext is not DownloadRepository downloadRepository) return;
        if (checkBox.IsChecked is not { } isChecked) return;
        if (isChecked == downloadRepository.IsFilterEnabled) return;
        
        downloadRepository.IsFilterEnabled = isChecked;
    }
}