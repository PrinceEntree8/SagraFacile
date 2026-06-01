using SagraFacile.Domain.Features.Events;

namespace SagraFacile.Application.Tests.Features.Events;

public class EventAdditionalOptionsDefaultsTests
{
    [Fact]
    public void EventAdditionalOptions_DefaultInstance_HasSafeDefaults()
    {
        var opts = new EventAdditionalOptions();

        Assert.NotNull(opts.Reservations);
    }

    [Fact]
    public void ReservationOptions_DefaultInstance_HasSafeDefaults()
    {
        var opts = new ReservationOptions();

        Assert.NotNull(opts.PartyCompletion);
    }

    [Fact]
    public void PartyCompletionOptions_DefaultInstance_IsDisabled()
    {
        var opts = new PartyCompletionOptions();

        Assert.False(opts.Enabled);
    }

    [Fact]
    public void PartyCompletionOptions_DefaultInstance_HasPositiveMinPartySize()
    {
        var opts = new PartyCompletionOptions();

        Assert.True(opts.MinPartySize >= 1);
    }

    [Fact]
    public void Event_DefaultInstance_HasSafeAdditionalOptions()
    {
        var ev = new Event();

        Assert.NotNull(ev.AdditionalOptions);
        Assert.NotNull(ev.AdditionalOptions.Reservations);
        Assert.NotNull(ev.AdditionalOptions.Reservations.PartyCompletion);
        Assert.False(ev.AdditionalOptions.Reservations.PartyCompletion.Enabled);
    }
}
