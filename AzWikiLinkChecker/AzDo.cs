namespace AzWikiLinkChecker
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Net.Http;
    using System.Text;
    using System.Threading.Tasks;

    using Flurl.Http;

    public class AzDo
    {
        private const string AuthHeader = "Authorization";
        private const string ContentType = "application/json";

        private const string WikiPagesContentData = "{ \"top\": 100, \"continuationToken\": ^^continuationToken^^ }";

        private const string WikisUrl = "/_apis/wiki/wikis?api-version=6.1-preview.2";
        private const string WikiUrl = "/_apis/wiki/wikis/{0}?api-version=6.1-preview.2";
        private const string WikiPagesUrl = "/_apis/wiki/wikis/{0}/pagesbatch?api-version=6.1-preview.1";
        private const string WikiPageUrl = "/_apis/wiki/wikis/{0}/pages{1}?includeContent=true&recursionLevel=full&api-version=6.1-preview.1";

        public static async Task<List<Wiki>> GetWikisAsync(Account account)
        {
            var pat = "Basic " + Convert.ToBase64String(Encoding.ASCII.GetBytes($":{account.Token}"));

            //  https://docs.microsoft.com/en-us/rest/api/azure/devops/wiki/pages%20batch/get?view=azure-devops-rest-6.0
            using (var content = new StringContent(WikiPagesContentData, Encoding.UTF8, ContentType))
            {
                if (string.IsNullOrWhiteSpace(account.Wiki))
                {
                    var url = account.BaseUrl + WikisUrl;
                    var result = await url.WithHeader(AuthHeader, pat).GetJsonAsync<Wikis>();
                    return result.items.ToList();
                }
                else
                {
                    var url = account.BaseUrl + string.Format(CultureInfo.InvariantCulture, WikiUrl, account.Wiki);
                    var result = await url.WithHeader(AuthHeader, pat).GetJsonAsync<Wiki>();
                    return new List<Wiki> { result };
                }
            }
        }

        public static async IAsyncEnumerable<Page> GetPagesAsync(Account account, Wiki wiki)
        {
            var pat = "Basic " + Convert.ToBase64String(Encoding.ASCII.GetBytes($":{account.Token}"));
            string continuationToken = "null";

            do
            {
                //  https://docs.microsoft.com/en-us/rest/api/azure/devops/wiki/pages%20batch/get?view=azure-devops-rest-6.0
                using (var content = new StringContent(WikiPagesContentData.Replace("^^continuationToken^^", continuationToken), Encoding.UTF8, ContentType))
                {
                    // TODO:
                    var url = account.BaseUrl + string.Format(CultureInfo.InvariantCulture, WikiPagesUrl, wiki.name);

                    var result = await url.WithHeader(AuthHeader, pat).PostAsync(content);
                    var pages = await result.GetJsonAsync<Pages>();
                    continuationToken = result.Headers.SingleOrDefault(x => x.Name.Equals("X-MS-ContinuationToken", StringComparison.OrdinalIgnoreCase)).Value;
                    foreach (var item in pages.items)
                    {
                        url = account.BaseUrl + string.Format(CultureInfo.InvariantCulture, WikiPageUrl, wiki.name, item.path); // $"/{item.id}"
                        var page = await url.WithHeader(AuthHeader, pat).GetJsonAsync<Page>();
                        page.sanitizedWikiUrl = $"{account.BaseUrl}/_wiki/wikis/{wiki.name}/{page.id}/{page.path.Split('/', StringSplitOptions.RemoveEmptyEntries).LastOrDefault()}";
                        page.sanitizedGitUrl = $"{account.BaseUrl}/_git/{wiki.repositoryId}?path={page.gitItemPath}";
                        yield return page;
                    }
                }
            }
            while (!string.IsNullOrWhiteSpace(continuationToken));
        }
    }
}
