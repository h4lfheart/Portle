using System.Threading.Tasks;
using RestSharp;
using RepositoryResponse = Portle.Models.API.Response.RepositoryResponse;

namespace Portle.Models.API;

public class MiscAPI(RestClient client) : APIBase(client)
{
    public async Task<RepositoryResponse?> GetRepositoryAsync(string url)
    {
        return await ExecuteAsync<RepositoryResponse>(url);
    }

    public RepositoryResponse? GetRepository(string url)
    {
        return GetRepositoryAsync(url).GetAwaiter().GetResult();
    }
}