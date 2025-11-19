namespace LibrarySystem.Domain.Models
{
    public class LendingRecord
    {
        public int Id { get; set; }
        public int BookId { get; set; }
        public int   UserId { get; set; }
        public DateTime BorrowedAt { get; set; }
        public DateTime? ReturnedAt { get; set; }
        public int PagesAtBorrow { get; set; }
    }
}
