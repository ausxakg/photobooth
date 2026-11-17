namespace Photobooth.Core.Models;

/// <summary>An immutable original capture. Never modified or deleted by review actions.</summary>
public sealed record Capture(Guid Id, string OriginalPath, DateTimeOffset CapturedAt);

/// <summary>A position in the print layout, pointing at a capture.</summary>
public sealed record SessionSlot(int SlotIndex, Guid CaptureId);

public enum SessionStatus { Capturing, Review, Printing, Printed, Cancelled }
