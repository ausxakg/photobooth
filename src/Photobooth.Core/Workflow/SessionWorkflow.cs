namespace Photobooth.Core.Workflow;

public enum Screen { Welcome, Preview, Countdown, Capturing, Review, RetakeCountdown, RetakeCapturing, Printing, Sharing }

/// <summary>Guest workflow state machine. Invalid transitions throw.</summary>
public sealed class SessionWorkflow
{
    public Screen Current { get; private set; } = Screen.Welcome;
    public int ShotsTaken { get; private set; }
    public int ShotsRequired { get; }
    public int? RetakeSlot { get; private set; }

    public SessionWorkflow(int shotsRequired)
    {
        if (shotsRequired < 1) throw new ArgumentOutOfRangeException(nameof(shotsRequired));
        ShotsRequired = shotsRequired;
    }

    private void Require(params Screen[] allowed)
    {
        if (!allowed.Contains(Current)) throw new InvalidOperationException($"Not allowed from {Current}.");
    }

    public void Start() { Require(Screen.Welcome); ShotsTaken = 0; Current = Screen.Preview; }
    public void BeginCountdown() { Require(Screen.Preview); Current = Screen.Countdown; }
    public void BeginCapture() { Require(Screen.Countdown); Current = Screen.Capturing; }

    public void ShotCaptured()
    {
        Require(Screen.Capturing);
        ShotsTaken++;
        Current = ShotsTaken >= ShotsRequired ? Screen.Review : Screen.Preview;
    }

    public void BeginRetake(int slot)
    {
        Require(Screen.Review);
        if (slot < 0 || slot >= ShotsRequired) throw new ArgumentOutOfRangeException(nameof(slot));
        RetakeSlot = slot; Current = Screen.RetakeCountdown;
    }
    public void RetakeCapturing() { Require(Screen.RetakeCountdown); Current = Screen.RetakeCapturing; }
    public void RetakeDone() { Require(Screen.RetakeCapturing); RetakeSlot = null; Current = Screen.Review; }
    public void RetakeCancelled() { Require(Screen.RetakeCountdown, Screen.RetakeCapturing); RetakeSlot = null; Current = Screen.Review; }

    public void ConfirmPrint() { Require(Screen.Review); Current = Screen.Printing; }
    public void PrintDone() { Require(Screen.Printing); Current = Screen.Sharing; }
    public void Finish() { Require(Screen.Sharing); Current = Screen.Welcome; }
    public void Cancel() { if (Current != Screen.Welcome) { Current = Screen.Welcome; ShotsTaken = 0; RetakeSlot = null; } }
}
