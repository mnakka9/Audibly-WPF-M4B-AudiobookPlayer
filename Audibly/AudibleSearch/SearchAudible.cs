
using System.Linq;
using System.Threading.Tasks;

using Audibly.Data;

using HtmlAgilityPack;

namespace AudibleSearch
{
    public class SearchAudible
    {
        public static async Task<Book?> SearchAudioBookInAudible (string title,
                                                                  string? author,
                                                                  bool searchByKeyword = false)
        {
            var web = new HtmlWeb();

            string url = string.Empty;
            if (searchByKeyword || string.IsNullOrEmpty(author))
            {
                url = string.Format(Constants.SearchUrl, title, string.Empty, string.Empty, string.Empty, string.Empty);
            }
            else
            {
                url = string.Format(Constants.SearchUrl, string.Empty, title, author, string.Empty, string.Empty);
            }

            var doc = await web.LoadFromWebAsync(url);

            var nodes = doc.DocumentNode.Descendants(0).Where(n => n.HasClass(Constants.AuthorClass));

            if (!nodes.Any()) return await SearchAudioBookInAudible(title, null, true);

            foreach (var node in nodes)
            {
                var headingNode = node.ParentNode;

                if (headingNode != null)
                {
                    var heading = headingNode.Descendants().Where(n => n.HasClass(Constants.HeadingClass)).FirstOrDefault();
                    bool checkAuthor = (node.InnerText?.Contains(author ?? string.Empty, System.StringComparison.OrdinalIgnoreCase) ?? false) || searchByKeyword;
                    bool checkTitle = title.Contains(heading?.InnerText?.Trim(), System.StringComparison.OrdinalIgnoreCase) || (heading?.InnerText?.Contains(title, System.StringComparison.OrdinalIgnoreCase) ?? false);
                    if (checkTitle && checkAuthor)
                    {
                        var aNode = headingNode?.Descendants().Where(n => n.HasClass(Constants.UrlClass))?.FirstOrDefault()?.Attributes["href"]?.Value;
                        if (aNode != null)
                        {
                            var book = GetAudioBookDetails(aNode);

                            if (book != null)
                            {
                                return book;
                            }
                            else
                            {
                                return null;
                            }
                        }
                    }
                }

            }
            return null;

        }

        private static Book? GetAudioBookDetails (string aNode)
        {
            Book audiobook = new();

            var web = new HtmlWeb();
            var url = string.Format(Constants.BookUrl, aNode);
            var doc = web.Load(url);

            var authorNode = doc.DocumentNode.Descendants(0).Where(n => n.HasClass(Constants.AuthorClass))?.FirstOrDefault();

            if (authorNode == null) return null;

            audiobook.Author = authorNode.SelectSingleNode(".//a")?.InnerText;

            var parentNode = authorNode.ParentNode;
            var headingNode = parentNode?.Descendants().Where(n => n.HasClass(Constants.HeadingClass))?.FirstOrDefault();

            audiobook.Title = headingNode?.InnerText;
            audiobook.Series = parentNode?.Descendants().Where(n => n.HasClass(Constants.SeriesClass))
                                        ?.FirstOrDefault()?.SelectSingleNode(".//a")?.InnerText;

            var imageNode = doc.DocumentNode.Descendants().Where(n => n.HasClass("hero-content"));
            var imageElement = imageNode?.Where(n => n.InnerHtml?.Contains("img") ?? false).FirstOrDefault()?.SelectSingleNode(".//img");
            audiobook.ImagePath = imageElement?.Attributes["src"]?.Value;

            audiobook.Narrator = parentNode?.Descendants().Where(n => n.HasClass("narratorLabel"))
                                        ?.FirstOrDefault()?.SelectSingleNode(".//a")?.InnerText;

            var desNode = doc.DocumentNode.Descendants().Where(n => n.HasClass(Constants.PublisherSummary))?.FirstOrDefault();
            audiobook.Description = desNode?.Descendants()?.Where(n => n.HasClass("bc-text"))?.FirstOrDefault()?.InnerText;

            return audiobook;
        }
    }
}