using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Crawler
{
    public static class Cleaner
    {
        public static string CleanLyrics(string lyrics)
        {
            lyrics = CleanText(lyrics);
            lyrics = Regex.Replace(lyrics, "[\t ]+", " ");
            lyrics = Regex.Replace(lyrics, "\n +", "\n");
            lyrics = Regex.Replace(lyrics, " +\n", "\n");
            lyrics = Regex.Replace(lyrics, "\n+", "\n");

            return lyrics;
        }

        public static string CleanText(string text)
        {
            return text.Trim(new[] { '\n', '\t', ' ' });
        }

        public static string CleanHtml(string songListHtml)
        {
            songListHtml = Regex.Replace(songListHtml, "&nbsp;", "&#160;");
            return songListHtml;
        }
    }
}
