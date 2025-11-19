using LibrarySystemWeb.API.Models;
using LibrarySystemWeb.API.Validation;

namespace LibrarySystem.Tests.Validation
{
    public class MostActiveUsersQueryValidatorTests
    {
        private readonly MostActiveUsersQueryValidator _validator = new();

        [Theory]
        [InlineData("2025-01-01", "2025-02-01")]
        [InlineData("01/01/2025", "02/01/2025")] // culture-friendly
        public void ValidInput_Passes(string from, string to)
        {
            var model = new MostActiveUsersQuery { From = from, To = to };

            var result = _validator.Validate(model);

            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public void MissingFrom_Fails()
        {
            var model = new MostActiveUsersQuery { From = null, To = "2025-01-02" };

            var result = _validator.Validate(model);

            result.IsValid.Should().BeFalse();
            result.Errors.Select(e => e.ErrorMessage).Should().Contain("'from' is required.");
        }

        [Fact]
        public void MissingTo_Fails()
        {
            var model = new MostActiveUsersQuery { From = "2025-01-01", To = null };

            var result = _validator.Validate(model);

            result.IsValid.Should().BeFalse();
            result.Errors.Select(e => e.ErrorMessage).Should().Contain("'to' is required.");
        }

        [Fact]
        public void InvalidFromFormat_Fails()
        {
            var model = new MostActiveUsersQuery { From = "not-a-date", To = "2025-01-02" };

            var result = _validator.Validate(model);

            result.IsValid.Should().BeFalse();
            result.Errors.Select(e => e.ErrorMessage).Should().Contain("'from' must be a valid ISO or culture date.");
        }

        [Fact]
        public void InvalidToFormat_Fails()
        {
            var model = new MostActiveUsersQuery { From = "2025-01-01", To = "###" };

            var result = _validator.Validate(model);

            result.IsValid.Should().BeFalse();
            result.Errors.Select(e => e.ErrorMessage).Should().Contain("'to' must be a valid ISO or culture date.");
        }

        [Fact]
        public void FromLaterThanTo_FailsRangeRule()
        {
            var model = new MostActiveUsersQuery { From = "2025-02-01", To = "2025-01-01" };

            var result = _validator.Validate(model);

            result.IsValid.Should().BeFalse();
            result.Errors.Select(e => e.ErrorMessage).Should().Contain("'from' must be earlier than 'to'.");
        }

        [Fact]
        public void EqualDates_FailsRangeRule()
        {
            var model = new MostActiveUsersQuery { From = "2025-01-01", To = "2025-01-01" };

            var result = _validator.Validate(model);

            result.IsValid.Should().BeFalse();
            result.Errors.Select(e => e.ErrorMessage).Should().Contain("'from' must be earlier than 'to'.");
        }
    }
}