using System.ComponentModel.DataAnnotations;

namespace MedSys.Api.Validation;

public sealed class VisitTypeAttribute : ValidationAttribute
{
    private static readonly HashSet<string> Allowed = new(StringComparer.OrdinalIgnoreCase)
    { "GP","KRV","X-RAY","CT","MR","ULTRA","EKG","ECHO","EYE","DERM","DENTA","MAMMO","NEURO" };

    public VisitTypeAttribute() => ErrorMessage = "visitType mora biti jedan od: GP,KRV,X-RAY,CT,MR,ULTRA,EKG,ECHO,EYE,DERM,DENTA,MAMMO,NEURO.";
    public override bool IsValid(object? value)
        => value is string s && Allowed.Contains(s);
}
