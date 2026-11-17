# Photobooth

Photobooth app core (Phase 1). Requires the .NET 8 SDK.

## Run the tests
    dotnet run --project tests/Photobooth.Core.Tests

On GitHub: open the **Actions** tab, select **CI**, and check the "Run tests" step.

## Contents
- `src/Photobooth.Core`: ReviewSession (swap, retake, undo, print lock), SessionWorkflow state machine, camera and printer interfaces.
- `tests/Photobooth.Core.Tests`: 18 tests.

Next: WPF shell and SQLite, then Canon EDSDK and webcam providers.
