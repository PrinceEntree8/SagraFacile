using System;
using System.Net.Http;
using NBomber.Contracts;
using NBomber.CSharp;

namespace SagraFacile.Tests.Smoke.Scenarios;

/// <summary>
/// Anonymous public-page load: loops GET / and /menu (~50/50, no auth header),
/// asserting HTTP 200 plus page-specific SSR markers.
/// </summary>
public static class PublicPagesScenario
{
    public static ScenarioProps Create(TestConfig config, HttpClient anonymousClient)
    {
        var copies = config.PublicPagesCopies;

        return Scenario
            .Create("public_pages", async ctx =>
            {
                var isHome = ctx.Random.Next(2) == 0;
                var path = isHome ? "/" : "/menu";

                return await Step.Run(isHome ? "home" : "menu", ctx, async () =>
                {
                    using var response = await anonymousClient.GetAsync(path, ctx.ScenarioCancellationToken);

                    if ((int)response.StatusCode != 200)
                        return Response.Fail(message: $"GET {path} returned {(int)response.StatusCode}");

                    var body = await response.Content.ReadAsStringAsync(ctx.ScenarioCancellationToken);

                    var markerFound = isHome
                        ? ContainsAny(body, "home-last-called-card", "home-menu-button", "home-container")
                        : ContainsAny(body, "card-title", "card-body", "menu-header", "menu-footer");

                    if (!markerFound)
                        return Response.Fail(message: $"GET {path} returned 200 but no expected page marker was found");

                    return Response.Ok(sizeBytes: (int)(response.Content.Headers.ContentLength ?? body.Length));
                });
            })
            .WithLoadSimulations(
                Simulation.RampingConstant(copies, TimeSpan.FromMinutes(2)),
                Simulation.KeepConstant(copies: copies, during: TimeSpan.FromMinutes(5))
            );
    }

    private static bool ContainsAny(string body, params string[] markers)
    {
        foreach (var marker in markers)
        {
            if (body.Contains(marker, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }
}
