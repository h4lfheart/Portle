global using static Portle.Services.ApplicationService;
using Avalonia.Platform.Storage;

namespace Portle;

public static class Globals
{
    public const string DEFAULT_REPOSITORY = "https://fortniteporting.halfheart.dev/api/v3/repository";
    
    public static readonly FilePickerFileType ExecutableFileType = new("Executable") { Patterns = ["*.exe"] };
}