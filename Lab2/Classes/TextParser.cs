using Lab2.Enums;
using Lab2.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lab2.Classes
{
    public class TextParser
    {
        public static List<ITextFragment> Parse(string text, DocumentType type)
        {
            var fragments = new List<ITextFragment>();
            var lines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            for (int i = 0; i < lines.Length; i++)
            {
                if (i > 0)
                {
                    fragments.Add(new NewlineFragment());
                }
                if (!string.IsNullOrEmpty(lines[i]))
                {
                    fragments.AddRange(ParseLine(lines[i], type));
                }
            }
            return fragments;
        }

        private static List<ITextFragment> ParseLine(string line, DocumentType type)
        {
            var fragments = new List<ITextFragment>();
            if (type == DocumentType.PlainText)
            {
                return new List<ITextFragment> { new PlainTextFragment(line) };
            }
            else
            {
                string remainingText = line;
                while (remainingText.Length > 0)
                {
                    if (remainingText.StartsWith("**") && remainingText.IndexOf("**", 2) > 2)
                    {
                        int end = remainingText.IndexOf("**", 2);
                        string content = remainingText.Substring(2, end - 2);
                        fragments.Add(new BoldDecorator(new PlainTextFragment(content)));
                        remainingText = remainingText.Substring(end + 2);
                    }
                    else if (remainingText.StartsWith("*") && remainingText.IndexOf("*", 1) > 1)
                    {
                        int end = remainingText.IndexOf("*", 1);
                        string content = remainingText.Substring(1, end - 1);
                        fragments.Add(new ItalicDecorator(new PlainTextFragment(content)));
                        remainingText = remainingText.Substring(end + 1);
                    }
                    else if (remainingText.StartsWith("__") && remainingText.IndexOf("__", 2) > 2)
                    {
                        int end = remainingText.IndexOf("__", 2);
                        string content = remainingText.Substring(2, end - 2);
                        fragments.Add(new UnderlineDecorator(new PlainTextFragment(content)));
                        remainingText = remainingText.Substring(end + 2);
                    }
                    else
                    {
                        fragments.Add(new PlainTextFragment(remainingText));
                        break;
                    }
                }
            }
            return fragments;
        }
    }

}
