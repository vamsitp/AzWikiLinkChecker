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
                { "-v", "Version" },
                { "-t", "Token" },
            };

            var builder = new ConfigurationBuilder();
            builder.AddCommandLine(args, switchMappings);

            var config = builder.Build();
            var account = new Account { Org = config["Org"], Project = config["Project"], Token = config["Token"], Wiki = config["Wiki"], Version = config["Version"] };
            foreach (var c in config.AsEnumerable().Where(x => x.Key != "Token"))
            {
                Console.WriteLine($"{c.Key}: {c.Value}");
            }

            Console.WriteLine($"\n{account.BaseUrl} (Wikis: {account.Wiki ?? "All"})\n");
            var wikis = await AzDo.GetWikisAsync(account);
            var exitCode = 0;
            foreach (var wiki in wikis)
            {
                Console.WriteLine($" {wiki.name} ({wiki.remoteUrl})\n");
                Console.Write(" ");
                await foreach (var page in AzDo.GetPagesAsync(account, wiki))
                {
                    wiki.pages.Add(page);
                    Console.Write(".");
                }

                foreach (var page in wiki.pages)
                {
                    Debug.WriteLine($"{page.sanitizedWikiUrl}:\n{page.content}");
                    Console.Write($" {wiki.pages.Count}. ".PadLeft(6));
                    Console.WriteLine(page.sanitizedWikiUrl);
                    // Console.WriteLine($"      gitItemPath: {page.gitItemPath}");
                    // Console.WriteLine($"        remoteUrl: {page.remoteUrl}");
                    // Console.WriteLine($"         subPages: {page.subPages?.Length}");
                    Console.WriteLine($"      git: {page.sanitizedGitUrl}");

                    foreach (var link in Markdown.Parse(page.content).Descendants<ParagraphBlock>().SelectMany(x => x.Inline.Descendants<LinkInline>()))
                    {
                        var uri = link.Url;
                        if (new Uri(link.Url, UriKind.RelativeOrAbsolute).IsAbsoluteUri)
                        {
                            uri = HttpUtility.UrlDecode(link.Url); // .Replace("-", " ").Replace(".md", string.Empty);
                        }
                        else
                        {
                            var u = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(page.path), link.Url));
                            uri = u.Substring(Path.GetPathRoot(u).Length).Replace("\\", "/").Replace("-", " ").Replace(".md", string.Empty);
                        }

                        var color = ConsoleColor.White;
                        if (new Uri(uri, UriKind.RelativeOrAbsolute).IsAbsoluteUri)
                        {
                            color = ConsoleColor.Blue;
                        }
                        else
                        {
                            var paths = uri.Split(new[] { '/', '\\' });
                            if (paths.Any(x => x.Trim().StartsWith(".")) || paths.Any(x => x.Contains("#"))) // .attachment / .images / #anchor etc.
                            {
                                color = ConsoleColor.DarkYellow;
                            }
                            else
                            {
                                if (wiki.pages.Select(p => p.path.TrimStart('/')).Contains(uri))
                                {
                                    color = ConsoleColor.Green;
                                }
                                else
                                {
                                    color = ConsoleColor.Red;
                                }
                            }
                        }

                        page.links.Add((link, uri, color));
                        ColorConsole.WriteLine($"\n      link: ", $"[{(link.FirstOrDefault()?.ToString() ?? string.Empty)}]".Cyan(), $" {link.Url}");
                        ColorConsole.WriteLine($"      root: {uri}".Color(color));
                    }

                    Console.WriteLine($"      -----\n");
                }

                Console.WriteLine(" -------------------------\n");
                var file = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), $"azwlc_{wiki.name}.csv");
                await Save(wiki.pages, file);

                var brokenLinksFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), $"azwlc_{wiki.name}_broken_links.csv");
                var brokenLinks = wiki.pages.SelectMany(page => page.links.Where(l => l.color == ConsoleColor.Red).Select(l => new { version = string.IsNullOrWhiteSpace(account.Version) ? (wiki.versions?.FirstOrDefault()?.version ?? string.Empty) : account.Version, page = page.sanitizedWikiUrl, text = (l.link?.FirstOrDefault()?.ToString()) ?? string.Empty, content_link = l.link.Url, sanitized_link = l.sanitizedUrl }));
                await Save(brokenLinks, brokenLinksFile);

                Console.WriteLine($"Saved to: {file} & {brokenLinksFile}");
                exitCode = brokenLinks?.Count() ?? 0; // To check %errorlevel% after the app exists in the pipeline
            }

            Console.WriteLine("--------------------------------------------------");
            Environment.ExitCode = exitCode;
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
