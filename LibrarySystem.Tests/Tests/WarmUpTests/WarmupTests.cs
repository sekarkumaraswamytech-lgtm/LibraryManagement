using LibrarySystem.Domain.Utils;

namespace LibrarySystem.Tests.Tests.WarmUpTests
{
    public class WarmupTests
    {
        // ----------------------------
        // 1. IsPowerOfTwo Tests
        // ----------------------------
        [Theory]
        [InlineData(1, true)]
        [InlineData(2, true)]
        [InlineData(3, false)]
        [InlineData(4, true)]
        [InlineData(16, true)]
        [InlineData(0, false)]
        [InlineData(-8, false)]
        public void IsPowerOfTwo_ShouldReturnExpected(long value, bool expected)
        {
            var result = BookHelpers.IsPowerOfTwo(value);
            Assert.Equal(expected, result);
        }

        // ----------------------------
        // 2. ReverseTitle Tests
        // ----------------------------
        [Fact]
        public void ReverseTitle_ShouldReverse()
        {
            string? result = BookHelpers.ReverseTitle("Moby Dick");
            Assert.Equal("kciD yboM", result);
        }

        [Fact]
        public void ReverseTitle_Empty_ReturnsEmpty()
        {
            Assert.Equal("", BookHelpers.ReverseTitle(""));
        }

        [Fact]
        public void ReverseTitle_Null_ReturnsNull()
        {
            Assert.Null(BookHelpers.ReverseTitle(null));
        }

        // ----------------------------
        // 3. RepeatTitle Tests
        // ----------------------------
        [Fact]
        public void RepeatTitle_ShouldRepeatCorrectly()
        {
            string? result = BookHelpers.RepeatTitle("Read", 3);
            Assert.Equal("ReadReadRead", result);
        }

        [Fact]
        public void RepeatTitle_ZeroTimes_ReturnsEmpty()
        {
            Assert.Equal("", BookHelpers.RepeatTitle("Book", 0));
        }

        [Fact]
        public void RepeatTitle_Null_ReturnsNull()
        {
            Assert.Null(BookHelpers.RepeatTitle(null, 5));
        }

        // ----------------------------
        // 4. Odd IDs Tests
        // ----------------------------
        [Fact]
        public void OddIds0To100_ShouldReturnOnlyOdds()
        {
            var values = BookHelpers.OddIds0To100().ToList();

            Assert.Equal(50, values.Count); // 1..99 = 50 numbers
            Assert.Equal(1, values.First());
            Assert.Equal(99, values.Last());
            Assert.All(values, v => Assert.True(v % 2 != 0));
        }
    }
}
