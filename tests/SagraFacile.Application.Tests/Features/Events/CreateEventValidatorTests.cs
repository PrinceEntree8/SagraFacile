using SagraFacile.Application.Features.Events;

namespace SagraFacile.Application.Tests.Features.Events;

public class CreateEventValidatorTests
{
    private readonly CreateEvent.Validator _validator = new();

    [Fact]
    public void Validator_ValidCommand_Passes()
    {
        var command = new CreateEvent.Command("Sagra 2026", "Desc", DateTime.UtcNow, "EUR", "€");
        var result = _validator.Validate(command);
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validator_EmptyName_Fails()
    {
        var command = new CreateEvent.Command("", "Desc", DateTime.UtcNow, "EUR", "€");
        var result = _validator.Validate(command);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateEvent.Command.Name));
    }

    [Fact]
    public void Validator_NameTooLong_Fails()
    {
        var longName = new string('x', 201);
        var command = new CreateEvent.Command(longName, "Desc", DateTime.UtcNow, "EUR", "€");
        var result = _validator.Validate(command);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateEvent.Command.Name));
    }

    [Fact]
    public void Validator_EmptyCurrency_Fails()
    {
        var command = new CreateEvent.Command("Name", "Desc", DateTime.UtcNow, "", "€");
        var result = _validator.Validate(command);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateEvent.Command.Currency));
    }

    [Fact]
    public void Validator_EmptyCurrencySymbol_Fails()
    {
        var command = new CreateEvent.Command("Name", "Desc", DateTime.UtcNow, "EUR", "");
        var result = _validator.Validate(command);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateEvent.Command.CurrencySymbol));
    }
}
