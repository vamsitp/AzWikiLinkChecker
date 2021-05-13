namespace AzWikiLinkChecker
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Microsoft.Extensions.Configuration;

    class Program
    {
        static async Task Main(string[] args)
        {
            var switchMappings = new Dictionary<string, string>()
            {
                { "-o", "Org" },
                { "-p", "Project" },
                { "-w", "Wiki" },
                { "-t", "Token" },
            };

            var builder = new ConfigurationBuilder();
            builder.AddCommandLine(args, switchMappings);

            var config = builder.Build();
            var account = new Account { Org = config["Org"], Project = config["Project"], Token = config["Token"], Wiki = config["Wiki"] };
            Console.WriteLine($"{account.BaseUrl} (Wikis: {account.Wiki ?? "All"})\n");
            var wikis = await AzDo.GetWikisAsync(account);
            foreach (var wiki in wikis)
            {
                Console.WriteLine($" {wiki.name} ({wiki.remoteUrl})\n");
                await foreach (var page in AzDo.GetPagesAsync(account, wiki))
                {
                    wiki.pages.Add(page);
                    Console.Write($" {wiki.pages.Count}. ".PadLeft(6));
                    Console.WriteLine($"{account.BaseUrl}/_wiki/wikis/{wiki.name}/{page.id}/{page.path.Split('/', StringSplitOptions.RemoveEmptyEntries).LastOrDefault()}");
                    Console.WriteLine($"      gitItemPath: {page.gitItemPath}");
                    Console.WriteLine($"        remoteUrl: {page.remoteUrl}");
                    Console.WriteLine($"         subPages: {page.subPages?.Length}");
                    Console.WriteLine($"          content: {page.content}");
                    Console.WriteLine($"      -----\n");
                }

                Console.WriteLine(" -------------------------\n");
            }

            Console.WriteLine("--------------------------------------------------");
            Console.ReadLine();
        }
    }
}
