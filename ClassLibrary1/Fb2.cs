
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace BookReader
{
    public class Fb2
    {
        private static XNamespace fb = "http://www.gribuser.ru/xml/fictionbook/2.0";
        private static XNamespace l = "http://www.w3.org/1999/xlink";
        private static XNamespace htmlns = "http://www.w3.org/1999/xhtml";
        private static Dictionary<XName, XName> AccociatedTags = new Dictionary<XName, XName>()
            {
                {fb + "body","body" },
                {fb + "section","section" },
                {fb + "title","h1" },
                {fb + "emphasis","em" },
                {fb + "p","p" },
                {fb + "cite","cite" },
                {fb + "strong","strong" },
                {fb + "epigraph","blockquote" },
                {fb + "table","table" },
                {fb + "empty-line","br" },
                {fb + "a","a" },
                {fb + "poem","pre" },
                {fb + "text-author","cite" },
                {fb + "stanza","p" },
                {fb + "v","span" },
                {fb + "sup","sup" },
                {fb + "subtitle","p" },
                {fb + "image","img" },
            };

        public static IBook Parse(Stream stream)
        {
            XDocument xml = XDocument.Load(stream);
            XElement metadata = xml.Descendants(fb + "title-info").First();
            IBook book = new Fb2Book()
            {
                Author = metadata?.Element(fb + "author")?.Elements().Where(x => x.Name != fb + "id")?.Select(x => x.Value).Aggregate("", (x, y) => x + y + " "),
                Genre = metadata?.Elements(fb + "genre")?.Select(x => x.Value).ToList(),
                Title = metadata?.Element(fb + "book-title")?.Value,
                Description = metadata?.Element(fb + "annotation")?.Value,
                Date = metadata?.Element(fb + "date")?.Value,
                KeyWords = metadata?.Element(fb + "keywords")?.Value,

                Coverpage = "data:image/png;base64, " + (from binary in xml.Descendants(fb + "binary")
                                                         where '#' + binary?.Attribute("id")?.Value == metadata?.Descendants(fb + "image")?.First().FirstAttribute?.Value
                                                         select binary).First()?.Value,

                Chapters = xml?.Root.Element(fb + "body").Elements(fb + "section").ToList(),

                Resources = xml?.Root.Elements(fb + "binary")
                                      .Select(item=>(item.Attribute("id")?.Value, $"data:{item.Attribute("content-type").Value};base64, {item.Value}"))
                                      .ToList(),
                BookFormat = BookFormat.Fb2

            };
            return book;
        }
        class Fb2Book : IBook {
          

            public Enum BookType { get; set; }
            public string Title { get; set; }
            public string Description { get; set; }
            public string Author { get; set; }
            public string Coverpage { get; set; }
            public List<string> Genre { get; set; }
            public string KeyWords { get; set; }
            public string Date { get; set; }
            public List<XElement> Chapters { get; set; }
            public BookFormat BookFormat { get; set; }
            public List<(string,string)> Resources { get; set; }

            string IBook.ToHtml()
            {
                IBook convertedBook = this.MemberwiseClone() as IBook;
                convertedBook.Chapters = (from chapter in this.Chapters
                                          select new XElement(chapter))
                                         .ToList();
               
                 

                (from images in convertedBook.Chapters?.Descendants(fb + "image")
                 join binary in convertedBook.Resources
                 on images.Attribute(l + "href")?.Value equals '#' + binary.Item1
                 select new { binary, images })
                .ToList()
                .ForEach(x => x.images.ReplaceAttributes(new XAttribute("src",  x.binary.Item2)));

                convertedBook.Chapters.Descendants()
                                      .Select(x => x.Name = AccociatedTags[x.Name]);

                var page = new TemplatePage();
                page.Model = convertedBook;
                
                return page.GenerateString();
            }
        }

    }

}
