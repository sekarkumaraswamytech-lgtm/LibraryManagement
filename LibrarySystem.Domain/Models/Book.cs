using System.ComponentModel.DataAnnotations;

namespace LibrarySystem.Domain.Models
{
    public class Book
    {
        public int Id { get; set; }
        public int BookId { get; set; }            // External/business identifier
        public string Title { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public int Pages { get; set; }
        public int TotalCopies { get; set; }
        public int AvailableCopies { get; set; }

        // Optimistic concurrency token – EF Core will include this in WHERE clause for updates.
        [Timestamp]
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }
}
