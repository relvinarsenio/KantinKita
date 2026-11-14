#if ANDROID
using Android.Animation;
using Android.Views;
using Android.Views.Animations;
using KantinKita.Helpers;
using KantinKita.ViewModels;
using NativeView = Android.Views.View;

namespace KantinKita.Views.Tabs;

public partial class HomeView
{
    private readonly PromoPanGesture _pan = new();
    private readonly DecelerateInterpolator _promoEasing = new(1.6f);
    private readonly PathInterpolator _promoReturnEasing = new(0.2f, 0.9f, 0.25f, 1f);
    private NativeView? _deckView;
    private NativeView? _frontCard;
    private NativeView? _backCard;
    private LayerType _frontLayer;
    private LayerType _backLayer;
    private bool _hasLayers;
    private bool _isAnimating;
    private int _animationVersion;
    private int _gestureSequence;
    private int _pointerId = -1;
    private float _downX;
    private float _downY;
    private float _density = 1f;
    private PromoPanResult _pendingAdvance;

    private float Density => _density;

    private void InitializePromoInput()
    {
        PromoDeck.HandlerChanging += (_, _) => DetachPromoInput();
        PromoDeck.HandlerChanged += (_, _) => AttachPromoInput();
        PromoDeck.Loaded += (_, _) => AttachPromoInput();
    }

    private void AttachPromoInput()
    {
        if (PromoDeck.Handler?.PlatformView is not NativeView nativeView || ReferenceEquals(_deckView, nativeView))
        {
            return;
        }

        DetachPromoInput();
        _deckView = nativeView;
        _density = nativeView.Resources?.DisplayMetrics?.Density ?? 1f;
        // A single controller handles the complete sequence, including native Cancel.
        // There is no second MAUI pan/tap detector whose state can remain active.
        nativeView.Touch += OnPromoTouch;
        nativeView.Click += OnPromoClick;
    }

    private void DetachPromoInput()
    {
        CancelPromoInteraction();
        if (_deckView is { } nativeView)
        {
            nativeView.Touch -= OnPromoTouch;
            nativeView.Click -= OnPromoClick;
        }

        _deckView = null;
        _frontCard = null;
        _backCard = null;
    }

    private async void OnPromoClick(object? sender, EventArgs e)
    {
        if (!_isAnimating && !_pan.IsActive && IsVisible && !InputTransparent && BindingContext is HomeViewModel vm)
        {
            await vm.OpenPromoCommand.ExecuteAsync(null);
        }
    }

    private void OnPromoTouch(object? sender, NativeView.TouchEventArgs e)
    {
        if (e.Event is not { } motion || !IsVisible || InputTransparent || !TryGetNativeCards())
        {
            e.Handled = false;
            return;
        }

        e.Handled = true;
        switch (motion.ActionMasked)
        {
            case MotionEventActions.Down:
                // Read Resources once per gesture, not repeatedly through JNI on every Move.
                _density = _deckView?.Resources?.DisplayMetrics?.Density ?? 1f;
                CompletePromoTransition();
                _pointerId = motion.GetPointerId(0);
                _downX = motion.RawX;
                _downY = motion.RawY;
                _pan.Start(++_gestureSequence);
                // Observe the first move before deciding whether the parent should scroll.
                _deckView?.Parent?.RequestDisallowInterceptTouchEvent(true);
                break;

            case MotionEventActions.Move:
                if (!_pan.IsActive)
                {
                    return;
                }
                if (motion.PointerCount != 1 || motion.GetPointerId(0) != _pointerId)
                {
                    CancelPromoInteraction();
                    return;
                }

                UpdatePromoDrag(motion.RawX, motion.RawY);
                break;

            case MotionEventActions.Up:
                if (!_pan.IsActive)
                {
                    return;
                }

                // Use the actual release coordinates even if no Move event was delivered.
                UpdatePromoDrag(motion.RawX, motion.RawY);
                var result = _pan.Finish(_gestureSequence, _deckView!.Width / Density, canceled: false);
                ReleaseParentScroll();
                _pointerId = -1;
#if DEBUG
                Android.Util.Log.Debug("KantinKita.Promo", $"Release: {result}; offset={_pan.OffsetX:F1}dp");
#endif
                if (result == PromoPanResult.Tap)
                {
                    ResetNativeCards();
                    _deckView.PerformClick();
                }
                else
                {
                    AnimatePromo(result);
                }
                break;

            case MotionEventActions.Cancel:
            case MotionEventActions.PointerDown:
            case MotionEventActions.Outside:
#if DEBUG
                Android.Util.Log.Debug("KantinKita.Promo", "Touch canceled; resetting gesture and card");
#endif
                CancelPromoInteraction();
                break;
        }
    }

    private void UpdatePromoDrag(float rawX, float rawY)
    {
        var horizontal = _pan.Update(_gestureSequence, (rawX - _downX) / Density, (rawY - _downY) / Density);
        if (_pan.Axis == PromoPanAxis.Vertical)
        {
            // ScrollView can intercept the next move; its Cancel is handled above.
            ReleaseParentScroll();
            return;
        }

        if (!horizontal || _frontCard is null || _backCard is null || _deckView is null)
        {
            return;
        }

        CacheCardLayers();
        if (BindingContext is HomeViewModel vm)
        {
            vm.ShowPromoPreview(previous: _pan.OffsetX > 0);
        }

        var width = Math.Max(_deckView.Width, 1);
        var offset = Math.Clamp((float)_pan.OffsetX * Density, -width, width);
        var progress = Math.Clamp(Math.Abs(offset) / (width * 0.5f), 0, 1);
        var reveal = progress * (2 - progress);
        var direction = Math.Sign(offset);
        // Native render properties avoid managed binding/layout work on every frame.
        _frontCard.PivotX = _frontCard.Width * 0.5f;
        _frontCard.PivotY = _frontCard.Height * 0.82f;
        _frontCard.TranslationX = offset;
        _frontCard.TranslationY = -2.5f * Density * reveal;
        _frontCard.Rotation = direction * 3 * reveal;
        _frontCard.ScaleX = _frontCard.ScaleY = 1;
        _frontCard.Alpha = 1 - 0.06f * reveal;
        // Keep both cards at their final size. Depth comes from a small opposing
        // parallax, never a zoom that has to jump or grow after the finger lifts.
        _backCard.ScaleX = _backCard.ScaleY = 1;
        _backCard.Alpha = 1;
        _backCard.TranslationX = -direction * 3 * Density * (1 - reveal);
        _backCard.TranslationY = 2 * Density * (1 - reveal);
        _backCard.Rotation = 0;
    }

    private void AnimatePromo(PromoPanResult result)
    {
        if (result == PromoPanResult.Ignored || !IsVisible || !IsLoaded || !TryGetNativeCards())
        {
            return;
        }

        if (_isAnimating || _pendingAdvance != PromoPanResult.Ignored)
        {
            CompletePromoTransition();
        }
        var advancing = result is PromoPanResult.Next or PromoPanResult.Previous;
        _pendingAdvance = advancing ? result : PromoPanResult.Ignored;
        if (BindingContext is HomeViewModel vm && advancing)
        {
            vm.ShowPromoPreview(previous: result == PromoPanResult.Previous);
            Haptics.Tap();
        }

        if ((!advancing && _frontCard!.TranslationX == 0) ||
            (OperatingSystem.IsAndroidVersionAtLeast(26) && !ValueAnimator.AreAnimatorsEnabled()))
        {
            CompletePromoTransition();
            return;
        }

        _isAnimating = true;
        var version = ++_animationVersion;
        CacheCardLayers();
        var direction = result == PromoPanResult.Next ? -1 : 1;
        var exitX = (_deckView!.Width + 64 * Density) * direction;
        var duration = advancing ? 280L : 260L;

        // The incoming surface reaches its final position before the handoff.
        // Its dimensions never change, including while the new front is drawn.
        _backCard!.Animate()!
            .ScaleX(1)!.ScaleY(1)!.Alpha(1)!
            .TranslationX(0)!
            .TranslationY(0)!
            .Rotation(0)!
            .SetDuration(duration)!
            .SetInterpolator(advancing ? _promoEasing : _promoReturnEasing)!
            .Start();

        _frontCard!.Animate()!
            .TranslationX(advancing ? exitX : 0)!
            .TranslationY(advancing ? -6 * Density : 0)!
            .Rotation(advancing ? direction * 6 : 0)!
            .ScaleX(1)!.ScaleY(1)!
            .Alpha(advancing ? 0 : 1)!
            .SetDuration(duration)!
            .SetInterpolator(advancing ? _promoEasing : _promoReturnEasing)!
            .WithEndAction(new Java.Lang.Runnable(() =>
            {
                if (version == _animationVersion)
                {
                    if (advancing) PromotePreview(version);
                    else CompletePromoTransition();
                }
            }))!
            .Start();
    }

    private void PromotePreview(int version)
    {
        if (version != _animationVersion) return;
        if (!TryGetNativeCards() || BindingContext is not HomeViewModel vm)
        {
            CompletePromoTransition();
            return;
        }

        var result = _pendingAdvance;
        _pendingAdvance = PromoPanResult.Ignored;
        _backCard!.Animate()?.Cancel();
        // Keep PreviewPromo pointing to the card that is currently visible. The
        // new CurrentPromo is prepared underneath with the identical transform.
        vm.AdvancePromo(previous: result == PromoPanResult.Previous, preservePreview: true);
        _frontCard!.TranslationX = 0;
        _frontCard.TranslationY = _backCard!.TranslationY;
        _frontCard.Rotation = 0;
        _frontCard.ScaleX = _backCard.ScaleX;
        _frontCard.ScaleY = _backCard.ScaleY;
        _frontCard.Alpha = 0;

        // Binding updates can invalidate text/geometry. Allow a full draw while
        // the existing preview stays visible; then exchange identical surfaces.
        _deckView!.PostOnAnimation(new Java.Lang.Runnable(() =>
        {
            if (version != _animationVersion || !IsAlive(_deckView)) return;
            _deckView!.PostOnAnimation(new Java.Lang.Runnable(() =>
            {
                if (version != _animationVersion || !TryGetNativeCards()) return;
                _frontCard!.Alpha = 1;
                // Both surfaces already occupy the same pixels. Preparing the
                // next preview is fully occluded by the new opaque front card.
                CompletePromoTransition();
            }));
        }));
    }

    private void CompletePromoTransition()
    {
        ++_animationVersion;
        if (IsAlive(_frontCard)) _frontCard!.Animate()?.Cancel();
        if (IsAlive(_backCard)) _backCard!.Animate()?.Cancel();
        var result = _pendingAdvance;
        _pendingAdvance = PromoPanResult.Ignored;
        _isAnimating = false;

        if (BindingContext is HomeViewModel vm)
        {
            if (result == PromoPanResult.Next) vm.GoToNextPromo();
            else if (result == PromoPanResult.Previous) vm.GoToPreviousPromo();
            vm.ShowPromoPreview(previous: false);
        }

        ResetNativeCards();
    }

    private void CancelPromoInteraction()
    {
        _pan.Reset();
        _pointerId = -1;
        ReleaseParentScroll();
        // A swipe already released commits once, even if a new touch interrupts its animation.
        CompletePromoTransition();
    }

    private void ReleaseParentScroll() => _deckView?.Parent?.RequestDisallowInterceptTouchEvent(false);

    private bool TryGetNativeCards()
    {
        if (PromoCard.Handler?.PlatformView is not NativeView front ||
            PromoBackCard.Handler?.PlatformView is not NativeView back || !IsAlive(_deckView))
        {
            return false;
        }

        _frontCard = front;
        _backCard = back;
        return IsAlive(front) && IsAlive(back);
    }

    private static bool IsAlive(NativeView? view) => view is not null && view.Handle != IntPtr.Zero;

    private void CacheCardLayers()
    {
        if (_hasLayers || !IsAlive(_frontCard) || !IsAlive(_backCard)) return;
        _frontLayer = _frontCard!.LayerType;
        _backLayer = _backCard!.LayerType;
        _frontCard.SetLayerType(LayerType.Hardware, null);
        _backCard.SetLayerType(LayerType.Hardware, null);
        _hasLayers = true;
    }

    private void ResetNativeCards()
    {
        if (IsAlive(_frontCard))
        {
            _frontCard!.TranslationX = _frontCard.TranslationY = _frontCard.Rotation = 0;
            _frontCard.ScaleX = _frontCard.ScaleY = _frontCard.Alpha = 1;
            if (_hasLayers) _frontCard.SetLayerType(_frontLayer, null);
        }
        ResetBackCard();
        if (_hasLayers && IsAlive(_backCard)) _backCard!.SetLayerType(_backLayer, null);

        _hasLayers = false;
    }

    private void ResetBackCard()
    {
        if (!IsAlive(_backCard)) return;
        _backCard!.TranslationX = _backCard.Rotation = 0;
        _backCard.TranslationY = 0;
        _backCard.ScaleX = _backCard.ScaleY = _backCard.Alpha = 1;
    }
}
#endif
