using KantinKita.ViewModels;

namespace KantinKita.Views;

public partial class LoginPage : ContentPage
{
    private readonly LoginViewModel _viewModel;
    private bool _isLeaving;
    private int _motionVersion;
    public event EventHandler? SessionStarted;

    public LoginPage(LoginViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _isLeaving = false;
        LoginContent.InputTransparent = false;
        _viewModel.Completed -= OnSessionCompleted;
        _viewModel.Completed += OnSessionCompleted;
        var version = ++_motionVersion;
        Dispatcher.Dispatch(() =>
        {
            if (version != _motionVersion) return;
            AnimateEntrance(BrandHeader, 0, 6, 1);
            AnimateEntrance(WelcomeCard, 35, 12, 1);
            AnimateEntrance(MealCard, 90, 16, .985f);
            AnimateEntrance(CoffeeCard, 140, 10, .96f);
            AnimateEntrance(PickupCard, 185, 12, .985f);
            AnimateEntrance(FormContent, 100, 16, 1);
        });
    }

    protected override void OnDisappearing()
    {
        ++_motionVersion;
        _viewModel.Completed -= OnSessionCompleted;
        foreach (var view in new VisualElement[] { BrandHeader, WelcomeCard, MealCard, CoffeeCard, PickupCard,
            FormContent, LoginContent, NameField, EmailField })
        {
            CancelMotion(view);
        }
        base.OnDisappearing();
    }

    private void OnNameCompleted(object? sender, EventArgs e) => EmailEntry.Focus();
    private void OnEmailCompleted(object? sender, EventArgs e)
    {
        EmailEntry.Unfocus();
        _viewModel.ContinueCommand.Execute(null);
    }

    private void OnFieldFocused(object? sender, FocusEventArgs e) => AnimateField(sender, true);
    private void OnFieldUnfocused(object? sender, FocusEventArgs e) => AnimateField(sender, false);

    private void AnimateField(object? sender, bool focused)
    {
        var border = ReferenceEquals(sender, NameEntry) ? NameField : EmailField;
        if (Application.Current?.Resources is { } resources)
            border.SetAppTheme<Brush>(Border.StrokeProperty,
                new SolidColorBrush((Color)resources[focused ? "BrandLight" : "LineLight"]),
                new SolidColorBrush((Color)resources[focused ? "BrandDark" : "LineDark"]));
#if ANDROID
        if (border.Handler?.PlatformView is Android.Views.View native && MotionEnabled)
        {
            native.Animate()?.Cancel();
            native.Animate()?.TranslationY(focused ? -2 * (native.Resources?.DisplayMetrics?.Density ?? 1f) : 0)
                ?.SetDuration(160)?.Start();
        }
#endif
    }

    private async void OnSessionCompleted(object? sender, EventArgs e)
    {
        if (_isLeaving) return;
        _isLeaving = true;
        NameEntry.Unfocus();
        EmailEntry.Unfocus();
        LoginContent.InputTransparent = true;
        var version = _motionVersion;
        if (MotionEnabled)
        {
            await Task.WhenAll(LoginContent.FadeToAsync(0, 170, Easing.CubicIn),
                LoginContent.ScaleToAsync(.97, 200, Easing.CubicIn));
        }
        if (version == _motionVersion) SessionStarted?.Invoke(this, EventArgs.Empty);
    }

    private static bool MotionEnabled
    {
        get
        {
#if ANDROID
            return !OperatingSystem.IsAndroidVersionAtLeast(26) || Android.Animation.ValueAnimator.AreAnimatorsEnabled();
#else
            return true;
#endif
        }
    }

    private static void AnimateEntrance(VisualElement element, long delay, float offset, float scale)
    {
#if ANDROID
        if (element.Handler?.PlatformView is Android.Views.View native && MotionEnabled)
        {
            native.Animate()?.Cancel();
            native.Alpha = 0;
            native.TranslationY = offset * (native.Resources?.DisplayMetrics?.Density ?? 1f);
            native.ScaleX = native.ScaleY = scale;
            var restingRotation = (float)element.Rotation;
            native.Rotation = restingRotation == 0 ? 0 : restingRotation + (restingRotation > 0 ? 3 : -3);
            native.Animate()?.Alpha(1)?.TranslationY(0)?.ScaleX(1)?.ScaleY(1)?.Rotation(restingRotation)
                ?.SetStartDelay(delay)?.SetDuration(420)
                ?.SetInterpolator(new Android.Views.Animations.PathInterpolator(.16f, 1f, .3f, 1f))?.Start();
        }
#endif
    }

    private static void CancelMotion(VisualElement element)
    {
        element.CancelAnimations();
        element.Opacity = element.Scale = 1;
        element.TranslationY = 0;
#if ANDROID
        if (element.Handler?.PlatformView is Android.Views.View native)
        {
            native.Animate()?.Cancel();
            native.Alpha = native.ScaleX = native.ScaleY = 1;
            native.TranslationX = native.TranslationY = 0;
            native.Rotation = (float)element.Rotation;
        }
#endif
    }
}
