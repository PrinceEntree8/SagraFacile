# Requirements

Overview & Goals
Run the test suite, identify why tests fail, and restore green status for the failing reservation-related application tests.

Scope
In Scope
* Fix failures in tests/SagraFacile.Application.Tests caused by ArgumentNullException in reservation handlers.
* Keep existing reservation business behavior unchanged (status transitions, notifications, retry behavior).
* Ensure dotnet test passes for the affected test project (and solution if run end-to-end).

Out of Scope
* Refactoring unrelated features or changing domain rules.
* Addressing package vulnerability warnings (NU1902/NU1903) observed during restore.

Functional Requirements
* Handlers that publish reservation counter updates must not throw when repository counter data is absent/null in tests or edge runtime conditions.
* Existing success/failure command outcomes and status transitions in reservation commands must remain intact.
* Existing notifier calls (EnqueueStatusChangedAsync, EnqueueCountersUpdatedAsync) must still occur for successful flows.

# Technical Design

Current Implementation
From dotnet test, 12 tests fail in SagraFacile.Application.Tests, all with System.ArgumentNullException (source) at Enumerable.Select(...) over repository.GetCountersAsync(...) results.

Confirmed failing handlers:
* src/SagraFacile.Application/Features/Reservations/SeatReservation.cs (line ~72)
* src/SagraFacile.Application/Features/Reservations/MarkPartyComplete.cs (line ~65)
* src/SagraFacile.Application/Features/Reservations/CallReservation.cs (line ~97)
* src/SagraFacile.Application/Features/Reservations/CreateReservation.cs (line ~85)

Pattern in these files:
* var counters = (await repository.GetCountersAsync(...)).Select(...).ToList();
* In many tests, GetCountersAsync is not explicitly configured, so NSubstitute returns null for Task<List<ReservationCounterDto>>, which then fails at .Select(...).

Reference pattern in tests:
* RestoreReservationHandlerTests explicitly stubs counters with [] and does not fail.

Key Decisions
* Make handlers resilient to null counter collections by normalizing to an empty sequence before projection.
* Apply the same defensive pattern consistently across reservation handlers that map counters to ReservationCounterDto to avoid repeated future regressions.
* Keep behavior of notifications unchanged: counters update still sent, but with empty list when repository returns null.

Proposed Changes
* Update affected handlers to use null-safe mapping, e.g.:
  * var rawCounters = await repository.GetCountersAsync(...);
  * var counters = (rawCounters ?? []).Select(...).ToList();
* Apply this in:
  * CreateReservation.Handler
  * CallReservation.Handler
  * SeatReservation.Handler
  * MarkPartyComplete.Handler
* Optionally extend same safeguard to similar reservation handlers (EditReservation, RestoreReservation, CallAndSeatReservation, VoidReservation, etc.) if they use the same direct projection pattern, for consistency.

Risks
* Low risk: change is defensive and does not alter success criteria/state transitions.
* Minor behavioral nuance: if counters retrieval unexpectedly returns null in production, UI receives empty counters update instead of exception (preferred fail-safe).

# Testing

Validation Approach
* Re-run dotnet test after code changes.
* Focus on tests/SagraFacile.Application.Tests reservation feature tests that previously failed.

Key Scenarios
* CallReservationHandlerTests scenarios (waiting, party-completed, already-called).
* SeatReservationHandlerTests successful seat flow.
* MarkPartyCompleteHandlerTests successful completion flow.
* CreateReservationHandlerTests normal create + unique-retry + party completion option paths.

Edge Cases
* Repository returns null counters list: handlers still complete successfully and emit counters notification with empty list.
* Existing concurrency/validation failure tests continue to pass unchanged.

Test Changes
* If needed, add/adjust a targeted unit test to explicitly verify null counters are handled safely in one representative handler (optional if current suite already covers through implicit NSubstitute null behavior).

# Delivery Steps

Step 1: Implement null-safe counter mapping in failing reservation handlers
All failing reservation command handlers stop throwing when GetCountersAsync returns null.

* Update CreateReservation.cs, CallReservation.cs, SeatReservation.cs, and MarkPartyComplete.cs to normalize repository counters to an empty list before .Select(...).
* Preserve existing command semantics: status transitions, messages, SaveChangesAsync, and notification enqueue behavior.
* Keep code style aligned with existing project conventions (file-scoped namespaces, async flow, current CQRS handler structure).

Step 2: Harden similar reservation handlers using the same projection pattern
Reservation handlers sharing the same counters projection pattern are made consistently defensive.

* Review other files in src/SagraFacile.Application/Features/Reservations/ that call GetCountersAsync(...).Select(...) (e.g., EditReservation, RestoreReservation, CallAndSeatReservation, VoidReservation).
* Apply the same null-safe mapping where needed to prevent future test/runtime regressions.
* Avoid changing unrelated logic or business rules in those handlers.

Step 3: Validate with tests and confirm regression closure
Reservation application tests run successfully and the previously failing cases are resolved.

* Execute dotnet test and verify former ArgumentNullException failures are gone.
* Confirm previously failing test classes now pass: CreateReservationHandlerTests, CallReservationHandlerTests, SeatReservationHandlerTests, MarkPartyCompleteHandlerTests.
* Report any remaining non-related failures separately (if present), without expanding scope beyond this fix.