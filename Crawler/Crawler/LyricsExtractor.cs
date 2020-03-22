using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace Crawler
{
    public class LyricsExtractor
    {
        Regex _songTextStartRegex;
        public LyricsExtractor()
        {
            _songTextStartRegex = new Regex("<div class=\"song-text\">.*<!-- Share buttons div -->", RegexOptions.Singleline);
        }

        public string Extract(string content)
        {
            var lyricsHtmlContent = Cleaner.CleanHtml(ExtractLyricsHtml(content));

            var xml = new XmlDocument();
            xml.LoadXml(lyricsHtmlContent);

            return Cleaner.CleanLyrics(xml.ChildNodes[0].InnerText);
        }

        string ExtractLyricsHtml(string content)
        {
            var match = _songTextStartRegex.Match(content);
            var result = match.Value;
            if (string.IsNullOrEmpty(result))
            {
                throw new Exception("unable to find lyrics");
            }
            else
            {
                var index = result.LastIndexOf("</div>");
                result = result.Substring(0, index + 6);

                // removing inside scripts and comments
                result = Regex.Replace(result, "<br>", "", RegexOptions.Singleline);
                result = Regex.Replace(result, "<script[^<]*</script>", "", RegexOptions.Singleline);
                result = Regex.Replace(result, "<div[^<]*</div>", "", RegexOptions.Singleline);
                result = Regex.Replace(result, "<span[^<]*</span>", "", RegexOptions.Singleline);
                result = Regex.Replace(result, "<!--[^\n]*-->", "", RegexOptions.Singleline);

                return result;
            }
        }
    }
}
