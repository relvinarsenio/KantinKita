using KantinKita.Controls;
using KantinKita.Helpers;
using KantinKita.ViewModels;
using KantinKita.Views.Tabs;

namespace KantinKita.Views;

public partial class MainTabsPage : ContentPage
{
    private readonly MainTabsViewModel _viewModel;
    private readonly OrdersViewModel _ordersViewModel;
    private readonly ContentView[] _views;
    private readonly NavTabItem[] _tabs;
    private int _selectedIndex = -1;
    private int _transitionVersion;
    private bool _isPageVisible;
    private double _dockInset = -1;

    public MainTabsPage(MainTabsViewModel viewModel, OrdersViewModel ordersViewModel,
        HomeView homeView, MenuView menuView, OrdersView ordersView, ProfileView profileView)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
        _ordersViewModel = ordersViewModel;
        _views = [homeView, menuView, ordersView, profileView];
        _tabs = [HomeTab, MenuTab, OrdersTab, ProfileTab];
        foreach (var view in _views)
        {
            // An animation shield blocks touches while panes move. Avoid cascading
            // an input-property change through hundreds of children on every tap.
            view.CascadeInputTransparent = false;
            view.SetAppThemeColor(BackgroundColorProperty,
                Color.FromArgb("#F3F5F9"), Colors.Black);
            view.Loaded += (_, _) => ShowScene(view, visible: ReferenceEquals(view, _views[_selectedIndex]));
        }

        NavRail.SizeChanged += (_, _) => MoveSelectionIndicator(animate: false);
        NavSelection.Loaded += (_, _) => MoveSelectionIndicator(animate: false);
        NavSelection.SizeChanged += (_, _) => MoveSelectionIndicator(animate: false);
        SceneInputShield.Loaded += (_, _) => SetSceneInputShield(visible: false);
        FloatingDock.Loaded += (_, _) => SetDockLayer();
        FloatingDock.SizeChanged += (_, _) => UpdateDockInset();
        CartDock.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(IsVisible))
            {
                AnimateCartDock();
                Dispatcher.Dispatch(UpdateDockInset);
            }
        };
        Unloaded += (_, _) => CancelTabTransition();
        UpdateDockInset();
        SelectTab(0);
    }

    public void SelectTab(int index)
    {
        index = Math.Clamp(index, 0, _views.Length - 1);
        if (index == _selectedIndex)
        {
            if (index == 2) _ordersViewModel.Refresh();
            return;
        }

        var previousIndex = _selectedIndex;
        var animate = previousIndex >= 0 && _isPageVisible;
        CancelTabTransition();
        _selectedIndex = index;
        for (var i = 0; i < _tabs.Length; i++) _tabs[i].IsSelected = i == index;
        if (index == 2) _ordersViewModel.Refresh();

        var incoming = _views[index];
        if (incoming.Parent is null) PageHost.Children.Add(incoming);
        incoming.IsVisible = true;
        incoming.InputTransparent = false;
        ShowScene(incoming, visible: true);
        MoveSelectionIndicator(animate);

        if (animate && previousIndex >= 0)
        {
            var outgoing = _views[previousIndex];
            outgoing.InputTransparent = true;
            SetSceneInputShield(visible: true);
            if (AnimateScene(outgoing, incoming, index > previousIndex ? 1 : -1)) return;
        }
        FinishTabTransition();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _isPageVisible = true;
        _viewModel.Refresh();
        MoveSelectionIndicator(animate: false);
    }

    protected override void OnDisappearing()
    {
        _isPageVisible = false;
        CancelTabTransition();
        base.OnDisappearing();
    }

    protected override bool OnBackButtonPressed() =>
        _selectedIndex == 0 && _views[0] is HomeView home && home.DismissNotificationsIfOpen()
        || base.OnBackButtonPressed();

    private void FinishTabTransition()
    {
        ++_transitionVersion;
        SetSceneInputShield(visible: false);
        for (var i = 0; i < _views.Length; i++)
        {
            ResetSceneMotion(_views[i]);
            _views[i].InputTransparent = i != _selectedIndex;
            // Native Invisible keeps measurements warm without drawing inactive tabs.
            // Toggling MAUI IsVisible would invalidate each page's whole layout tree.
            ShowScene(_views[i], visible: i == _selectedIndex);
        }
    }

    private void CancelTabTransition() => FinishTabTransition();

    private void UpdateDockInset()
    {
        var height = FloatingDock.Height > 0 ? FloatingDock.Height : (_viewModel.IsCartVisible ? 158 : 82);
        var inset = Math.Ceiling(height + FloatingDock.Margin.Bottom + 12);
        if (Math.Abs(_dockInset - inset) < 0.5) return;
        _dockInset = inset;
        foreach (var content in _views.OfType<IFloatingDockContent>()) content.SetDockInset(inset);
    }

    private void OnTabTapped(object? sender, EventArgs e)
    {
        if (sender is not NavTabItem tab) return;
        var index = Array.IndexOf(_tabs, tab);
        if (index < 0 || index == _selectedIndex) return;
        SelectTab(index);
        Haptics.Tap();
    }
}
