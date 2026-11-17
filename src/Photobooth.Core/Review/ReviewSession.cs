using Photobooth.Core.Models;

namespace Photobooth.Core.Review;

/// <summary>
/// Slot-assignment model for Photo Review Mode. Swap and undo only change which capture
/// sits in which slot; retake adds a new capture and repoints one slot.
/// Original captures are never removed.
/// </summary>
public sealed class ReviewSession
{
    private readonly Dictionary<Guid, Capture> _captures = new();
    private SessionSlot[] _slots;
    private readonly Stack<SessionSlot[]> _undo = new();

    public Guid SessionId { get; } = Guid.NewGuid();
    public int RetakeCount { get; private set; }
    public bool PrintSubmitted { get; private set; }

    public ReviewSession(IEnumerable<Capture> initial)
    {
        var list = initial.ToList();
        if (list.Count == 0) throw new ArgumentException("At least one capture is required.");
        foreach (var c in list) _captures[c.Id] = c;
        _slots = list.Select((c, i) => new SessionSlot(i, c.Id)).ToArray();
    }

    public int SlotCount => _slots.Length;
    public IReadOnlyList<SessionSlot> Slots => _slots;
    public IReadOnlyCollection<Capture> AllCaptures => _captures.Values;
    public bool CanUndo => _undo.Count > 0;
    public Capture CaptureAt(int slot) => _captures[_slots[Check(slot)].CaptureId];

    private int Check(int slot)
    {
        if (slot < 0 || slot >= _slots.Length) throw new ArgumentOutOfRangeException(nameof(slot));
        return slot;
    }
    private void EnsureEditable()
    {
        if (PrintSubmitted) throw new InvalidOperationException("Session already sent to print.");
    }
    private void Push() => _undo.Push((SessionSlot[])_slots.Clone());

    public void Swap(int a, int b)
    {
        EnsureEditable(); Check(a); Check(b);
        if (a == b) return;
        Push();
        var (ca, cb) = (_slots[a].CaptureId, _slots[b].CaptureId);
        _slots[a] = new SessionSlot(a, cb);
        _slots[b] = new SessionSlot(b, ca);
    }

    /// <summary>Drag-and-drop reorder: dropping onto another slot swaps the two.</summary>
    public void Move(int from, int to) => Swap(from, to);

    public void Retake(int slot, Capture replacement)
    {
        EnsureEditable(); Check(slot);
        if (_captures.ContainsKey(replacement.Id)) throw new ArgumentException("Capture id already used.");
        Push();
        _captures[replacement.Id] = replacement;
        _slots[slot] = new SessionSlot(slot, replacement.Id);
        RetakeCount++;
    }

    public bool Undo()
    {
        EnsureEditable();
        if (_undo.Count == 0) return false;
        _slots = _undo.Pop();
        return true;
    }

    /// <summary>Validates all slots and locks the session; a second call throws (no duplicate prints).</summary>
    public IReadOnlyList<SessionSlot> ConfirmForPrint()
    {
        EnsureEditable();
        foreach (var s in _slots)
            if (!_captures.ContainsKey(s.CaptureId)) throw new InvalidOperationException($"Slot {s.SlotIndex} has no capture.");
        PrintSubmitted = true;
        return _slots;
    }
}
