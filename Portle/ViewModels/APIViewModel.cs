using Portle.Models.API;

namespace Portle.ViewModels;

public class APIViewModel : APIViewModelBase
{
    public MiscAPI Misc;
    
    public APIViewModel() : base("Portle")
    {
        Misc = new MiscAPI(_client);
    }
}