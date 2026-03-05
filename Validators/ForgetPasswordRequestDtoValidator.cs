using AuthProject.Dtos;
using FluentValidation;

namespace AuthProject.Validators
{
    public class ForgetPasswordRequestDtoValidator : AbstractValidator<ForgetPasswordRequestDto>
    {
        public ForgetPasswordRequestDtoValidator()
        {
            RuleFor(x => x)
                .Must(dto => !string.IsNullOrEmpty(dto.Email) || !string.IsNullOrEmpty(dto.Phone))
                .WithMessage("Şifre sıfırlama kodu almak için Email veya Telefon numarası girmelisiniz.");
        }
    }
}
