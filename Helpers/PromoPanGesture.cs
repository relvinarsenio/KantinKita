namespace KantinKita.Helpers;

internal enum PromoPanResult
{
    Ignored,
    Tap,
    SnapBack,
    Next,
    Previous,
}

internal enum PromoPanAxis
{
    Undecided,
    Horizontal,
    Vertical,
}

/// <summary>Tracks one touch sequence independently of the card's visual animation.</summary>
internal sealed class PromoPanGesture
{
    private const double DirectionSlop = 8;
    private int? _gestureId;

    public double OffsetX { get; private set; }
    public bool IsActive => _gestureId.HasValue;
    public PromoPanAxis Axis { get; private set; }

    public void Start(int gestureId)
    {
        Reset();
        _gestureId = gestureId;
    }

    public bool Update(int gestureId, double totalX, double totalY)
    {
        if (_gestureId != gestureId)
        {
            return false;
        }

        if (Axis == PromoPanAxis.Undecided)
        {
            if (Math.Max(Math.Abs(totalX), Math.Abs(totalY)) < DirectionSlop)
            {
                return false;
            }

            Axis = Math.Abs(totalX) > Math.Abs(totalY)
                ? PromoPanAxis.Horizontal
                : PromoPanAxis.Vertical;
        }

        if (Axis != PromoPanAxis.Horizontal)
        {
            // A vertical scroll must not become a promo swipe halfway through.
            return false;
        }

        OffsetX = totalX;
        return true;
    }

    public PromoPanResult Finish(int gestureId, double width, bool canceled)
    {
        if (_gestureId != gestureId)
        {
            return PromoPanResult.Ignored;
        }

        // Consume before awaiting any animation: duplicate end/running events are ignored.
        _gestureId = null;
        if (canceled)
        {
            return PromoPanResult.SnapBack;
        }

        if (Axis == PromoPanAxis.Undecided)
        {
            return PromoPanResult.Tap;
        }

        var threshold = Math.Clamp(width * 0.22, 56, 110);
        if (Axis != PromoPanAxis.Horizontal || Math.Abs(OffsetX) < threshold)
        {
            return PromoPanResult.SnapBack;
        }

        return OffsetX < 0 ? PromoPanResult.Next : PromoPanResult.Previous;
    }

    public void Reset()
    {
        _gestureId = null;
        Axis = PromoPanAxis.Undecided;
        OffsetX = 0;
    }
}
