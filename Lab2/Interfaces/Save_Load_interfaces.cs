using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lab2.Documentn;

namespace Lab2.Interfaces
{
    public interface IDocumentSaver
    {
        void Save(string path, Document document);
    }

    public interface IDocumentLoader
    {
        Document Load(string path);
    }
}
