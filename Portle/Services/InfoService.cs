using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using FluentAvalonia.UI.Controls;
using Portle.Extensions;
using Portle.Models.Information;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;

namespace Portle.Services;

public partial class InfoService : ObservableObject, IService
{
    [ObservableProperty] private ObservableCollection<MessageData> _messages = [];
    [ObservableProperty] private DialogQueue _dialogQueue = new();
    
    private readonly object _messageLock = new();
    
    public string LogFilePath;
    
    public DirectoryInfo LogsFolder => new(Path.Combine(App.ApplicationDataFolder.FullName, "Logs"));
    
    public InfoService()
    {
        TaskService.Exception += HandleException;
        
        Dispatcher.UIThread.UnhandledException += (sender, args) =>
        {
            args.Handled = true;
            HandleException(args.Exception);
        };
    }

    public void CreateLogger()
    {
        LogsFolder.Create();
        
        LogFilePath = Path.Combine(LogsFolder.FullName, $"Portle-{DateTime.Now:yyyy-MM-dd-hh-mm-ss}.log");
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console(theme: AnsiConsoleTheme.Literate)
            .WriteTo.File(LogFilePath)
            .CreateLogger();
    }

    public void Message(string title, string message, InfoBarSeverity severity = InfoBarSeverity.Informational, bool autoClose = true, string id = "", float closeTime = 3f, bool useButton = false, string buttonTitle = "", Action? buttonCommand = null)
    {
        Message(new MessageData(title, message, severity, autoClose, id, closeTime, useButton, buttonTitle, buttonCommand));
    }

    public void Message(MessageData data)
    {
        Messages.Add(data);
        if (!data.AutoClose) return;
        
        TaskService.Run((Func<Task>)(async () =>
        {
            await Task.Delay((int) (data.CloseTime * 1000));
            
            lock (_messageLock)
                Messages.Remove(data);
        }));
    }
    
    public void UpdateMessage(string id, string message)
    {
        var foundInfoBar = Enumerable.FirstOrDefault<MessageData>(Messages, infoBar => infoBar.Id == id);

        foundInfoBar?.Message = message;
    }
    
    public void UpdateTitle(string id, string title)
    {
        var foundInfoBar = Enumerable.FirstOrDefault<MessageData>(Messages, infoBar => infoBar.Id == id);

        foundInfoBar?.Title = title;
    }
    
    public void CloseMessage(string id)
    {
        lock (_messageLock)
            MiscExtensions.RemoveAll<MessageData>(Messages, info => info.Id == id);
    }
    
    public void Dialog(string title, string? message = null, object? content = null, DialogButton[]? buttons = null, bool canClose = true)
    {
        DialogQueue.Enqueue(new DialogData
        {
            Title = title,
            Message = message,
            Content = content,
            Buttons = buttons is not null ? [..buttons] : [],
            CanClose = canClose
        });
    }
    
    public void HandleException(Exception e)
    {
        var exceptionString = e.ToString();
        Log.Error(exceptionString);

        
        Dialog("An unhandled exception has occurred", exceptionString, buttons: [
            new DialogButton
            {
                Text = "Open Logs Folder",
                Action = () => App.LaunchSelected(LogFilePath)
            }
        ]);
    }
}