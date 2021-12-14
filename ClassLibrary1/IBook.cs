using BookReader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace BookReader
{
    public enum BookFormat
    {
        Epub,
        Fb2
    }
    public interface IBook
    {
        Enum BookType { get; set; }
        string Title { get; set; }
        string Description { get; set; }
        string Author { get; set; }
        string Coverpage { get; set; }
        List<string> Genre { get; set; }
        string KeyWords { get; set; }
        string Date { get; set; }
        List<XElement> Chapters { get; set; }
        BookFormat BookFormat { get; set; }
        List<(string,string)> Resources { get; set; }

        string ToHtml();

    }
}
