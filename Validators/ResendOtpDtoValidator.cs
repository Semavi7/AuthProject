using AuthProject.Dtos;
using FluentValidation;

namespace AuthProject.Validators
{
    public class ResendOtpDtoValidator : AbstractValidator<ResendOtpDto>
    {
        public ResendOtpDtoValidator()
        {
            RuleFor(x => x)
                .Must(dto => !string.IsNullOrEmpty(dto.Email) || !string.IsNullOrEmpty(dto.Phone))
                .WithMessage("Yeni kod istemek için Email veya Telefon numarası girmelisiniz.");
        }
    }
}
