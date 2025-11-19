using FluentValidation;
using LibrarySystemWeb.API.Models;

namespace LibrarySystemWeb.API.Validation
{
    public class ReadingPaceRouteValidator : AbstractValidator<ReadingPaceRoute>
    {
        public ReadingPaceRouteValidator()
        {
            RuleFor(r => r.UserId).GreaterThan(0);
            RuleFor(r => r.BookId).GreaterThan(0);
        }
    }
}