using AuthProject.Dtos;
using FluentValidation;

namespace AuthProject.Validators
{
    public class RegisterLoginDtoValidator : AbstractValidator<RegisterLoginDto>
    {
        public RegisterLoginDtoValidator() 
        {
            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Şifre alanı zorunludur.")
                .MinimumLength(6).WithMessage("Şifre en az 6 karakter olmalıdır.");
            RuleFor(x => x.Email)
                .EmailAddress().WithMessage("Geçerli bir email adresi giriniz.")
                .When(x => !string.IsNullOrEmpty(x.Email));
            RuleFor(x => x)
                .Must(dto => !string.IsNullOrEmpty(dto.Email) || !string.IsNullOrEmpty(dto.Phone))
                .WithMessage("Kayıt olmak veya giriş yapmak için Email veya Telefon numarası girmelisiniz.");
        }
    }
}
