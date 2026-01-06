using FluentValidation;
using FlavorNotes.DTO;

namespace FlavorNotes.Validators;

public class CreateRecipeDtoValidator : AbstractValidator<CreateRecipeDto>
{
    public CreateRecipeDtoValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(255);
        RuleFor(x => x.Description).NotEmpty();
        RuleFor(x => x.CategoryId).GreaterThan(0);
        RuleFor(x => x.PrepTimeMinutes).GreaterThanOrEqualTo(0);
        RuleFor(x => x.CookTimeMinutes).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Servings).GreaterThan(0);
        RuleForEach(x => x.Ingredients).SetValidator(new CreateRecipeIngredientDtoValidator());
        RuleForEach(x => x.Instructions).SetValidator(new CreateInstructionStepDtoValidator());
    }
}

public class CreateRecipeIngredientDtoValidator : AbstractValidator<CreateRecipeIngredientDto>
{
    public CreateRecipeIngredientDtoValidator()
    {
        RuleFor(x => x.IngredientId).GreaterThan(0);
        RuleFor(x => x.UnitId).GreaterThan(0);
        RuleFor(x => x.Quantity).GreaterThan(0);
    }
}

public class CreateInstructionStepDtoValidator : AbstractValidator<CreateInstructionStepDto>
{
    public CreateInstructionStepDtoValidator()
    {
        RuleFor(x => x.StepNumber).GreaterThan(0);
        RuleFor(x => x.InstructionText).NotEmpty();
    }
}

