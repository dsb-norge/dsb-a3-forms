using DsbA3Forms.Clients;

namespace DsbA3Forms.Tests;

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

    
}