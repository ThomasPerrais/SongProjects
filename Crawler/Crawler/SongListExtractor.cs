using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace Crawler
{
    public class SongListExtractor
    {
        Regex _songListStartRegex;
        public SongListExtractor()
        {
            _songListStartRegex = new Regex("<div class=\"box\">\n\t+<h1>Chansons en +\".{1,3}\"</h1>.*<!-- // box -->", RegexOptions.Singleline);
        }

        string ExtractSongListHtml(string content)
        {
            var match = _songListStartRegex.Match(content);
            var result = match.Value;
            if (string.IsNullOrEmpty(result))
            {
                throw new Exception("unable to find song list");
            }
            else
            {
                var index = result.LastIndexOf("</div>");
                return result.Substring(0, index + 6);
            }
        }

        public List<(string url, string title, string author)> Extract(string content)
        {
            var songListHtmlContent = ExtractSongListHtml(content);
            songListHtmlContent = Cleaner.CleanHtml(songListHtmlContent);

            var xml = new XmlDocument();
            xml.LoadXml(songListHtmlContent);

            var result = new List<(string url, string title, string author)>();
            foreach (var index in GetTableIndex(xml.ChildNodes[0].ChildNodes))
            {
                var tableBody = xml.ChildNodes[0].ChildNodes[index].ChildNodes[0].ChildNodes[0];
                foreach (XmlNode child in tableBody.ChildNodes)
                {
                    string url;
                    try
                    {
                        url = child.ChildNodes[1].ChildNodes[0].ChildNodes[0].Attributes[0].Value;
                    }
                    catch
                    {
                        continue;
                    }

                    string title;
                    try
                    {
                        title = Cleaner.CleanText(child.ChildNodes[1].ChildNodes[0].InnerText);
                    }
                    catch
                    {
                        continue;
                    }
                    string author;
                    try
                    {
                        author = Cleaner.CleanText(child.ChildNodes[2].ChildNodes[0].InnerText);
                    }
                    catch
                    {
                        continue;
                    }

                    result.Add((url, title, author));
                }
            }
            return result;
        }

        static int[] GetTableIndex(XmlNodeList tableElements)
        {
            var sizes = new List<int>();
            foreach (XmlNode node in tableElements)
            {
                sizes.Add(node.InnerText.Length);
            }
            return sizes.Zip(Enumerable.Range(0, sizes.Count), (size, index) => (size, index))
                .OrderByDescending(t => t.size)
                .Select(t => t.index)
                .Take(2).ToArray();
        }

    }
}
