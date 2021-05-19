namespace AzWikiLinkChecker
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Web;

    using ColoredConsole;

    using CsvHelper;
    using CsvHelper.Configuration;

    using Markdig;
    using Markdig.Syntax;
    using Markdig.Syntax.Inlines;

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
                    Console.Write(".");
                    wiki.pages.Add(page);
                }

                foreach (var page in wiki.pages)
                {
                    Console.Write($" {wiki.pages.Count}. ".PadLeft(6));
                    Console.WriteLine(page.sanitizedWikiUrl);
                    // Console.WriteLine($"      gitItemPath: {page.gitItemPath}");
                    // Console.WriteLine($"        remoteUrl: {page.remoteUrl}");
                    // Console.WriteLine($"         subPages: {page.subPages?.Length}");
                    Console.WriteLine($"      git: {page.sanitizedGitUrl}");
                    Debug.WriteLine($"{page.sanitizedWikiUrl}:\n{page.content}");
                    foreach (var link in Markdown.Parse(page.content).Descendants<ParagraphBlock>().SelectMany(x => x.Inline.Descendants<LinkInline>()))
                    {
                        Console.WriteLine($"\n      link: {link.Url}");
                        var uri = link.Url;
                        if (new Uri(link.Url, UriKind.RelativeOrAbsolute).IsAbsoluteUri)
                        {
                            uri = HttpUtility.UrlDecode(link.Url); // .Replace("-", " ").Replace(".md", string.Empty);
                        }
                        else
                        {
                            var url = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(page.path), link.Url));
                            uri = url.Substring(Path.GetPathRoot(url).Length).Replace("\\", "/").Replace("-", " ").Replace(".md", string.Empty);
                        }

                        if (new Uri(uri, UriKind.RelativeOrAbsolute).IsAbsoluteUri)
                        {
                            ColorConsole.WriteLine($"       uri: {uri}".Blue());
                        }
                        else
                        {
                            if (uri.Contains(".attachment") || uri.Contains(".images"))
                            {
                                ColorConsole.WriteLine($"       uri: {uri}".DarkYellow());
                            }
                            else
                            {
                                if (wiki.pages.Select(p => p.path.TrimStart('/')).Contains(uri))
                                {
                                    Console.WriteLine($"       uri: {uri}");
                                }
                                else
                                {
                                    ColorConsole.WriteLine($"       uri: {uri}".Red());
                                }
                            }
                        }
                    }

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
