using Lab2.Documentn;
using Lab2.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lab2.Classes
{
    public class CopyTextCommand : ICommand
    {
        private readonly Document _document;
        private readonly int _startIndex;
        private readonly int _count;

        public CopyTextCommand(Document document, int startIndex, int count)
        {
            _document = document;
            _startIndex = startIndex;
            _count = count;
        }

        public void Execute()
        {
            if (_startIndex < 0 || _startIndex >= _document._fragments.Count || _count < 0 || _startIndex + _count > _document._fragments.Count)
            {
                throw new ArgumentOutOfRangeException("Invalid start index or count.");
            }

            DocumentManager.Clipboard = _document._fragments.GetRange(_startIndex, _count);
        }

        public void Undo()
        {
        }
    }
    public class CutTextCommand : ICommand
    {
        private readonly Document _document;
        private readonly int _startIndex;
        private readonly int _count;
        private List<ITextFragment> _cutFragments;

        public CutTextCommand(Document document, int startIndex, int count)
        {
            _document = document;
            _startIndex = startIndex;
            _count = count;
        }

        public void Execute()
        {
            if (_startIndex < 0 || _startIndex >= _document._fragments.Count || _count < 0 || _startIndex + _count > _document._fragments.Count)
            {
                throw new ArgumentOutOfRangeException("Invalid start index or count.");
            }
            _document._history.AddEntry("CUT", _document.GetOriginalText());

            _cutFragments = _document._fragments.GetRange(_startIndex, _count);
            DocumentManager.Clipboard = _cutFragments;
            _document._fragments.RemoveRange(_startIndex, _count);
            _document.Notify("Text cut from document.");
        }

        public void Undo()
        {
            _document._history.AddEntry("UNDO_CUT", _document.GetOriginalText());

            _document._fragments.InsertRange(_startIndex, _cutFragments);
            _document.Notify("Cut operation undone.");
        }
    }
    public class PasteTextCommand : ICommand
    {
        private readonly Document _document;
        private readonly int _position;
        private List<ITextFragment> _pastedFragments;

        public PasteTextCommand(Document document, int position)
        {
            _document = document;
            _position = position;
        }

        public void Execute()
        {
            if (DocumentManager.Clipboard == null || DocumentManager.Clipboard.Count == 0)
            {
                throw new InvalidOperationException("Clipboard is empty.");
            }

            _pastedFragments = DocumentManager.Clipboard.ToList();

            if (_position < 0 || _position > _document._fragments.Count)
            {
                throw new ArgumentOutOfRangeException("Invalid paste position.");
            }
            _document._history.AddEntry("PASTE", _document.GetOriginalText());

            _document._fragments.InsertRange(_position, _pastedFragments);
            _document.Notify("Text pasted into document.");
        }

        public void Undo()
        {
            _document._history.AddEntry("UNDO_PASTE", _document.GetOriginalText());

            _document._fragments.RemoveRange(_position, _pastedFragments.Count);
            _document.Notify("Paste operation undone.");
        }
    }
}
