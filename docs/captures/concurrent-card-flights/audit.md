# Concurrent card flights — 09-10-2026

Request: top card departs first; waiting cards retain their source poses; independent taps overlap without cancelling earlier flights.

## Confirmed baseline

`firstBatch=3 visibleBeforeDeparture=0 afterSecondTap=3 firstGhostRetained=False`

`Render` called `FinishAnimations` on every tap/redraw. Stack ghosts were inactive until departure, so the settled redraw removed all source cards immediately.

## Change

- Ordinary renders retain active ghosts and outgoing peg roots.
- Pending cards show at the captured world position/rotation/scale until departure.
- Preserve successor pose and rebind destination renderers across redraws.
- Track logical peg generations so active arrivals join the correct outgoing peg and successor flights wait for its arrival.
- Preserve departure order within a shared pose root and landing order within a peg generation; independent roots/columns remain concurrent.
- Across batches, a buffer departure waits for the incoming card to reach its groove.
- Explicit attempt reset occurs at StartLevel, not at every draw.

## Evidence

- Unity compilation completed.
- Headless regression: 132 passed, 0 failed, 0 skipped.
- Bounded offscreen runtime observations: see visual-report.txt (29 PASS conditions). These are diagnostic observations, not a Unity PlayMode test suite.
- Actual GameplayScreen L1 two taps: first=9, after second=18, first ghost retained=True, finished=True. See gameplay-report.txt and gameplay-two-stacks.png.
- Temporary diagnostic script initially lacked `using Game.Composition`; corrected and rerun successfully. Console retains that diagnostic compilation error; no new game runtime exception was observed in the successful runs.
- Player save backed up immediately before the session and restored after Play stopped.

## Not run

- Full framework gate.
- Unity EditMode/PlayMode suites.
- Device build and CI.
