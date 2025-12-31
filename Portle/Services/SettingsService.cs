using System;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using Newtonsoft.Json;
using Portle.ViewModels;
using Serilog;

namespace Portle.Services;

public partial class SettingsService : ObservableObject, IService
{
    [ObservableProperty] private SettingsViewModel _application = new();

    public bool ShouldSaveOnExit = true;
    
    public static readonly DirectoryInfo DirectoryPath = new(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Portle"));
    public static readonly FileInfo FilePath = new(Path.Combine(DirectoryPath.FullName, "AppSettings.json"));

    public SettingsService()
    {
        DirectoryPath.Create();
    }
    
    public void Load()
    {
        if (!FilePath.Exists) return;
        
        try
        {
            var settings = JsonConvert.DeserializeObject<SettingsService>(File.ReadAllText(FilePath.FullName));
            if (settings is null) return;

            foreach (var property in settings.GetType().GetProperties())
            {
                if (!property.CanWrite) return;
                
                var value = property.GetValue(settings);
                property.SetValue(this, value);
            }
        }
        catch (Exception e)
        {
            Log.Error("Failed to load settings:");
            Log.Error(e.ToString());
        }
    }

    public void Save()
    {
        try
        {
            Application.Profiles = [..ProfilesVM.ProfilesSource.Items];
            File.WriteAllText(FilePath.FullName, JsonConvert.SerializeObject(this, Formatting.Indented));
        }
        catch (Exception e)
        {
            Log.Error("Failed to save settings:");
            Log.Error(e.ToString());
        }
    }
    
    public void Reset()
    {
        File.Delete(FilePath.FullName);
        ShouldSaveOnExit = false;
    }
}