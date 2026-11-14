using System.ComponentModel;
using KantinKita.Helpers;
using KantinKita.ViewModels;
using KantinKita.Models;
using KantinKita.Services;

namespace KantinKita.Views.Tabs;

public partial class HomeView : ContentView, IFloatingDockContent
{
    private readonly HomeNotificationsViewModel _notifications;
    private readonly IAppNavigator _notificationNavigator;
    private readonly IDialogService _catalogDialogs;
    private bool _isCanteenPickerOpen;
    private bool _isCategoryPickerOpen;
    private bool _notificationsOpen;
    private int _notificationAnimationVersion;
    private CancellationTokenSource? _searchSubmission;

    public HomeView(HomeViewModel viewModel, HomeNotificationsViewModel notifications, IAppNavigator navigator, IDialogService dialogs)
    {
        InitializeComponent();
        BindingContext = viewModel;
        viewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(HomeViewModel.IsBrowsing) && !viewModel.IsBrowsing)
                CancelPromoInteraction();
        };
        _notifications = notifications;
        _notificationNavigator = navigator;
        _catalogDialogs = dialogs;
        NotificationOverlay.BindingContext = notifications;
        NotificationBadge.IsVisible = notifications.HasUnread;
        notifications.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(HomeNotificationsViewModel.HasUnread)) NotificationBadge.IsVisible = notifications.HasUnread;
        };
        NotificationOverlay.Loaded += (_, _) => HideNotificationsImmediately();
        SizeChanged += (_, _) =>
        {
            if (Width > 0) NotificationPanel.WidthRequest = Math.Min(320, Math.Max(1, Width - 32));
        };
        InitializePromoInput();
        Unloaded += (_, _) => { CancelPromoInteraction(); HideNotificationsImmediately(); CancelSearchSubmission(); };
        PropertyChanged += OnVisibilityChanged;
    }

    private void OnVisibilityChanged(object? sender, PropertyChangedEventArgs e)
    {
        if ((e.PropertyName == nameof(IsVisible) && !IsVisible) ||
            (e.PropertyName == nameof(InputTransparent) && InputTransparent))
        {
            CancelPromoInteraction();
            HideNotificationsImmediately();
            CancelSearchSubmission();
        }
    }

    private void CancelSearchSubmission()
    {
        _searchSubmission?.Cancel();
        _searchSubmission?.Dispose();
        _searchSubmission = null;
    }

    private async void OnSearchCompleted(object? sender, EventArgs e)
    {
        if (InputTransparent || !IsVisible || !IsLoaded) return;
        CancelSearchSubmission();
        var request = _searchSubmission = new CancellationTokenSource();
        try
        {
            await SearchInteraction.DismissKeyboardAsync(SearchEntry, request.Token);
            if (request.IsCancellationRequested || InputTransparent || !IsVisible ||
                string.IsNullOrWhiteSpace(SearchEntry.Text)) return;
            await HomeScroll.ScrollToAsync(ResultsHeader, ScrollToPosition.Start, SearchInteraction.AnimationsEnabled);
        }
        catch (OperationCanceledException) { }
    }

    private void OnNextTapped(object? sender, EventArgs e) => AnimatePromo(PromoPanResult.Next);

    private async void OnCategoryTapped(object? sender, TappedEventArgs e)
    {
        if (_isCategoryPickerOpen || InputTransparent || !IsVisible || !IsLoaded ||
            BindingContext is not HomeViewModel viewModel) return;
        _isCategoryPickerOpen = true;
        CancelSearchSubmission();
        CancelPromoInteraction();
        try
        {
            await SearchInteraction.DismissKeyboardAsync(SearchEntry);
            if (InputTransparent || !IsVisible || !IsLoaded) return;
            await viewModel.ChooseCategoryCommand.ExecuteAsync(null);
        }
        finally { _isCategoryPickerOpen = false; }
    }

    public void SetDockInset(double bottomInset) => ScrollBody.Padding = new Thickness(20, 12, 20, 28 + bottomInset);

    private void OnPreviousTapped(object? sender, EventArgs e) => AnimatePromo(PromoPanResult.Previous);

    private async void OnCanteenTapped(object? sender, TappedEventArgs e)
    {
        if (_isCanteenPickerOpen || BindingContext is not HomeViewModel viewModel) return;
        _isCanteenPickerOpen = true;
        CancelSearchSubmission();
        CancelPromoInteraction();
        try
        {
            var selected = await _catalogDialogs.ChooseAsync("Mau makan dari mana?",
                viewModel.Canteens.Select(canteen => new DialogChoice(canteen.Id, canteen.Name, canteen.Location)).ToArray(),
                viewModel.SelectedCanteen.Id, "Setiap kantin punya pilihan dan harga menunya sendiri.");
            if (selected is not null)
            {
                CancelPromoInteraction();
                viewModel.SelectCanteen(selected);
            }
        }
        finally { _isCanteenPickerOpen = false; }
    }

    private void OnNotificationsTapped(object? sender, TappedEventArgs e)
    {
        if (_notificationsOpen) CloseNotifications();
        else
        {
            CancelSearchSubmission();
            CancelPromoInteraction();
            _notifications.Refresh(markRead: true);
            _notificationsOpen = true;
            ShowNotifications();
        }
    }

    private void OnNotificationDismissTapped(object? sender, TappedEventArgs e) => CloseNotifications();

    private void OnNotificationPanelTapped(object? sender, TappedEventArgs e) { }

    internal bool DismissNotificationsIfOpen()
    {
        if (!_notificationsOpen) return false;
        CloseNotifications();
        return true;
    }

    private async void OnNotificationEntryTapped(object? sender, TappedEventArgs e)
    {
        if (e.Parameter is not Order order) return;
        HideNotificationsImmediately();
        await _notificationNavigator.OpenReceiptAsync(order);
    }
}
