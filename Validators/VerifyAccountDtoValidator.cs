using AuthProject.Dtos;
using FluentValidation;

namespace AuthProject.Validators
{
    public class VerifyAccountDtoValidator : AbstractValidator<VerifyAccountDto>
    {
        public VerifyAccountDtoValidator()
        {
            RuleFor(x => x.Code)
                .NotEmpty().WithMessage("Doğrulama kodu zorunludur.")
                .Length(6).WithMessage("Doğrulama kodu 6 haneli olmalıdır.");

            RuleFor(x => x)
                .Must(dto => !string.IsNullOrEmpty(dto.Email) || !string.IsNullOrEmpty(dto.Phone))
                .WithMessage("Hesabı doğrulamak için Email veya Telefon numarası girmelisiniz.");
        }
    }
}
