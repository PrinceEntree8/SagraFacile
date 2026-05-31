using SagraFacile.Domain.Features.Events;
using SagraFacile.Infrastructure.Repositories;
using SagraFacile.Infrastructure.Tests;

namespace SagraFacile.Infrastructure.Tests.Repositories;

public class EventAdditionalOptionsRepositoryTests
{
    [Fact]
    public async Task AdditionalOptions_DefaultEvent_HasSafeDefaults()
    {
        using var factory = new TestDbContextFactory();
        await using var repo = new EventRepository(factory);

        var ev = new Event { Name = "Default Options Test", Currency = "EUR", CurrencySymbol = "€" };
        await repo.AddAsync(ev, CancellationToken.None);
        await repo.SaveChangesAsync(CancellationToken.None);

        var found = await repo.GetByIdAsync(ev.Id, CancellationToken.None);

        Assert.NotNull(found);
        Assert.NotNull(found!.AdditionalOptions);
        Assert.False(found.AdditionalOptions.Reservations.PartyCompletion.Enabled);
        Assert.True(found.AdditionalOptions.Reservations.PartyCompletion.MinPartySize >= 1);
    }

    [Fact]
    public async Task AdditionalOptions_RoundTrip_PersistsAndRestoresCorrectly()
    {
        using var factory = new TestDbContextFactory();
        await using var repo = new EventRepository(factory);

        var ev = new Event
        {
            Name = "Round Trip Test",
            Currency = "EUR",
            CurrencySymbol = "€",
            AdditionalOptions = new EventAdditionalOptions
            {
                Reservations = new ReservationOptions
                {
                    PartyCompletion = new PartyCompletionOptions { Enabled = true, MinPartySize = 12 }
                }
            }
        };
        await repo.AddAsync(ev, CancellationToken.None);
        await repo.SaveChangesAsync(CancellationToken.None);

        await using var repo2 = new EventRepository(factory);
        var found = await repo2.GetByIdAsync(ev.Id, CancellationToken.None);

        Assert.NotNull(found);
        Assert.True(found!.AdditionalOptions.Reservations.PartyCompletion.Enabled);
        Assert.Equal(12, found.AdditionalOptions.Reservations.PartyCompletion.MinPartySize);
    }

    [Fact]
    public async Task AdditionalOptions_Update_PersistsNewValues()
    {
        using var factory = new TestDbContextFactory();
        await using var repo = new EventRepository(factory);

        var ev = new Event { Name = "Update Test", Currency = "EUR", CurrencySymbol = "€" };
        await repo.AddAsync(ev, CancellationToken.None);
        await repo.SaveChangesAsync(CancellationToken.None);

        ev.AdditionalOptions = new EventAdditionalOptions
        {
            Reservations = new ReservationOptions
            {
                PartyCompletion = new PartyCompletionOptions { Enabled = true, MinPartySize = 5 }
            }
        };
        await repo.SaveChangesAsync(CancellationToken.None);

        await using var repo2 = new EventRepository(factory);
        var found = await repo2.GetByIdAsync(ev.Id, CancellationToken.None);

        Assert.NotNull(found);
        Assert.True(found!.AdditionalOptions.Reservations.PartyCompletion.Enabled);
        Assert.Equal(5, found.AdditionalOptions.Reservations.PartyCompletion.MinPartySize);
    }
}
