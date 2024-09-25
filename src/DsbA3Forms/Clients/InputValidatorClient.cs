using System.Net.Mail;

#nullable enable
namespace DsbA3Forms.Clients;

public class InputValidatorClient : IInputValidatorClient
{
    public bool ValidatePhoneNumber(string inputValue)
    {
        return string.IsNullOrEmpty(inputValue) ||
               inputValue.All<char>((Func<char, bool>)(c =>
                   char.IsDigit(c) || c == ' ' || c == '+' || c == '(' || c == ')'));
    }

    public bool ValidateEmailAddress(string inputValue)
    {
        if (string.IsNullOrEmpty(inputValue))
            return true;
        try
        {
            string address = new MailAddress(inputValue).Address;
            return true;
        }
        catch (FormatException ex)
        {
            return false;
        }
    }
}