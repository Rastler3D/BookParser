using System;
using System.IO.Compression;
using System.Xml.Linq;
using System.Linq;
using System.IO;
using System.Collections.Generic;


namespace BookReader
{
    public class Epub
    {
        private static XNamespace ns = "http://purl.org/dc/elements/1.1/";
        private static XNamespace xmlns = "http://www.idpf.org/2007/opf";
        private static XNamespace ct = "urn:oasis:names:tc:opendocument:xmlns:container";
        private static XNamespace htmlns = "http://www.w3.org/1999/xhtml";
        public static IBook Parse(Stream stream)
        {
            
            EpubBook Book;
            using (ZipArchive archive = new ZipArchive(stream))
            {
                string root;
                using (Stream MetaInfo = archive.GetEntry("META-INF/container.xml").Open())
                {
                    XDocument xml = XDocument.Load(MetaInfo);

                    root = xml.Descendants(ct + "rootfile").First().Attribute("full-path").Value;
                    
                }
                using (Stream BookInfo = archive.GetEntry(root).Open())
                {
                    XDocument xml = XDocument.Load(BookInfo);

                    XElement metadata = xml.Root.Element(xmlns + "metadata");

                    string directory = Path.GetDirectoryName(root) == String.Empty ? String.Empty : Path.GetDirectoryName(root) + '/';
                    var items = xml.Descendants(xmlns + "item");

                    Book = new EpubBook()
                    {
                        BookFormat = BookFormat.Epub,
                        Description = metadata.Element(ns + "description")?.Value,
                        Author = metadata.Element(ns + "creator")?.Value,
                        Title = metadata.Element(ns + "title")?.Value,
                        Genre = metadata.Elements(ns + "subject")?.Select(x => x.Value).ToList(),
                        Date = metadata.Element(ns + "date")?.Value,
                    };

                    var coverpageId = metadata.Elements().Where(x => x.Name.LocalName == "meta" && x.Attribute("name").Value == "cover").First()?.Attribute("content").Value;
                    String coverpageUri = (from item in items
                                           where item.Attribute("id").Value == coverpageId
                                           select item).First().Attribute("href").Value;


                    using (Stream coverpageImage = (archive.GetEntry(directory + coverpageUri).Open()))
                    {

                        MemoryStream memoryStream = new MemoryStream();
                        coverpageImage.CopyTo(memoryStream);
                        Book.Coverpage = "data:image/png;base64, " + Convert.ToBase64String((memoryStream).ToArray());
                    }

                    Book.Chapters = (from item in items
                                     where item.Attribute("media-type").Value == "application/xhtml+xml"
                                     select XElement.Load(archive.GetEntry(directory + item.Attribute("href").Value).Open()))
                                       .ToList();

                    Book.Resources = items.Where(item=> item.Attribute("media-type").Value != "application/xhtml+xml")
                                          .Select(item => {
                                              using (MemoryStream resourceMemoryStream = new MemoryStream())
                                              {
                                                  using (Stream resourceStream = archive.GetEntry(directory + item.Attribute("href").Value).Open())
                                                  {
                                                      resourceStream.CopyTo(resourceMemoryStream);
                                                      var str = $"data:{item.Attribute("media-type").Value};base64, {Convert.ToBase64String(resourceMemoryStream.ToArray())}";
                                                      return (item.Attribute("href").Value, str);
                                                  }
                                              }
                                          }).ToList();                
                                     
                }                   
                
            }
            return Book;
        }
        class EpubBook : IBook
        {
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
            public List<(string, string)> Resources { get; set; }

            public string ToHtml()
            {
                IBook convertedBook = this.MemberwiseClone() as IBook;
 
                convertedBook.Chapters = (from chapter in this.Chapters
                                          select new XElement(chapter.Element(htmlns + "body")))
                                         .ToList();
                (from images in convertedBook.Chapters?.Descendants().Attributes()
                 join binary in convertedBook.Resources
                 on images.Value equals binary.Item1
                 select new { binary, images })
                .ToList()
                .ForEach(x => x.images.Value = x.binary.Item2);

                convertedBook.Chapters.ForEach(chapter => chapter.Name = "section");


                var page = new TemplatePage();
                page.Model = convertedBook;
                return page.GenerateString();
            }
        }
    }
   
}