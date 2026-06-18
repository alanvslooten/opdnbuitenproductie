using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace KinderKompas.Api.Validatie;

/// <summary>
/// Brug tussen FluentValidation en de standaard ASP.NET Core ProblemDetails-respons,
/// zodat we de (verouderde) FluentValidation.AspNetCore-integratie niet nodig hebben.
/// </summary>
public static class ValidatieUitbreidingen
{
    /// <summary>
    /// Valideert het model. Geeft <c>null</c> als het geldig is, anders een
    /// 400 ValidationProblem-resultaat dat de controller direct kan teruggeven:
    /// <c>if (await validator.ValideerAsync(invoer, this, ct) is { } fout) return fout;</c>
    /// </summary>
    public static async Task<ActionResult?> ValideerAsync<T>(
        this IValidator<T> validator,
        T model,
        ControllerBase controller,
        CancellationToken ct = default)
    {
        ValidationResult resultaat = await validator.ValidateAsync(model, ct);
        if (resultaat.IsValid)
        {
            return null;
        }

        var modelState = new ModelStateDictionary();
        foreach (ValidationFailure fout in resultaat.Errors)
        {
            modelState.AddModelError(fout.PropertyName, fout.ErrorMessage);
        }

        return controller.ValidationProblem(modelState);
    }
}
