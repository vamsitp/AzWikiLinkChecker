namespace AzWikiLinkChecker
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Threading.Tasks;

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
                await foreach (var page in AzDo.GetPagesAsync(account, wiki))
                {
                    wiki.pages.Add(page);
                    Console.Write($" {wiki.pages.Count}. ".PadLeft(6));
                    Console.WriteLine(page.sanitizedWikiUrl);
                    // Console.WriteLine($"      gitItemPath: {page.gitItemPath}");
                    // Console.WriteLine($"        remoteUrl: {page.remoteUrl}");
                    // Console.WriteLine($"         subPages: {page.subPages?.Length}");
                    Console.WriteLine($"      git: {page.sanitizedGitUrl}");
                    Console.WriteLine($"      content: {page.content}");
                    Console.WriteLine($"      -----\n");
                }

                Console.WriteLine(" -------------------------\n");
                var file = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), $"AzWikiLinkChecker_{wiki.name}.csv");
                await Save(wiki.pages, file);
                Console.WriteLine($"Saved to: {file}");
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
