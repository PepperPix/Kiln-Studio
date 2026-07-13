namespace Kiln.Studio.ViewModels;

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

/// <summary>
/// Drives the persistent left navigation rail (ADR-054/PLAN-072): six fixed destinations, the
/// currently-selected one, and an expanded/collapsed (icon+text vs. icon-only) display state.
///
/// Note on scope: the real Asset-browsing surface (ADR-055/PLAN-073) is a dedicated follow-up
/// plan, not part of PLAN-072 - its step list never builds an Assets view, so "Assets" is a
/// placeholder destination here alongside Menus/Theme/Deployment (documented as an
/// implementation-detail decision in the PLAN-072 execution report).
/// </summary>
public partial class NavRailViewModel : ViewModelBase
{
    public ObservableCollection<NavRailItemViewModel> Items { get; } = [];

    [ObservableProperty]
    private NavTarget _selected = NavTarget.Content;

    [ObservableProperty]
    private bool _isExpanded = true;

    public NavRailViewModel()
    {
        Items.Add(new NavRailItemViewModel(NavTarget.Content, "Content", "Note", isPlaceholder: false));
        Items.Add(new NavRailItemViewModel(NavTarget.Assets, "Assets", "ImageMultipleOutline", isPlaceholder: true));
        Items.Add(new NavRailItemViewModel(NavTarget.Menus, "Menus", "Menu", isPlaceholder: true));
        Items.Add(new NavRailItemViewModel(NavTarget.Theme, "Theme", "PaletteOutline", isPlaceholder: true));
        Items.Add(new NavRailItemViewModel(NavTarget.Deployment, "Deployment", "CloudUploadOutline", isPlaceholder: true));
        Items.Add(new NavRailItemViewModel(NavTarget.Settings, "Settings", "CogOutline", isPlaceholder: false));

        UpdateSelectionFlags();
    }

    partial void OnSelectedChanged(NavTarget value) => UpdateSelectionFlags();

    private void UpdateSelectionFlags()
    {
        foreach (var item in Items)
            item.IsSelected = item.Target == Selected;
    }

    [RelayCommand]
    private void Select(NavTarget target) => Selected = target;

    [RelayCommand]
    private void ToggleExpanded() => IsExpanded = !IsExpanded;
}
