using System;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Portle.Framework;
using Portle.Services;
using Portle.ViewModels;
using Portle.WindowModels;
using InfoService = Portle.Services.InfoService;

namespace Portle.Application;

public static class AppServices
{
    public static ServiceProvider Services;

    public static void Initialize()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddCommonServices();
        serviceCollection.AddViewModels();

        Services = serviceCollection.BuildServiceProvider();
    }

    // Services
    public static AppService App => Services.GetRequiredService<AppService>();
    public static SettingsService AppSettings => Services.GetRequiredService<SettingsService>();
    public static InfoService Info => Services.GetRequiredService<InfoService>();
    public static NavigationService Navigation => Services.GetRequiredService<NavigationService>();
    public static APIService Api => Services.GetRequiredService<APIService>();
    public static RepositoryService Repositories => Services.GetRequiredService<RepositoryService>();
    
    // ViewModels
    public static AppWindowModel AppWM => Services.GetRequiredService<AppWindowModel>();
    public static ProfilesViewModel ProfilesVM => Services.GetRequiredService<ProfilesViewModel>();
    public static DownloadsViewModel DownloadsVM => Services.GetRequiredService<DownloadsViewModel>();
    public static RepositoriesViewModel RepositoriesVM => Services.GetRequiredService<RepositoriesViewModel>();
}

public static class AppServiceExtensions
{
    extension(ServiceCollection collection)
    {
        public  void AddCommonServices()
        {
            var serviceTypes = Assembly.GetAssembly(typeof(IService))?
                .GetTypes()
                .Where(type => !type.IsAbstract && type.IsAssignableTo(typeof(IService))) ?? [];

            foreach (var serviceType in serviceTypes)
            {
                collection.AddSingleton(serviceType);
            }
        }
    
        public void AddViewModels()
        {
            var viewModelTypes = Assembly.GetAssembly(typeof(ViewModelBase))?
                .GetTypes()
                .Where(type => !type.IsAbstract && type.IsAssignableTo(typeof(ViewModelBase))) ?? [];

            foreach (var viewModelType in viewModelTypes)
            {
                if (viewModelType.GetCustomAttribute<TransientAttribute>() is not null)
                {
                    collection.AddTransient(viewModelType);
                }
                else
                {
                    collection.AddSingleton(viewModelType);
                }
            }
        }
    }
}

public class TransientAttribute : Attribute;