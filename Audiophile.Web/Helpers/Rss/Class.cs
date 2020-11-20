using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Audiophile.Web.Helpers.Rss
{
    public class Class
    {
    }
    public class AudiophileItem
    {
        public Author Author { get; set; }
        public string Image { get; set; }

        public string Body { get; set; }
        public ICollection<string> Categories { get; set; } = new List<string>();
        public Uri Comments { get; set; }
        public Uri Link { get; set; }
        /// <summary>Maps to 'guid' property on Feed.Serialize()</summary>
        public string Permalink { get; set; }
        /// <summary>Maps to 'pubDate' property on Feed.Serialize()</summary>
        public DateTime PublishDate { get; set; }
        public string Title { get; set; }
        public ICollection<Enclosure> Enclosures { get; set; } = new List<Enclosure>();
    }
    public class AudiophileFeed
    {
        public string Description { get; set; }
        public Uri Link { get; set; }
        public string Title { get; set; }
        public string Copyright { get; set; }

        public ICollection<AudiophileItem> Items { get; set; } = new List<AudiophileItem>();

        /// <summary>Produces well-formatted rss-compatible xml string.</summary>
        public string Serialize(bool isFeedNew = false)
        {
            var defaultOption = new SerializeOption()
            {
                Encoding = Encoding.UTF8,
            };
            return Serialize(defaultOption, isFeedNew);
        }

        public List<XAttribute> SetAttributes(bool isFeedNew)
        {
            var list = new List<XAttribute>
            {
                new XAttribute(XNamespace.Xmlns + "atom", "http://www.w3.org/2005/Atom"),
                new XAttribute("version", "2.0")
            };
            if (!isFeedNew) return list;
            list.Add(new XAttribute(XNamespace.Xmlns + "content", "http://purl.org/rss/1.0/modules/content"));
            list.Add(new XAttribute(XNamespace.Xmlns + "dc", "http://purl.org/dc/elements/1.1"));
            return list;

        }

        public string Serialize(SerializeOption option, bool isFeedNew)
        {
            XNamespace nsAtom = "http://www.w3.org/2005/Atom";
            XNamespace content = "http://purl.org/rss/1.0/modules/content";
            XNamespace dc = "http://purl.org/dc/elements/1.1";
            var doc = new XDocument(new XElement("rss"));
            if (doc.Root == null) return doc.ToStringWithDeclaration(option);
            doc.Root.Add(SetAttributes(isFeedNew));
            var channel = new XElement("channel");


            channel.Add(new XElement("title", Title));
            channel.Add(
                new XElement(nsAtom + "link",
                    new XAttribute("type", "application/rss+xml"),
                    new XAttribute("rel", "self"),
                    new XAttribute("href", Link.AbsoluteUri)
                ));
            if (isFeedNew)
            {
                channel.Add(
                    new XElement(dc + "link",
                        new XAttribute("href", Link.AbsoluteUri),
                        new XAttribute("rel", "self"),
                        new XAttribute("type", "application/rss+xml")
                    ));
                channel.Add(
                    new XElement(content + "link",
                        new XAttribute("href", Link.AbsoluteUri),
                        new XAttribute("rel", "self"),
                        new XAttribute("type", "application/rss+xml")
                    ));
            }
            if (Link != null) channel.Add(new XElement("link", Link.AbsoluteUri));
            channel.Add(new XElement("description", Description));
            // copyright is not a requirement
            if (!string.IsNullOrEmpty(Copyright)) channel.Add(new XElement("copyright", Copyright));

            doc.Root.Add(channel);

            foreach (var item in Items)
            {
                var itemElement = new XElement("item");
                itemElement.Add(new XElement("title", item.Title));
                if (item.Link != null) itemElement.Add(new XElement("link", item.Link.AbsoluteUri));
                //if (string.IsNullOrEmpty(item.Image) == false)
                //{
                //    item.Body = $"{item.Body}  <![CDATA[\r\n  Image inside RSS\r\n  <img src=\"{item.Image}\">         \r\n]> ";
                //}
                itemElement.Add(new XElement("description", item.Body));
                if (item.Author != null)
                    itemElement.Add(new XElement("author", $"{item.Author.Email} ({item.Author.Name})"));
                foreach (var c in item.Categories) itemElement.Add(new XElement("category", c));
                if (item.Comments != null) itemElement.Add(new XElement("comments", item.Comments.AbsoluteUri));
                if (!string.IsNullOrWhiteSpace(item.Permalink))
                    itemElement.Add(new XElement("guid", item.Permalink));
                var dateFmt = item.PublishDate.ToString("r");
                if (item.PublishDate != DateTime.MinValue) itemElement.Add(new XElement("pubDate", dateFmt));


                if (item.Enclosures != null && item.Enclosures.Any())
                {
                    foreach (var enclosure in item.Enclosures)
                    {
                        var enclosureElement = new XElement("enclosure");
                        foreach (var key in enclosure.Values.AllKeys)
                        {
                            enclosureElement.Add(new XAttribute(key, enclosure.Values[key]));
                        }

                        itemElement.Add(enclosureElement);
                    }
                }
                channel.Add(itemElement);
            }

            return doc.ToStringWithDeclaration(option);
        }
    }
    public class Enclosure
    {
        public Enclosure()
        {
            Values = new NameValueCollection();
        }

        public NameValueCollection Values { get; set; }
    }
    public class Author
    {
        public string Name { get; set; }
        public string Email { get; set; }
    }
    public static class XDocumentExtensions
    {
        public static string ToStringWithDeclaration(this XDocument what, SerializeOption option)
        {
            if (what == null)
                throw new ArgumentNullException(nameof(what));

            var builder = new StringBuilder();
            using (TextWriter writer = new RssStringWriter(builder, option))
                what.Save(writer);
            return builder.ToString();
        }
    }
    public class SerializeOption
    {
        public Encoding Encoding { get; set; } = Encoding.UTF8;
    }
    internal class RssStringWriter : StringWriter
    {
        private readonly SerializeOption _option;

        public RssStringWriter(StringBuilder sb, SerializeOption option) : base(sb)
        {
            _option = option;
        }

        public override Encoding Encoding => _option.Encoding;
    }

}
