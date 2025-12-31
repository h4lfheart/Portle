global using static Portle.Application.AppServices;
using Avalonia.Platform.Storage;
using Portle.Models;

namespace Portle;

public static class Globals
{
    
    public static string VersionString => Version.GetDisplayString(EVersionStringType.IdentifierPrefix);
    public static readonly FPVersion Version = new(2, 0, 1, 0, "beta");
    
    public const string DEFAULT_REPOSITORY = "https://api.fortniteporting.app/v1/static/repository";
    
    public static readonly FilePickerFileType ExecutableFileType = new("Executable") { Patterns = ["*.exe"] };
}