﻿using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using Avalonia;
using Portle.Application;
using Portle.Services;
using Serilog;

namespace Portle;

internal static class Program
{
    private static Mutex _programMutex;
    
    [STAThread]
    public static void Main(string[] args)
    {
        try
        {
            _programMutex = new Mutex(true, "PortleMutex", out var isNew);
            
            if (isNew)
            {
                StartNewApp(args);
            }
            else
            {
                OpenExistingApp(args);
            }
        }
        catch (Exception e)
        {
            Log.Fatal(e.ToString());
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    public static void StartNewApp(string[] args)
    {
        using var pipe = new NamedPipeServerStream("Portle");
        
        var responseThread = new Thread(() =>
        {
            var reader = new BinaryReader(pipe);
            while (true)
            {
                pipe.WaitForConnection();

                var argCount = reader.ReadInt32();
                var arguments = new string[argCount];
                for (var i = 0; i < argCount; i++)
                {
                    arguments[i] = reader.ReadString();
                }
            
                TaskService.RunDispatcher(() => ExecuteArguments(arguments));
                
                pipe.Disconnect();
            }
        });

        responseThread.IsBackground = true;
        responseThread.Start();
        
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }
    
    public static void OpenExistingApp(string[] args)
    {
        using var pipe = new NamedPipeClientStream("Portle");
        pipe.Connect(1000);

        var writer = new BinaryWriter(pipe);
        writer.Write(args.Length);
        foreach (var arg in args)
        {
            writer.Write(arg);
        }
    }
    
    private static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace()
            .With(new Win32PlatformOptions { CompositionMode = [Win32CompositionMode.WinUIComposition] });
}