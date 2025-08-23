using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace MedSys.Api.Validation;

public sealed class OibAttribute : ValidationAttribute
{
    public OibAttribute() => ErrorMessage = "OIB mora imati 11 znamenki i ispravan kontrolni broj.";

    public override bool IsValid(object? value)
    {
        var s = value as string;
        if (string.IsNullOrWhiteSpace(s)) return false;
        s = s.Trim();
        if (!Regex.IsMatch(s, @"^\d{11}$")) return false;

        // kontrola (mod 11,10)
        int a = 10;
        for (int i = 0; i < 10; i++)
        {
            int d = s[i] - '0';
            a = a + d;
            a %= 10;
            if (a == 0) a = 10;
            a = (a * 2) % 11;
        }
        int control = 11 - a;
        if (control == 10) control = 0;

        return control == (s[10] - '0');
    }
}
