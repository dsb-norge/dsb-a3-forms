using System.Net.Mail;
using System.Text.RegularExpressions;

namespace DsbNorge.A3Forms.Clients.InputValidator;

public partial class InputValidatorClient : IInputValidatorClient
{
    public bool ValidatePhoneNumber(string inputValue)
    {
        try
        {
            if (string.IsNullOrEmpty(inputValue))
                return true;

            if (!PhoneNumberRegex().IsMatch(inputValue))
                return false;
        }
        catch (FormatException)
        {
            return false;
        }

        return true;
    }

    public bool ValidateEmailAddress(string inputValue)
    {
        return string.IsNullOrEmpty(inputValue) || MailAddress.TryCreate(inputValue, out _);
    }
    
    public bool ValidatePostalCode(string inputValue)
    {
        return string.IsNullOrEmpty(inputValue) || RegexPostalCode().IsMatch(inputValue);
    }
    
    public bool ValidateOrgNumber(string inputValue)
    {
        if (string.IsNullOrEmpty(inputValue))
            return true;
        
        if (!RegexOrgNumber().IsMatch(inputValue))
            return false;
        
        if (inputValue.Distinct().Count() == 1)
            return false;
        
        return true;
    }

    public static bool ValidateVictimAge(int? inputValue)
    {
        return inputValue is not (> 120 or < 0);
    }
    
    public bool ValidateSsn(string inputValue)
    {
        if (inputValue.Length != 11)
        {
            return false;
        }
        
        if (inputValue.Distinct().Count() == 1)
            return false;

        return true;
    }

    [GeneratedRegex(@"^(\+[0-9]*)?\s?[0-9]{8,}$", RegexOptions.IgnoreCase, "nb-NO")]
    private static partial Regex PhoneNumberRegex();
    
    [GeneratedRegex(@"^[0-9]{4}$", RegexOptions.IgnoreCase, "nb-NO")]
    private static partial Regex RegexPostalCode();
    
    [GeneratedRegex(@"[0-9]{9}", RegexOptions.IgnoreCase, "nb-NO")]
    private static partial Regex RegexOrgNumber();
}