namespace LibrarySystem.Domain.Utils
{
    public static class BookHelpers
    {
        // Returns true if id is a power of two (1,2,4,8,...). Assumes id >= 1.
        public static bool IsPowerOfTwo(long id)
        {
            if (id <= 0) return false;
            // classic bit trick
            return (id & (id - 1)) == 0;
        }

        // 2) Reverse a book title
        public static string? ReverseTitle(string? title)
        {
            if (title is null) return null;
            // preserve whitespace and characters exactly, just reverse character order
            var arr = title.ToCharArray();
            Array.Reverse(arr);
            return new string(arr);
        }

        // 3) Generate title replicas
        public static string? RepeatTitle(string? title, int times)
        {
            if (title == null) return null;
            if (times <= 0) return string.Empty;
            // Efficient repeat using StringBuilder
            var builder = new System.Text.StringBuilder(title.Length * times);
            for (int i = 0; i < times; i++) builder.Append(title);
            return builder.ToString();
        }

        // 4) List odd-numbered book IDs between 0 and 100 (inclusive)
        public static IEnumerable<int> OddIds0To100()
        {
            for (int i = 1; i <= 100; i += 2)
                yield return i;
        }
    }
}
