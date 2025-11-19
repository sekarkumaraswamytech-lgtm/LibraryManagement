using FluentValidation;
using LibrarySystemWeb.API.Models;

namespace LibrarySystemWeb.API.Validation
{
    public class RelatedBooksRouteValidator : AbstractValidator<RelatedBooksRoute>
    {
        public RelatedBooksRouteValidator()
        {
            RuleFor(r => r.BookId).GreaterThan(0);
        }
    }
}