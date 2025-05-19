using System;
using System.Collections.Generic;
using Lab2.Documentn;
using Lab2.Enums;
using Lab2.Interfaces;

namespace Lab2.Classes
{
    public class AppendTextCommand : ICommand
    {
        private readonly Document _document;
        private readonly string _text;
        private List<ITextFragment> _previousFragments;

        public AppendTextCommand(Document document, string text)
        {
            _document = document ?? throw new ArgumentNullException(nameof(document));
            _text = text;
            _previousFragments = new List<ITextFragment>(_document._fragments);
        }

        public void Execute()
        {
            if (_document.GetUserPermission(Session.CurrentUser) < UserRole.Editor)
                throw new InvalidOperationException("You don't have permission to edit.");
            _document.AppendText(_text);
        }

        public void Undo()
        {
            _document._fragments.Clear();
            _document._fragments.AddRange(_previousFragments);
        }
    }

    public class InsertTextCommand : ICommand
    {
        private readonly Document _document;
        private readonly int _charPosition;
        private readonly string _text;
        private List<ITextFragment> _previousFragments;

        public InsertTextCommand(Document document, int charPosition, string text)
        {
            _document = document;
            _charPosition = charPosition;
            _text = text;
            _previousFragments = new List<ITextFragment>(_document._fragments);
        }

        public void Execute()
        {
            if (_document.GetUserPermission(Session.CurrentUser) < UserRole.Editor)
                throw new InvalidOperationException("You don't have permission to edit.");
            _document.InsertText(_charPosition, _text);
        }

        public void Undo()
        {
            _document._fragments.Clear();
            _document._fragments.AddRange(_previousFragments);
        }
    }

    public class DeleteTextCommand : ICommand
    {
        private readonly Document _document;
        private readonly int _fragmentStart;
        private readonly int _fragmentCount;
        private List<ITextFragment> _previousFragments;

        public DeleteTextCommand(Document document, int fragmentStart, int fragmentCount)
        {
            _document = document;
            _fragmentStart = fragmentStart;
            _fragmentCount = fragmentCount;
            _previousFragments = new List<ITextFragment>(_document._fragments);
        }

        public void Execute()
        {
            if (_document.GetUserPermission(Session.CurrentUser) < UserRole.Editor)
                throw new InvalidOperationException("You don't have permission to edit.");
            _document.DeleteText(_fragmentStart, _fragmentCount);
        }

        public void Undo()
        {
            _document._fragments.Clear();
            _document._fragments.AddRange(_previousFragments);
        }
    }
}