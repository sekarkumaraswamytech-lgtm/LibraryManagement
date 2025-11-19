using FluentValidation;
using LibrarySystemWeb.API.Models;

namespace LibrarySystemWeb.API.Validation
{
    public class MostActiveUsersQueryValidator : AbstractValidator<MostActiveUsersQuery>
    {
        public MostActiveUsersQueryValidator()
        {
            RuleFor(q => q.From)
                .NotEmpty().WithMessage("'from' is required.")
                .Must(BeValidDate).WithMessage("'from' must be a valid ISO or culture date.");

            RuleFor(q => q.To)
                .NotEmpty().WithMessage("'to' is required.")
                .Must(BeValidDate).WithMessage("'to' must be a valid ISO or culture date.");

            RuleFor(q => q)
                .Must(q => BeValidRange(q.From, q.To))
                .WithMessage("'from' must be earlier than 'to'.")
                .When(q => BeValidDate(q.From!) && BeValidDate(q.To!));
        }

        private static bool BeValidDate(string? value) =>
            !string.IsNullOrWhiteSpace(value) && DateTime.TryParse(value, out _);

        private static bool BeValidRange(string? from, string? to)
        {
            if (!DateTime.TryParse(from, out var f)) return false;
            if (!DateTime.TryParse(to, out var t)) return false;
            return f < t;
        }
    }
}