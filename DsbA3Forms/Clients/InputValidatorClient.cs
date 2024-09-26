#nullable enable
using System.Net.Mail;
using System.Text.RegularExpressions;

namespace DsbA3Forms.Clients
{
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
}