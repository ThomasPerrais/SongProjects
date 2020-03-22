using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Crawler
{
    public static class ProxyManagerFactory
    {
        private static (string ip, int port) ParseLine(string line)
        {
            var split = line.Split(':');
            return (split[0], int.Parse(split[1]));
        }

        private static IEnumerable<(string ip, int port)> GetFromWebsite(string url, int verbose)
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    var receiveStream = response.GetResponseStream();
                    StreamReader readStream;

                    if (string.IsNullOrWhiteSpace(response.CharacterSet))
                        readStream = new StreamReader(receiveStream);
                    else
                        readStream = new StreamReader(receiveStream, Encoding.GetEncoding(response.CharacterSet));

                    string content = readStream.ReadToEnd();

                    response.Close();
                    readStream.Close();

                    // looking for proxies in the HTML
                    return ExtractProxiesFromHtml(content);
                }
                if (verbose > 0)
                {
                    Console.WriteLine($"status code not OK for source {url}, skipping...");
                }
                return Enumerable.Empty<(string ip, int port)>();
            }
            catch
            {
                if (verbose > 0)
                {
                    Console.WriteLine($"error when downloading html content from {url}, skipping...");
                }
                return Enumerable.Empty<(string ip, int port)>();
            }
        }

        private static IEnumerable<(string ip, int port)> ExtractProxiesFromHtml(string htmlContent)
        {
            var regex = new Regex(@"((\d{1,3}\.){3}\d{1,3}).{1,10}(\d{2,5})");
            foreach (Match match in regex.Matches(htmlContent))
            {
                yield return (match.Groups[1].Value, int.Parse(match.Groups[3].Value));
            }
        }

        private static IEnumerable<(string ip, int port)> GetFromFile(string file, int verbose)
        {
            if (File.Exists(file))
            {
                return File.ReadLines(file).Select(line => ParseLine(line));
            }
            else
            {
                if (verbose > 0)
                {
                    Console.WriteLine($"file {file} wasn't found, skipping it");
                }
                return Enumerable.Empty<(string ip, int port)>();
            }
        }

        public static void FindProxies(string filename, string[] files,
            string[] urls, string testUrl, int verbose, int retry)
        {
            var proxies = new List<(string ip, int port)>();

            // Raw proxies from files
            foreach (var file in files)
            {
                proxies.AddRange(GetFromFile(file, verbose));
            }

            // scrap proxies from website
            foreach (var url in urls)
            {
                proxies.AddRange(GetFromWebsite(url, verbose));
            }

            var availableProxies = TestProxies(proxies, testUrl, verbose, retry);
            File.WriteAllLines(filename, availableProxies.Select(p => $"{p.ip}:{p.port}"));
        }

        private static IEnumerable<(string ip, int port)> TestProxies(IEnumerable<(string ip, int port)> proxies,
            string testUrl, int verbose, int retry)
        {
            var result = new ConcurrentDictionary<(string ip, int port), bool>();
            Parallel.ForEach(proxies, proxy => 
            {
                result[proxy] = TestProxy(proxy, testUrl, verbose);
            });
            int pass = 0;
            while (pass < retry)
            {
                pass++;
                if (verbose > 0)
                {
                    var count = result.Where(kvp => kvp.Value).Count();
                    Console.WriteLine($"found {count} available proxies... retrying for unavailable proxies");
                }
                Parallel.ForEach(result.Where(kvp => !kvp.Value).Select(kvp => kvp.Key), proxy =>
                {
                    result[proxy] = TestProxy(proxy, testUrl, verbose);
                });
            }
            if (verbose > 0)
            {
                var count = result.Where(kvp => kvp.Value).Count();
                Console.WriteLine($"found {count} available proxies");
            }
            return result.Where(kvp => kvp.Value).Select(kvp => kvp.Key);
        }

        private static bool TestProxy((string ip, int port) proxy, string testUrl, int verbose)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(testUrl);
            request.Proxy = new WebProxy(proxy.ip, proxy.port);
            try
            {
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    if (verbose > 0) // always write available proxies ?
                    {
                        Console.WriteLine($"proxy {proxy.ip}:{proxy.port} => available");
                    }
                    return true;
                }
                if (verbose > 1)
                {
                    Console.WriteLine($"proxy {proxy.ip}:{proxy.port} => unavailable");
                }
                return false;
            }
            catch
            {
                if (verbose > 1)
                {
                    Console.WriteLine($"proxy {proxy.ip}:{proxy.port} => unavailable");
                }
                return false;
            }
        }

        public static ProxyManager Build(string filename)
        {
            return new ProxyManager(File.ReadLines(filename).Select(line => ParseLine(line)));
        }
    }
}
