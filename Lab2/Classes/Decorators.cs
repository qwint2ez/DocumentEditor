using Lab2.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lab2.Classes
{
    public class PlainTextFragment : ITextFragment
    {
        private string _content;

        public PlainTextFragment(string content)
        {
            _content = content;
        }

        public string GetText()
        {
            return _content;
        }

        public string GetOriginalText()
        {
            return _content;
        }
    }

    public abstract class TextDecorator : ITextFragment
    {
        public ITextFragment _fragment;

        public TextDecorator(ITextFragment fragment)
        {
            _fragment = fragment;
        }

        public abstract string GetText();
        public abstract string GetOriginalText();
    }
    public class BoldDecorator : TextDecorator
    {
        public BoldDecorator(ITextFragment fragment) : base(fragment) { }

        public override string GetText()
        {
            return $"\x1b[1m{_fragment.GetText()}\x1b[0m"; // ANSI-код для жирного
        }

        public override string GetOriginalText()
        {
            return $"**{_fragment.GetOriginalText()}**"; // Оригинальные маркеры
        }
    }
    public class UnderlineDecorator : TextDecorator
    {
        public UnderlineDecorator(ITextFragment fragment) : base(fragment) { }

        public override string GetText()
        {
            return $"\x1b[4m{_fragment.GetText()}\x1b[0m"; // ANSI-код для подчёркивания
        }

        public override string GetOriginalText()
        {
            return $"__{_fragment.GetOriginalText()}__"; // Оригинальные маркеры
        }
    }

    public class ItalicDecorator : TextDecorator
    {
        public ItalicDecorator(ITextFragment fragment) : base(fragment) { }

        public override string GetText()
        {
            return $"\x1b[3m{_fragment.GetText()}\x1b[0m"; // ANSI code for italic
            //return $"/{_fragment.GetText()}/";
        }

        public override string GetOriginalText()
        {
            return $"*{_fragment.GetOriginalText()}*";
        }
    }

    public class NewlineFragment : ITextFragment
    {
        public string GetText() => "\n";
        public string GetOriginalText() => "\n";
    }
}
