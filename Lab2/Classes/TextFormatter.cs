using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Lab2.Classes
{
    public class TextFormatter
    {
        public static string FormatText(string text, string docType)
        {
            if (docType == "Markdown" || docType == "RichText")
            {
                text = Regex.Replace(text, @"^# (.*)$", "\x1b[41m$1\x1b[0m", RegexOptions.Multiline);
                text = Regex.Replace(text, @"^## (.*)$", "\x1b[42m$1\x1b[0m", RegexOptions.Multiline);
                text = Regex.Replace(text, @"^### (.*)$", "\x1b[43m$1\x1b[0m", RegexOptions.Multiline);

                text = Regex.Replace(text, @"\*\*(.*?)\*\*", "\x1b[1m$1\x1b[0m");
                text = Regex.Replace(text, @"\*(.*?)\*", "\x1b[3m$1\x1b[0m");
                text = Regex.Replace(text, @"__(.*?)__", "\x1b[4m$1\x1b[0m");
            }
            return text;
        }
    }
}
