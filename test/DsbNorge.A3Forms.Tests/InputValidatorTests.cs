using DsbNorge.A3Forms.Clients;

namespace DsbNorge.A3Forms.Tests;
public class InputValidatorTests
{
    private InputValidatorClient _inputValidatorClient { get; set; } = null!;


    [SetUp]
    public void Setup()
    {
        _inputValidatorClient = new InputValidatorClient();
    }

    [Test]
    public void PhoneNumber_should_be_valid()
    {
        var inputValue = "12345678";
        var result = _inputValidatorClient.ValidatePhoneNumber(inputValue);

        Assert.IsTrue(result);
    }

    [Test]
    public void PhoneNumber_should_be_invalid()
    {
        var inputValue = "9933";
        var result = _inputValidatorClient.ValidatePhoneNumber(inputValue);

        Assert.IsFalse(result);
    }

    [Test]
    public void EmailAddress_should_be_valid()
    {
        var inputValue = "test@mail.no";
        var result = _inputValidatorClient.ValidateEmailAddress(inputValue);

        Assert.IsTrue(result);
    }

    [Test]
    public void EmailAddress_should_be_invalid()
    {
        var inputValue = "testmail.no";
        var result = _inputValidatorClient.ValidateEmailAddress(inputValue);

        Assert.IsFalse(result);
    }

    [Test]
    public void Age_should_be_valid()
    {
        var inputValue = 119;
        var result = _inputValidatorClient.ValidateVictimAge(inputValue);

        Assert.IsTrue(result);
    }

    [Test]
    public void Age_should_be_invalid()
    {
        var inputValue = 121;
        var result = _inputValidatorClient.ValidateVictimAge(inputValue);

        Assert.IsFalse(result);
    }
}