using SagraFacile.Application.Features.Reservations;

namespace SagraFacile.Application.Tests.Features.Reservations;

public class CreateReservationValidatorTests
{
    private readonly CreateReservation.Validator _validator = new();

    [Fact]
    public void Validator_ValidCommand_Passes()
    {
        var command = new CreateReservation.Command("Mario Rossi", 4);
        var result = _validator.Validate(command);
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validator_EmptyCustomerName_Fails()
    {
        var command = new CreateReservation.Command("", 4);
        var result = _validator.Validate(command);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateReservation.Command.CustomerName));
    }

    [Fact]
    public void Validator_CustomerNameTooLong_Fails()
    {
        var longName = new string('x', 201);
        var command = new CreateReservation.Command(longName, 4);
        var result = _validator.Validate(command);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateReservation.Command.CustomerName));
    }

    [Fact]
    public void Validator_ZeroPartySize_Fails()
    {
        var command = new CreateReservation.Command("Mario", 0);
        var result = _validator.Validate(command);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateReservation.Command.PartySize));
    }

    [Fact]
    public void Validator_NegativePartySize_Fails()
    {
        var command = new CreateReservation.Command("Mario", -1);
        var result = _validator.Validate(command);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateReservation.Command.PartySize));
    }

    [Fact]
    public void Validator_PartySizeExceeds50_Fails()
    {
        var command = new CreateReservation.Command("Mario", 51);
        var result = _validator.Validate(command);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateReservation.Command.PartySize));
    }

    [Fact]
    public void Validator_NotesTooLong_Fails()
    {
        var longNotes = new string('x', 501);
        var command = new CreateReservation.Command("Mario", 4, longNotes);
        var result = _validator.Validate(command);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateReservation.Command.Notes));
    }

    [Fact]
    public void Validator_MaxValidPartySize_Passes()
    {
        var command = new CreateReservation.Command("Mario", 50);
        var result = _validator.Validate(command);
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validator_WithNotes_Passes()
    {
        var command = new CreateReservation.Command("Mario", 4, "Birthday celebration");
        var result = _validator.Validate(command);
        Assert.True(result.IsValid);
    }
}
