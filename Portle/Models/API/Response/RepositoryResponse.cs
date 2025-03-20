using System;
using System.Collections.Generic;

namespace Portle.Models.API.Response;

public class RepositoryResponse
{
    public string Id;
    public string Title;
    public string Description;
    public string? Icon;
    public List<RepositoryVersion> Versions = [];
}

public class RepositoryVersion
{
    public FPVersion Version;
    public string ExecutableURL;
    public DateTime UploadTime;
}