namespace LibrarySystemWeb.API.Models
{
    // Bound from query: /api/library/users/most-active?from=...&to=...
    public class MostActiveUsersQuery
    {
        public string? From { get; set; }
        public string? To { get; set; }
    }
}