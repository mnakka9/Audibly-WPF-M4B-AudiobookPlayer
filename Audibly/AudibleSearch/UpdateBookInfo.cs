using System.Linq;
using System.Threading.Tasks;

using AudibleSearch;

using Audibly.Data;

namespace Audibly.AudibleSearch
{
    public static class UpdateBookInfo
    {
        public static async Task UpdateBookInfoFromAudible(Book book)
        {
            if (book.Title.IsNullOrEmpty())
            {
                return;
            }

            var audiobook = await SearchAudible.SearchAudioBookInAudible(book.Title!, book.Author, false);

            if (audiobook != null)
            {
                book.ImagePath = audiobook.ImagePath;
                book.Description = audiobook.Description;
                book.Title = audiobook.Title;
                book.Narrator = audiobook.Narrator;
                book.Author = audiobook.Author;
            }

            DatabaseContext context = new();
            if (!context.Books.Any(b => b.Path == book.Path))
            {
                context.Books.Add(book);
                context.SaveChanges();
            }
            else
            {
                context.Books.Update(book);
                context.SaveChanges();
            }
        }

        public static bool IsNullOrEmpty(this string? value)
        {
            var isNullOrEmpty = string.IsNullOrEmpty(value);

            if (!isNullOrEmpty)
            {
                isNullOrEmpty = string.IsNullOrWhiteSpace(value);
            }

            return isNullOrEmpty;
        }
    }
}
