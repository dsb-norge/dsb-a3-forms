using DsbNorge.A3Forms.Clients.InputValidator;

namespace DsbNorge.A3Forms.Tests;

[TestFixture]
public class InputValidatorClientTests
{
    private InputValidatorClient InputValidatorClient { get; set; } = null!;

    [SetUp]
    public void Setup()
    {
        InputValidatorClient = new InputValidatorClient();
    }

    [Test]
    public void PhoneNumber_should_be_valid()
    {
        const string inputValue = "12345678";
        var result = InputValidatorClient.ValidatePhoneNumber(inputValue);

        Assert.That(result, Is.True);
    }

    [Test]
    public void PhoneNumber_should_be_invalid()
    {
        const string inputValue = "9933";
        var result = InputValidatorClient.ValidatePhoneNumber(inputValue);

        Assert.That(result, Is.False);
    }

    [Test]
    public void EmailAddress_should_be_valid()
    {
        const string inputValue = "test@mail.no";
        var result = InputValidatorClient.ValidateEmailAddress(inputValue);

        Assert.That(result, Is.True);
    }

    [Test]
    public void EmailAddress_should_be_invalid()
    {
        const string inputValue = "testmail.no";
        var result = InputValidatorClient.ValidateEmailAddress(inputValue);

        Assert.That(result, Is.False);
    }

    [Test]
    public void Age_should_be_valid()
    {
        const int inputValue = 119;
        var result = InputValidatorClient.ValidateVictimAge(inputValue);

        Assert.That(result, Is.True);
    }

    [Test]
    public void Age_should_be_invalid()
    {
        const int inputValue = 121;
        var result = InputValidatorClient.ValidateVictimAge(inputValue);

        Assert.That(result, Is.False);
    }
    
    [Test]
    public void Age_should_be_invalid_when_negative()
    {
        const int inputValue = -1;
        var result = InputValidatorClient.ValidateVictimAge(inputValue);

        Assert.That(result, Is.False);
    }
    
    [Test]
    public void PostalCode_should_be_valid()
    {
        const string inputValue = "1234";
        var result = InputValidatorClient.ValidatePostalCode(inputValue);

        Assert.That(result, Is.True);
    }
    
    [Test]
    public void PostalCode_should_be_invalid()
    {
        const string inputValue = "123";
        var result = InputValidatorClient.ValidatePostalCode(inputValue);

        Assert.That(result, Is.False);
    }
    
    [Test]
    public void PostalCode_should_be_invalid_when_too_long()
    {
        const string inputValue = "11234";
        var result = InputValidatorClient.ValidatePostalCode(inputValue);

        Assert.That(result, Is.False);
    }
    
    [Test]
    public void OrgNumber_should_be_valid()
    {
        const string inputValue = "123456789";
        var result = InputValidatorClient.ValidateOrgNumber(inputValue);

        Assert.That(result, Is.True);
    }
    
    [Test]
    public void OrgNumber_should_be_invalid_when_too_short()
    {
        const string inputValue = "12345678";
        var result = InputValidatorClient.ValidateOrgNumber(inputValue);

        Assert.That(result, Is.False);
    }
    
    [Test]
    public void OrgNumber_should_be_invalid_when_all_digits_are_same()
    {
        const string inputValue = "111111111";
        var result = InputValidatorClient.ValidateOrgNumber(inputValue);

        Assert.That(result, Is.False);
    }
    
    [Test]
    public void Ssn_should_be_valid()
    {
        const string inputValue = "12345678901";
        var result = InputValidatorClient.ValidateSsn(inputValue);

        Assert.That(result, Is.True);
    }
    
    [Test]
    public void Ssn_should_be_invalid_when_too_short()
    {
        const string inputValue = "1234567890";
        var result = InputValidatorClient.ValidateSsn(inputValue);

        Assert.That(result, Is.False);
    }
    
    [Test]
    public void Ssn_should_be_invalid_when_all_digits_are_same()
    {
        const string inputValue = "11111111111";
        var result = InputValidatorClient.ValidateSsn(inputValue);

        Assert.That(result, Is.False);
    }
}