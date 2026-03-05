using AuthProject.Dtos;
using FluentValidation;

namespace AuthProject.Validators
{
    public class ResetPasswordDtoValidator : AbstractValidator<ResetPasswordDto>
    {
        public ResetPasswordDtoValidator() 
        {
            RuleFor(x => x.VerifyId)
                .NotEmpty().WithMessage("Doğrulama işlem numarası (VerifyId) zorunludur.");
            
            RuleFor(x => x.Code)
                .NotEmpty().WithMessage("Doğrulama kodu (Code) zorunludur.")
                .Length(6).WithMessage("Doğrulama kodu 6 karakter olmalıdır.");

            RuleFor(x => x.NewPassword)
                .NotEmpty().WithMessage("Yeni şifre alanı zorunludur.")
                .MinimumLength(6).WithMessage("Yeni şifre en az 6 karakter olmalıdır.");
        }
    }
}
