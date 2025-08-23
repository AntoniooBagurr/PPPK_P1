using System.ComponentModel.DataAnnotations;

namespace MedSys.Api.Validation;

public sealed class PastDateAttribute : ValidationAttribute
{
    public PastDateAttribute() => ErrorMessage = "Datum mora biti u prošlosti.";
    public override bool IsValid(object? value)
    {
        if (value is DateTime dt) return dt.Date < DateTime.Today;
        return true; 
    }
}
