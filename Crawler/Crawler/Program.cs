using CsvHelper;
using NDesk.Options;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Crawler
{
    class Program
    {
        static void Help(string msg)
        {
            Console.WriteLine(msg);
            Console.WriteLine("Usage: dotnet Crawler.dll [MODE] [OPTIONS]\n" +
                "\t- dotnet Crawler.dll proxy -> extract list of available proxies. See dotnet Crawler.dll proxy --help for more information\n" +
                "\t- dotnet Crawler.dll titles -> extract list of song and authors. See dotnet Crawler.dll titles --help for more information\n" +
                "\t- dotnet Crawler.dll lyrics -> extract lyrics of the songs. See dotnet Crawler.dll lyrics --help for more information\n" +
                "\t- dotnet Crawler.dll dataset -> create a csv with authors, titles and song lyrics. see dotnet Crawler.dll dataset --help for more information\n" +
                "\t- dotnet Crawler.dll check-titles -> evaluate the percentage of authors/titles csv that were extracted per letter. see dotnet Crawler.dll dataset --help for more information\n" +
                "\t- dotnet Crawler.dll check-lyrics -> evaluate the percentage of lyrics that were extracted per letter. see dotnet Crawler.dll dataset --help for more information\n");
        }

        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Help("");
                return;
            }
            switch (args[0])
            {
                case "proxy":
                    FindProxies(args);
                    break;
                case "titles":
                    ExtractTitles(args);
                    break;
                case "songs":
                    ExtractLyrics(args);
                    break;
                case "dataset":
                    CreateDatasetCsv(args);
                    break;
                case "check-titles":
                    CheckTitles(args);
                    break;
                case "check-lyrics":
                    CheckLyrics(args);
                    break;
                default:
                    Help($"Unknown first argument encountered: {args[0]}");
                    return;
            }
        }

        static List<string> Fill(string[] args, OptionSet p)
        {
            bool help = false;
            List<string> extra = null;
            p.Add("h|?|help", v => help = v != null);
            try
            {
                extra = p.Parse(args);
            }
            catch (OptionException e)
            {
                Console.WriteLine($"unable to parse given options... ({e.Message})");
                ShowHelp(p);
                return null;
            }
            if (help)
            {
                ShowHelp(p);
                return null;
            }
            static void ShowHelp(OptionSet p)
            {
                Console.WriteLine("Usage: dotnet Crawler.dll titles [OPTIONS]");
                Console.WriteLine();
                Console.WriteLine("Options:");
                p.WriteOptionDescriptions(Console.Out);
            }
            return extra;
        }

        /// <summary>
        /// Main entrypoint for the proxy discovery. This function extracts proxies from given files and urls
        /// and test them against the given test URL. The available proxies are written in the given output file
        /// </summary>
        static void FindProxies(string[] args)
        {
            string testUrl = "https://www.paroles.net";
            List<string> files = new List<string>();
            string output = null;
            List<string> urls = new List<string>();
            int retry = 0;
            int verbose = 0;
            var p = new OptionSet() {
                { "v|verbose=", "verbose level. 0: nothing, 1: macro infos, 2: micro infos", (int v) => verbose = v },
                { "f|files=",  $"{Path.PathSeparator}-separated files containing proxy list",  fs =>
                    {
                        files.AddRange(fs.Split(Path.PathSeparator));
                    }
                },
                { "u|urls=", $"{Path.PathSeparator}-separated urls containing proxy lists", us =>
                    {
                        urls.AddRange(us.Split(Path.PathSeparator));
                    }
                },
                { "o|output=",  "ouptut file to write available proxies",  otp => output = otp },
                { "t|test-url=",  "url to test the proxies against (defaults to homepage of paroles.net)",  url => testUrl = url },
                { "r|retry=",  "retry failed proxies against the test url",  (int r) => retry = r},
            };

            if (Fill(args, p) == null)
                return;
            if (verbose > 0)
            {
                Console.WriteLine($"starting proxy test against URL : {testUrl} using \n" +
                    $"    - {urls.Count} url containing potential proxies\n" +
                    $"    - {files.Count} raw files containing potential proxies\n" +
                    $"Output file will be stored in {output}\n");
            }
            ProxyManagerFactory.FindProxies(output, files.ToArray(), urls.ToArray(), testUrl, verbose, retry);
        }

        /// <summary>
        /// Main entrypoint for the titles and authors CSV creation. This function extract the list of songs and authors
        /// starting with the given letters and store them in csv format with approximately 100 songs per file.
        /// </summary>
        static void ExtractTitles(string[] args)
        {
            List<string> letters = new List<string>();
            string outputFolder = null;
            string proxiesFilename = null;
            bool _override = false;
            int verbose = 0;
            int timeout = 3000;
            int maxRetry = int.MaxValue;
            var p = new OptionSet() {
                { "v|verbose=", "verbose level. 0: nothing, 1: macro infos, 2: micro infos", (int v) => verbose = v },
                { "m|max-retry=", "set a limit number of retry to reach the website. Defaults to int.MaxValue", (int m) => maxRetry = m },
                { "o|override", "override previously created csv in the output folder", v => _override = v != null },
                { "p|proxies=",  "ouptut file to write available proxies",  proxies => proxiesFilename = proxies },
                { "l|letters=", $"songs starting letter, separated {Path.PathSeparator}.", l => letters.AddRange(l.Split(Path.PathSeparator)) },
                { "f|folder=", "output folder where csv with authors and titles are stored", folder => outputFolder = folder },
                { "t|timeout=", "max time in ms before request timeout (default is 3000)", (int t) => timeout = t }
            };

            if (Fill(args, p) == null)
                return;

            var proxyManager = ProxyManagerFactory.Build(proxiesFilename);
            var htmlExtractor = new HtmlExtractor(proxyManager);
            var songListExtractor = new SongListExtractor();

            foreach (var letter in letters)
            {
                if (verbose > 0)
                {
                    Console.WriteLine($"extracting titles starting with {letter}...");
                }
                // creating output folder
                var folder = Path.Combine(outputFolder, letter);

                if (_override && Directory.Exists(folder))
                {
                    // warning ?
                    if (verbose > 0)
                    {
                        Console.WriteLine($"deleting previously created folder {folder}");
                    }
                    Directory.Delete(folder);
                }
                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }

                var completionFile = Path.Combine(folder, $"songs-{letter}.complete");
                if (File.Exists(completionFile))
                {
                    // the presence of the file indicated that we already run through all the song starting
                    // with the current letter. Hence we can skip this letter
                    if (verbose > 0)
                    {
                        Console.WriteLine("folder already contains all the pages and override is set to false, skipping letter...");
                    }
                    continue;
                }

                string address = $"https://www.paroles.net/paroles-{letter}";

                var filename = Path.Combine(folder, $"songs-{letter}-1.csv");

                var content = htmlExtractor.Extract(address, maxRetry: maxRetry, timeout: timeout, verbose);
                if (content == null)
                {
                    Console.WriteLine($"couldn't extract content from {address}, skipping this letter");
                    continue;
                }

                var numberOfPages = Utils.GetPageNumber(content);

                var songs = songListExtractor.Extract(content);
                Utils.Save(filename, songs);

                for (int i = 2; i <= numberOfPages; i++)
                {
                    address = $"https://www.paroles.net/paroles-{letter}-{i}";
                    filename = Path.Combine(folder, $"songs-{letter}-{i}.csv");
                    if (File.Exists(filename) && new FileInfo(filename).Length > 0)
                    {
                        if (verbose > 1)
                        {
                            Console.WriteLine($"file {filename} already exists, skipping this page");
                        }
                        continue;
                    }

                    content = htmlExtractor.Extract(address, maxRetry: maxRetry, timeout: timeout, verbose);
                    if (content == null)
                        continue;
                    songs = songListExtractor.Extract(content);
                    Utils.Save(filename, songs);
                }

                // end of loop: we create the completion file if we manage to get all pages
                var success = Directory.EnumerateFiles(folder).Count();
                if (verbose > 0)
                {
                    Console.WriteLine($"Letter {letter} => DONE. Retrieved {success} pages / {numberOfPages}");
                }
                if (success == numberOfPages)
                {
                    File.Create(completionFile);
                }
            }
        }

        /// <summary>
        /// Main entrypoint for the song lyrics extraction. This functions reads the csv containing titles and authors
        /// and extract the lyrics.
        /// </summary>
        static void ExtractLyrics(string[] args)
        {
            List<string> letters = new List<string>();
            string inputFolder = null;
            string outputFolder = null;
            string proxiesFilename = null;
            int verbose = 0;
            int timeout = 3000;
            int maxRetry = 5;
            var p = new OptionSet() {
                { "v|verbose=", "verbose level. 0: nothing, 1: macro infos, 2: micro infos", (int v) => verbose = v },
                { "i|input=", "input folder containing titles/authors csv", folder => inputFolder = folder },
                { "o|output=", "output folder to store lyrics - one file per song", folder => outputFolder= folder },
                { "l|letters", $"songs starting letter, separated {Path.PathSeparator}.", l => letters.AddRange(l.Split(Path.PathSeparator)) },
                { "p|proxies=",  "ouptut file to write available proxies",  proxies => proxiesFilename = proxies },
                { "m|max-retry=", "set a limit number of retry to reach the website. Defaults to int.MaxValue", (int m) => maxRetry = m },
                { "t|timeout=", "max time in ms before request timeout (default is 3000)", (int t) => timeout = t }
            };

            if (Fill(args, p) == null)
                return;

            string errorOutputFolder = Path.Combine(outputFolder, "errors");
            if (!Directory.Exists(errorOutputFolder))
            {
                Directory.CreateDirectory(errorOutputFolder);
            }

            var proxyManager = ProxyManagerFactory.Build(proxiesFilename);
            var htmlExtractor = new HtmlExtractor(proxyManager);
            var lyricsExtractor = new LyricsExtractor();

            if (verbose > 0)
            {
                Console.WriteLine("starting lyrics extractions for songs starting with letters :\n" +
                    $"\t- {string.Join(" ,", letters)}");
            }

            Parallel.ForEach(Utils.GetTitlesFilenames(letters, inputFolder, verbose), filename =>
            {
                foreach (var line in Utils.ReadCsv(filename))
                {
                    var song = Utils.SongFilename(line[0]);

                    // reading HTML content
                    string content;
                    if (File.Exists(Path.Combine(outputFolder, song)))
                    {
                        continue;
                    }
                    else
                    {
                        content = htmlExtractor.Extract(line[0], maxRetry, timeout: timeout, verbose);
                        if (content == null)
                            continue;
                    }
                    try
                    {
                        var lyrics = lyricsExtractor.Extract(content);
                        File.WriteAllText(Path.Combine(outputFolder, song), lyrics);
                    }
                    catch
                    {
                        File.WriteAllText(Path.Combine(errorOutputFolder, song), content);
                    }
                }
            });
        }

        /// <summary>
        /// Main entrypoint for the dataset creation. This function takes a list of starting song letters
        /// as input that will be added to the final dataset
        /// </summary>
        static void CreateDatasetCsv(string[] args)
        {
            List<string> letters = new List<string>();
            string lyricsFolder = null;
            string titlesFolder = null;
            string outputFilename = null;
            int verbose = 0;
            var p = new OptionSet() {
                { "v|verbose=", "verbose level. 0: nothing, 1: macro infos, 2: micro infos", (int v) => verbose = v },
                { "p|lyrics=", "input folder containing lyrics", folder => lyricsFolder = folder },
                { "t|titles=", "input folder containing titles/authors csv", folder => titlesFolder = folder },
                { "o|output=", "output filename of the dataset", filename => outputFilename = filename },
                { "l|letters", $"songs starting letter, separated {Path.PathSeparator}.", l => letters.AddRange(l.Split(Path.PathSeparator)) },
            };

            if (Fill(args, p) == null)
                return;

            if (verbose > 0)
            {
                Console.WriteLine("starting dataset creation for songs starting with letters :\n" +
                    $"\t- {string.Join(" ,", letters)}");
            }

            var songCleaner = new SongTextCleaner();

            using (var writer = new StreamWriter(File.OpenWrite(outputFilename)))
            {
                writer.Write($"\"Id\";\"Text\";\"Title\";\"Author\"\n");

                foreach (var file in Utils.GetTitlesFilenames(letters, titlesFolder, verbose))
                {
                    foreach (var line in Utils.ReadCsv(file))
                    {
                        var song = Utils.SongFilename(line[0]);
                        var songFilename = Path.Combine(lyricsFolder, song);
                        if (File.Exists(songFilename))
                        {
                            var text = songCleaner.CleanSongText(songFilename);
                            if (!string.IsNullOrEmpty(text))
                                writer.Write($"\"{song}\";\"{text}\";\"{line[1]}\";\"{line[2]}\"\n");
                            if (verbose > 1)
                            {
                                Console.WriteLine($"adding {songFilename}: OK");
                            }
                        }
                        else if (verbose > 1)
                        {
                            Console.WriteLine($"adding {songFilename}: NOT FOUND");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Main entrypoint to evaluate the number of titles/authors csv that were extracted and compare it
        /// to the actual number on paroles.net
        /// </summary>
        static void CheckTitles(string[] args)
        {
            string inputFolder = null;
            string outputFilename = "";
            string proxiesFilename = null;
            bool writeErrors = false;
            int timeout = 3000;
            int verbose = 0;
            int maxRetry = int.MaxValue;
            var p = new OptionSet() {
                { "v|verbose=", "verbose level. 0: nothing, 1: macro infos, 2: micro infos", (int v) => verbose = v },
                { "i|input=", "input folder containing title/authors csv", folder => inputFolder = folder },
                { "o|output=", "output filename where report will be written", folder => outputFilename = folder },
                { "p|proxies=",  "ouptut file to write available proxies",  proxies => proxiesFilename = proxies },
                { "m|max-retry=", "set a limit number of retry to reach the website. Defaults to int.MaxValue", (int m) => maxRetry = m },
                { "e|write-errors", "write a report line for errors", v => writeErrors = v != null },
                { "t|timeout=", "max time in ms before request timeout (default is 3000)", (int t) => timeout = t }
            };

            if (Fill(args, p) == null)
                return;

            var letters = new[] {"a", "b", "c", "d", "e", "f", "g", "h", "i", "j", "k", "l",
                "m", "n", "o", "p", "q", "r", "s", "t", "u", "v", "w", "x", "y", "z", "0_9" };

            var proxyManager = ProxyManagerFactory.Build(proxiesFilename);
            var htmlExtractor = new HtmlExtractor(proxyManager);
            var songListExtractor = new SongListExtractor();

            var results = new ConcurrentDictionary<string, (int processed, int toProcess)>();
            Parallel.ForEach(letters, l =>
            {
                string address = $"https://www.paroles.net/paroles-{l}";
                var content = htmlExtractor.Extract(address, maxRetry: maxRetry, timeout: timeout, verbose);

                if (content != null)
                {
                    var count = Directory.EnumerateFiles(Path.Combine(inputFolder, l), "*.csv").Count();
                    var numberOfPages = Utils.GetPageNumber(content);
                    results[l] = (count, numberOfPages);
                    if (verbose > 0)
                        Console.WriteLine($"Letter {l}: DONE");
                }
                else
                {
                    if (verbose > 0)
                        Console.WriteLine($"Letter {l}: ERROR (can't extract content from {address})");
                }
            });

            using (var writer = new StreamWriter(File.OpenWrite(outputFilename)))
            {
                writer.Write($"# Authors/Titles Report, Date {DateTime.Now.ToLongDateString()}\n");
                writer.Write($"Starting letter\tpage processed\tpages to process");
                foreach (var l in letters)
                {
                    if (results.ContainsKey(l))
                    {
                        writer.Write($"{l}\t{results[l].processed}\t{results[l].toProcess}\n");
                    }
                    else
                    {
                        if (writeErrors)
                        {
                            var c = Directory.EnumerateFiles(Path.Combine(inputFolder, l), "*.csv").Count();
                            writer.Write($"{l}\t{c}\t*ERROR*\n");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Main entrypoint to evaluate the number of lyrics that were extracted and compare it to the 
        /// total number of lyrics to process in the author/titles csv that were created.
        /// Note that it might differ from the actual total number of lyrics as some titles might not have been
        /// extracted from the website yet.
        /// </summary>
        static void CheckLyrics(string[] args)
        {
            string lyricsFolder = null;
            string titlesFolder = null;
            string outputFilename = "";
            int verbose = 0;
            var p = new OptionSet() {
                { "v|verbose=", "verbose level. 0: nothing, 1: macro infos, 2: micro infos", (int v) => verbose = v },
                { "p|lyrics=", "input folder containing lyrics", folder => lyricsFolder = folder },
                { "t|titles=", "input folder containing titles/authors csv", folder => titlesFolder = folder },
                { "o|output=", "output filename where report will be written", folder => outputFilename = folder },
            };

            if (Fill(args, p) == null)
                return;

            var letters = new[] {"a", "b", "c", "d", "e", "f", "g", "h", "i", "j", "k", "l",
                "m", "n", "o", "p", "q", "r", "s", "t", "u", "v", "w", "x", "y", "z", "0_9" };

            var result = new ConcurrentDictionary<string, (int processed, int toProcess)>();
            Parallel.ForEach(letters, l =>
            {
                var processed = 0;
                var toProcess = 0;
                foreach (var file in Utils.GetTitlesFilenames(new List<string> { l }, titlesFolder, verbose))
                {
                    foreach (var line in Utils.ReadCsv(file))
                    {
                        toProcess++;
                        if (File.Exists(Path.Combine(lyricsFolder, Utils.SongFilename(line[0]))))
                            processed++;
                    }
                }
                result[l] = (processed, toProcess);
            });

            using (var writer = new StreamWriter(File.OpenWrite(outputFilename)))
            {
                writer.Write($"# Authors/Titles Report, Date {DateTime.Now.ToLongDateString()}\n");
                writer.Write($"Starting letter\tlyrics processed\tlyrics to process");
                foreach (var l in letters)
                {
                    writer.Write($"{l}\t{result[l].processed}\t{result[l].toProcess}\n");
                }
            }
        }
    }

    public static class Utils
    {
        public static IEnumerable<string> GetTitlesFilenames(List<string> letters, string folder, int verbose)
        {
            // if letters is empty, file it with all subfolder of given folder
            if (letters.Count == 0)
            {
                letters = Directory.EnumerateDirectories(folder).ToList();
            }
            foreach (var letter in letters)
            {
                var dir = Path.Combine(folder, letter);
                if (!Directory.Exists(dir))
                {
                    if (verbose > 0)
                        Console.WriteLine($"cannot find folder associated with letter {letter} ({dir})");
                    continue;
                }
                foreach (var file in Directory.EnumerateFiles(dir, "*.csv"))
                {
                    if (verbose > 1)
                        Console.WriteLine($"processing {file}");
                    yield return file;
                }
            }
        }

        public static string SongFilename(string url)
        {
            if (url.StartsWith("https"))
            {
                return url.Substring("https://www.paroles.net/".Length).Replace("/", "_") + ".txt";
            }
            else
            {
                return url.Substring("http://www.paroles.net/".Length).Replace("/", "_") + ".txt";
            }
        }

        public static IEnumerable<string[]> ReadCsv(string filename)
        {
            using (var csv = new CsvReader(File.OpenText(filename), culture: CultureInfo.InvariantCulture))
            {
                csv.Configuration.Delimiter = ";";
                csv.Read();
                csv.ReadHeader();
                while (csv.Read())
                {
                    yield return csv.Context.Record;
                }
            }
        }

        public static void Save(string filename, List<(string url, string title, string author)> songs)
        {
            using (var writer = new StreamWriter(File.OpenWrite(filename)))
            {
                writer.Write($"\"Url\";\"Title\";\"Author\"\n");
                foreach (var (url, title, author) in songs)
                {
                    writer.Write($"\"{url}\";\"{title.Replace('"', '\'')}\";\"{author.Replace('"', '\'')}\"\n");
                }
            }
        }

        public static int GetPageNumber(string htmlContent)
        {
            var pattern = "<a class=\"pager-letter\" href=\"https://www.paroles.net/paroles-.{1,3}-([0-9]+)\">";
            var matches = Regex.Matches(htmlContent, pattern);
            var maxi = 0;
            foreach (Match match in matches)
            {
                var pageNumber = int.Parse(match.Groups[1].Value);
                if (pageNumber > maxi)
                    maxi = pageNumber;
            }
            return maxi;
        }
    }
}
