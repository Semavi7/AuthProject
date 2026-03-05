using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AuthProject.Filters
{
    public class FluentValidationFilter : IAsyncActionFilter
    {
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            foreach (var argument in context.ActionArguments.Values.Where(v => v != null))
            {
                var argumentType = argument.GetType();

                var validatorType = typeof(IValidator<>).MakeGenericType(argumentType);
                var validator = context.HttpContext.RequestServices.GetService(validatorType) as IValidator;

                if (validator != null)
                {
                    var validationContext = new ValidationContext<object>(argument);
                    var validationResult = await validator.ValidateAsync(validationContext);

                    if (!validationResult.IsValid)
                    {
            
                        context.Result = new BadRequestObjectResult(new
                        {
                            Message = "Validasyon hatası",
                            Errors = validationResult.Errors.Select(e => new
                            {
                                Field = e.PropertyName,
                                Error = e.ErrorMessage
                            })
                        });
                        return;
                    }
                }
            }

            await next();
        }
    }
}

