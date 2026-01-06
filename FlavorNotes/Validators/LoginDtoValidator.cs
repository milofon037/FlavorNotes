using FluentValidation;
using FlavorNotes.DTO;

namespace FlavorNotes.Validators;

public class LoginDtoValidator : AbstractValidator<LoginDto>
{
    public LoginDtoValidator()
    {
        RuleFor(x => x.Username).NotEmpty();
        RuleFor(x => x.Password).NotEmpty();
    }
}

