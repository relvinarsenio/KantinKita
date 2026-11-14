using KantinKita.Helpers;

// Package-free regression runner: dotnet run --project Tests/KantinKita.Gestures.Tests.csproj
var tests = new (string Name, Action Run)[]
{
    ("A short moved drag snaps back instead of opening detail", () =>
    {
        var pan = Drag(-46);
        Equal(PromoPanAxis.Horizontal, pan.Axis);
        Equal(PromoPanResult.SnapBack, pan.Finish(1, 360, false));
    }),
    ("Holding a long drag commits only once on release", () =>
    {
        var pan = Drag(-150);
        True(pan.IsActive);
        for (var update = 0; update < 100; update++)
        {
            True(pan.Update(1, -180, 0));
            True(pan.IsActive);
        }

        Equal(PromoPanResult.Next, pan.Finish(1, 360, false));
        Equal(PromoPanResult.Ignored, pan.Finish(1, 360, false));
        True(!pan.Update(1, -550, 0));
    }),
    ("Right drag selects previous", () =>
    {
        var pan = Drag(120);
        Equal(PromoPanResult.Previous, pan.Finish(1, 360, false));
    }),
    ("Canceled long drag never advances", () =>
    {
        var pan = Drag(-180);
        Equal(PromoPanResult.SnapBack, pan.Finish(1, 360, true));
        True(!pan.IsActive);
        Equal(PromoPanResult.Ignored, pan.Finish(1, 360, false));
    }),
    ("Vertical scroll stays vertical even after sideways drift", () =>
    {
        var pan = new PromoPanGesture();
        pan.Start(1);
        True(!pan.Update(1, 4, 25));
        Equal(PromoPanAxis.Vertical, pan.Axis);
        True(!pan.Update(1, -180, 120));
        Equal(PromoPanAxis.Vertical, pan.Axis);
        Equal(0d, pan.OffsetX);
        Equal(PromoPanResult.SnapBack, pan.Finish(1, 360, false));
    }),
    ("Returning to the origin remains a drag rather than a tap", () =>
    {
        var pan = Drag(-150);
        pan.Update(1, 0, 0);
        Equal(PromoPanAxis.Horizontal, pan.Axis);
        Equal(PromoPanResult.SnapBack, pan.Finish(1, 360, false));
    }),
    ("Reversing direction uses release direction", () =>
    {
        var pan = Drag(-150);
        pan.Update(1, 140, 0);
        Equal(PromoPanResult.Previous, pan.Finish(1, 360, false));
    }),
    ("Unrelated gesture events cannot complete active gesture", () =>
    {
        var pan = Drag(-150);
        True(!pan.Update(2, 200, 0));
        Equal(PromoPanResult.Ignored, pan.Finish(2, 360, false));
        True(pan.IsActive);
        Equal(PromoPanResult.Next, pan.Finish(1, 360, false));
    }),
    ("Late events after hiding the view are ignored", () =>
    {
        var pan = Drag(-150);
        pan.Reset();
        True(!pan.Update(1, -200, 0));
        Equal(PromoPanResult.Ignored, pan.Finish(1, 360, false));
        Equal(PromoPanAxis.Undecided, pan.Axis);
    }),
    ("A new swipe works immediately after native cancellation", () =>
    {
        var pan = Drag(-150);
        // Native Cancel resets the controller even when no MAUI Completed arrives.
        pan.Reset();
        pan.Start(2);
        Equal(PromoPanAxis.Undecided, pan.Axis);
        Equal(0d, pan.OffsetX);
        True(!pan.Update(1, 300, 0));
        True(pan.Update(2, -120, 0));
        Equal(PromoPanResult.Next, pan.Finish(2, 360, false));
    }),
    ("Touch jitter remains a tap without moving the card", () =>
    {
        var pan = new PromoPanGesture();
        pan.Start(1);
        True(!pan.Update(1, 3, 4));
        Equal(PromoPanAxis.Undecided, pan.Axis);
        Equal(0d, pan.OffsetX);
        Equal(PromoPanResult.Tap, pan.Finish(1, 360, false));
    }),
    ("Threshold scales with narrow and wide decks", () =>
    {
        Equal(PromoPanResult.SnapBack, Drag(-55).Finish(1, 200, false));
        Equal(PromoPanResult.Next, Drag(-56).Finish(1, 200, false));
        Equal(PromoPanResult.SnapBack, Drag(-109).Finish(1, 900, false));
        Equal(PromoPanResult.Next, Drag(-110).Finish(1, 900, false));
    }),
    ("A stationary touch opens the promo once", () =>
    {
        var pan = new PromoPanGesture();
        pan.Start(1);
        Equal(PromoPanResult.Tap, pan.Finish(1, 360, false));
        True(!pan.IsActive);
        Equal(PromoPanResult.Ignored, pan.Finish(1, 360, false));
    }),
    ("Final Up coordinates decide the result even without Move", () =>
    {
        var pan = new PromoPanGesture();
        pan.Start(1);
        // The native Up callback feeds the final position before finishing.
        pan.Update(1, -120, 0);
        Equal(PromoPanResult.Next, pan.Finish(1, 360, false));

        pan.Start(2);
        pan.Update(2, -150, 0);
        pan.Update(2, -40, 0);
        Equal(PromoPanResult.SnapBack, pan.Finish(2, 360, false));
    }),
    ("A replacement Down starts with independent coordinates", () =>
    {
        var pan = Drag(-150);
        pan.Start(2);
        Equal(0d, pan.OffsetX);
        Equal(PromoPanResult.Ignored, pan.Finish(1, 360, false));
        pan.Update(2, 20, 0);
        Equal(PromoPanResult.SnapBack, pan.Finish(2, 360, false));
    }),
};

foreach (var test in tests)
{
    test.Run();
    Console.WriteLine($"PASS {test.Name}");
}

Console.WriteLine($"{tests.Length} gesture regression tests passed.");

static PromoPanGesture Drag(double x)
{
    var pan = new PromoPanGesture();
    pan.Start(1);
    pan.Update(1, x, 0);
    return pan;
}

static void True(bool condition)
{
    if (!condition)
    {
        throw new InvalidOperationException("Expected condition to be true.");
    }
}

static void Equal<T>(T expected, T actual)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
    {
        throw new InvalidOperationException($"Expected {expected}, got {actual}.");
    }
}
