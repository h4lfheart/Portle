global using static Portle.Services.ApplicationService;
using Avalonia.Platform.Storage;
using Portle.Models;

namespace Portle;

public static class Globals
{
    
    public static string VersionString => Version.GetDisplayString(EVersionStringType.IdentifierPrefix);
    public static readonly FPVersion Version = new(1, 0, 0);
    
    public const string DEFAULT_REPOSITORY = "https://fortniteporting.halfheart.dev/api/v3/repository";
    
    public static readonly FilePickerFileType ExecutableFileType = new("Executable") { Patterns = ["*.exe"] };
}