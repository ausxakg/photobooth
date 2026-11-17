using Photobooth.Core.Models;
using Photobooth.Core.Review;
using Photobooth.Core.Workflow;

int pass = 0, fail = 0;
void Test(string name, Action a)
{
    try { a(); pass++; Console.WriteLine($"PASS {name}"); }
    catch (Exception e) { fail++; Console.WriteLine($"FAIL {name}: {e.Message}"); }
}
void Eq<T>(T exp, T act) { if (!EqualityComparer<T>.Default.Equals(exp, act)) throw new Exception($"expected {exp}, got {act}"); }
void Throws<E>(Action a) where E : Exception { try { a(); } catch (E) { return; } throw new Exception($"expected {typeof(E).Name}"); }

Capture Cap(string n) => new(Guid.NewGuid(), $"orig/{n}.jpg", DateTimeOffset.UtcNow);
(ReviewSession s, Capture[] c) Make() { var c = new[] { Cap("1"), Cap("2"), Cap("3"), Cap("4") }; return (new ReviewSession(c), c); }
string Order(ReviewSession s) => string.Join(",", s.Slots.Select(x => Path.GetFileNameWithoutExtension(s.CaptureAt(x.SlotIndex).OriginalPath)));

Test("swap 1 and 3 matches spec", () => { var (s, _) = Make(); s.Swap(0, 2); Eq("3,2,1,4", Order(s)); });
Test("swap is its own inverse", () => { var (s, _) = Make(); s.Swap(0, 2); s.Swap(0, 2); Eq("1,2,3,4", Order(s)); });
Test("swap does not alter captures", () => { var (s, c) = Make(); s.Swap(0, 3); Eq(4, s.AllCaptures.Count); foreach (var x in c) Eq(true, s.AllCaptures.Contains(x)); });
Test("retake replaces only selected slot", () => { var (s, _) = Make(); s.Retake(1, Cap("2b")); Eq("1,2b,3,4", Order(s)); });
Test("retake keeps original capture", () => { var (s, c) = Make(); s.Retake(1, Cap("2b")); Eq(5, s.AllCaptures.Count); Eq(true, s.AllCaptures.Contains(c[1])); });
Test("multiple retakes of same slot", () => { var (s, _) = Make(); s.Retake(1, Cap("2b")); s.Retake(1, Cap("2c")); Eq("1,2c,3,4", Order(s)); Eq(2, s.RetakeCount); });
Test("retake after swap targets slot position", () => { var (s, _) = Make(); s.Swap(0, 2); s.Retake(0, Cap("x")); Eq("x,2,1,4", Order(s)); });
Test("undo multiple steps", () => { var (s, _) = Make(); s.Swap(0, 1); s.Swap(2, 3); s.Undo(); Eq("2,1,3,4", Order(s)); s.Undo(); Eq("1,2,3,4", Order(s)); Eq(false, s.Undo()); });
Test("undo a retake restores previous capture", () => { var (s, _) = Make(); s.Retake(2, Cap("3b")); s.Undo(); Eq("1,2,3,4", Order(s)); });
Test("bad slot index rejected", () => { var (s, _) = Make(); Throws<ArgumentOutOfRangeException>(() => s.Swap(0, 9)); });
Test("duplicate capture id rejected", () => { var (s, c) = Make(); Throws<ArgumentException>(() => s.Retake(0, c[1])); });
Test("confirm locks session (no duplicate print)", () => { var (s, _) = Make(); s.ConfirmForPrint(); Throws<InvalidOperationException>(() => s.ConfirmForPrint()); Throws<InvalidOperationException>(() => s.Swap(0, 1)); });
Test("empty session rejected", () => Throws<ArgumentException>(() => new ReviewSession(Array.Empty<Capture>())));

Test("workflow 4-shot happy path", () =>
{
    var w = new SessionWorkflow(4); w.Start();
    for (int i = 0; i < 4; i++) { w.BeginCountdown(); w.BeginCapture(); w.ShotCaptured(); }
    Eq(Screen.Review, w.Current); w.ConfirmPrint(); w.PrintDone(); w.Finish(); Eq(Screen.Welcome, w.Current);
});
Test("workflow retake returns to review without restarting", () =>
{
    var w = new SessionWorkflow(2); w.Start();
    for (int i = 0; i < 2; i++) { w.BeginCountdown(); w.BeginCapture(); w.ShotCaptured(); }
    w.BeginRetake(1); Eq(1, w.RetakeSlot); w.RetakeCapturing(); w.RetakeDone();
    Eq(Screen.Review, w.Current); Eq(2, w.ShotsTaken);
});
Test("workflow retake can be cancelled", () =>
{
    var w = new SessionWorkflow(1); w.Start(); w.BeginCountdown(); w.BeginCapture(); w.ShotCaptured();
    w.BeginRetake(0); w.RetakeCancelled(); Eq(Screen.Review, w.Current);
});
Test("workflow blocks invalid transitions", () => { var w = new SessionWorkflow(2); Throws<InvalidOperationException>(() => w.ConfirmPrint()); Throws<InvalidOperationException>(() => w.BeginRetake(0)); });
Test("workflow cancel resets", () => { var w = new SessionWorkflow(3); w.Start(); w.Cancel(); Eq(Screen.Welcome, w.Current); Eq(0, w.ShotsTaken); });

Console.WriteLine($"\n{pass} passed, {fail} failed");
return fail == 0 ? 0 : 1;
