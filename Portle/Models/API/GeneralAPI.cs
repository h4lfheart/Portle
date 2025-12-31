using System.Threading.Tasks;
using RestSharp;
using RepositoryResponse = Portle.Models.API.Response.RepositoryResponse;

namespace Portle.Models.API;

public class GeneralAPI(RestClient client) : APIBase(client)
{
    public async Task<RepositoryResponse?> Repository(string url)
    {
        return await ExecuteAsync<RepositoryResponse>(url);
    }

    public RepositoryResponse? GetRepository(string url)
    {
        return Repository(url).GetAwaiter().GetResult();
    }
}