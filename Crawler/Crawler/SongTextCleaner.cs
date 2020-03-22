using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace Crawler
{
    public class SongTextCleaner
    {
        public SongTextCleaner()
        {

        }

        public string CleanSongText(string filename)
        {
            var str = new StringBuilder();
            using (var reader = new StreamReader(File.OpenRead(filename)))
            {
                var line = reader.ReadLine();

                // missing lyrics
                if (line.StartsWith("Désolé nous n'avons pas encore"))
                {
                    return null;
                }

                // if first line starts with "Parole de la chanson", skip it
                if (!line.StartsWith("Paroles de la chanson"))
                {
                    str.Append(line);
                    str.Append("\n");
                }

                while ((line = reader.ReadLine()) != null)
                {
                    if (string.IsNullOrEmpty(line) || Regex.Match(line, @"^\(.*\)$").Success || Regex.Match(line, @"^\[.*\]$").Success)
                    {
                        continue;
                    }
                    str.Append(line.Replace('"', '\''));
                    str.Append("\n");
                }
            }
            return str.ToString();
        }

    }
}
