using System.Text.RegularExpressions;

namespace EVSwap.Mobile.Validators;

public static class Validators
{
    public static bool IsValidEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email)) return false;
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    public static bool IsValidPassword(string? password)
    {
        return !string.IsNullOrWhiteSpace(password) && password.Length >= 6;
    }

    public static bool IsValidPhone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone)) return false;
        return Regex.IsMatch(phone, @"^\+?[\d\s\-\(\)]{7,15}$");
    }

    public static bool IsValidAmount(decimal amount)
    {
        return amount > 0;
    }
}
