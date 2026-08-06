using FluentValidation;
using MediatR;

namespace Identity.Common.Validation;

/// <summary>
/// MediatR pipeline behavior that runs the FluentValidation validators registered
/// for the request before it reaches the handler.
/// </summary>
/// <remarks>
/// The constraint is <c>notnull</c> rather than <c>IRequest&lt;TResponse&gt;</c>: a command
/// declared as the non-generic <c>IRequest</c> is dispatched with <c>Unit</c> as its
/// response, and <c>IRequest</c> does not derive from <c>IRequest&lt;Unit&gt;</c>. Under the
/// tighter constraint the container silently skips this behavior for such commands — they
/// would reach their handler with no validation at all.
/// </remarks>
public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (_validators.Any())
        {
            var context = new ValidationContext<TRequest>(request);

            var validationResults = await Task.WhenAll(
                _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

            var failures = validationResults
                .SelectMany(r => r.Errors)
                .Where(f => f != null)
                .ToList();

            if (failures.Count != 0)
            {
                throw new ValidationException(failures);
            }
        }

        return await next();
    }
}
