namespace DsbNorge.A3Forms.Clients.InputValidator;

public interface IInputValidatorClient
{
    bool ValidatePhoneNumber(string inputValue);

    bool ValidateEmailAddress(string inputValue);
    
    bool ValidateOrgNumber(string inputValue);
    
    bool ValidatePostalCode(string inputValue);
    
    bool ValidateSsn(string inputValue);
}