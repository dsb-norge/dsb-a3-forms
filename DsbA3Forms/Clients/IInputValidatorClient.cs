#nullable enable
namespace DsbA3Forms.Clients
{
    public interface IInputValidatorClient
    {
        bool ValidatePhoneNumber(string inputValue);

        bool ValidateEmailAddress(string inputValue);
    }
}
