using System.Net.Mail;
using System.Text.RegularExpressions;

#nullable enable
namespace DsbA3Forms.Clients;

public class InputValidatorClient : IInputValidatorClient
{
    public bool ValidatePhoneNumber(string inputValue)
    {
        return string.IsNullOrEmpty(inputValue) ||
               !PhoneNumberRegex.Match(inputValue, @"^(\+[0-9]*)?\s?[0-9]{8,}$", RegexOptions.IgnoreCase).Success)
    }

    public bool ValidateEmailAddress(string inputValue)
    {
        if (string.IsNullOrEmpty(inputValue))
            return true;
        try
        {
            new MailAddress(inputValue);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    [GeneratedRegex(@"^(\+[0-9]*)?\s?[0-9]{8,}$", RegexOptions.IgnoreCase, "nb-NO")]
    private static partial Regex PhoneNumberRegex();
}