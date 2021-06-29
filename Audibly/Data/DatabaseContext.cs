using Microsoft.EntityFrameworkCore;

namespace Audibly.Data
{
    public class DatabaseContext : DbContext
    {
        public DbSet<Book> Books { get; set; }
        public DbSet<BookMark> BookMarks { get; set; }
        public DbSet<Chapter> Chapters { get; set; }

        protected override void OnConfiguring(
            DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite(
                "Data Source=books.sqlite");
            base.OnConfiguring(optionsBuilder);
        }
    }
}
