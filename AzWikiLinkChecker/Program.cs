namespace AzWikiLinkChecker
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;

    using ColoredConsole;

    using CsvHelper;
    using CsvHelper.Configuration;

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
                Console.Write(" ");
                await foreach (var page in AzDo.GetPagesAsync(account, wiki))
                {
                    wiki.pages.Add(page);
                    Debug.WriteLine($"{page.sanitizedWikiUrl}:\n{page.content}");
                    Console.Write($" {wiki.pages.Count}. ".PadLeft(6));
                    Console.WriteLine(page.sanitizedWikiUrl);
                    // Console.WriteLine($"      gitItemPath: {page.gitItemPath}");
                    // Console.WriteLine($"        remoteUrl: {page.remoteUrl}");
                    // Console.WriteLine($"         subPages: {page.subPages?.Length}");
                    Console.WriteLine($"      git: {page.sanitizedGitUrl}");
                    foreach (var link in page.links)
                    {
                        Console.WriteLine($"\n      link: {link.link.Url}");
                        ColorConsole.WriteLine($"      root: {link.sanitizedUrl}".Color(link.color));
                    }

                    Console.WriteLine($"      -----\n");
                }

                Console.WriteLine(" -------------------------\n");
                var file = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), $"azwlc_{wiki.name}.csv");
                await Save(wiki.pages, file);

                var brokenLinksFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), $"azwlc_{wiki.name}_broken_links.csv");
                var brokenLinks = wiki.pages.SelectMany(page => page.links.Where(l => l.color == ConsoleColor.Red).Select(l => new { page = page.sanitizedWikiUrl, content_link = l.link.Url, sanitized_link = l.sanitizedUrl }));
                await Save(brokenLinks, brokenLinksFile);

                Console.WriteLine($"Saved to: {file} & {brokenLinksFile}");
            }

            Console.WriteLine("--------------------------------------------------");
            Console.ReadLine();
        }

        private static async Task Save<T>(IEnumerable<T> results, string file)
        {
            using (var reader = File.CreateText(file))
            {
                using (var csvWriter = new CsvWriter(reader, new CsvConfiguration(CultureInfo.InvariantCulture)))
                {
                    await csvWriter.WriteRecordsAsync(results);
                }
            }
        }
    }
}
