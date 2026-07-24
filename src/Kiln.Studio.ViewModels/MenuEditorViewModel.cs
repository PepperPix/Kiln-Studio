namespace Kiln.Studio.ViewModels;

using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kiln.Studio.Services;

/// <summary>
/// Drives the visual tree editor for <c>site.yaml.menus</c> (PLAN-079). Supports multiple named
/// menus, nested items, ref/url/external configuration, and drag-and-drop reordering.
/// </summary>
public sealed partial class MenuEditorViewModel : ViewModelBase
{
    private readonly IMenuService _menuService;
    private readonly IMenuRefProvider _menuRefProvider;
    private readonly IInputDialog _inputDialog;
    private string? _projectPath;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSave))]
    [NotifyPropertyChangedFor(nameof(HasStatusMessage))]
    private string? _statusMessage;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSave))]
    [NotifyPropertyChangedFor(nameof(HasSelectedMenu))]
    private MenuViewModel? _selectedMenu;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSave))]
    [NotifyPropertyChangedFor(nameof(HasSelectedItem))]
    [NotifyPropertyChangedFor(nameof(SelectedItemIsRef))]
    [NotifyPropertyChangedFor(nameof(SelectedItemIsUrl))]
    private MenuItemViewModel? _selectedItem;

    public ObservableCollection<MenuViewModel> Menus { get; } = [];

    public ObservableCollection<string> RefSuggestions { get; } = [];

    public IReadOnlyList<MenuLinkType> LinkTypes { get; } = [MenuLinkType.Ref, MenuLinkType.Url];

    public bool HasStatusMessage => !string.IsNullOrEmpty(StatusMessage);

    public bool HasSelectedMenu => SelectedMenu is not null;

    public bool HasSelectedItem => SelectedItem is not null;

    public bool SelectedItemIsRef => SelectedItem?.LinkType == MenuLinkType.Ref;

    public bool SelectedItemIsUrl => SelectedItem?.LinkType == MenuLinkType.Url;

    public bool CanSave => !string.IsNullOrWhiteSpace(_projectPath)
        && Menus.All(m => !string.IsNullOrWhiteSpace(m.Name))
        && Menus.SelectMany(m => m.Items.DescendantsAndSelf()).All(i => !string.IsNullOrWhiteSpace(i.Title));

    public MenuEditorViewModel(IMenuService menuService, IMenuRefProvider menuRefProvider, IInputDialog inputDialog)
    {
        _menuService = menuService;
        _menuRefProvider = menuRefProvider;
        _inputDialog = inputDialog;
    }

    public void LoadProject(string projectPath)
    {
        _projectPath = projectPath;
        StatusMessage = null;

        Menus.Clear();
        foreach (var menu in _menuService.LoadMenus(projectPath))
            Menus.Add(new MenuViewModel(menu));

        SelectedMenu = Menus.FirstOrDefault();
        SelectedItem = null;

        RefSuggestions.Clear();
        foreach (var r in _menuRefProvider.GetItemRefs(projectPath))
            RefSuggestions.Add(r);
    }

    public void ClearProject()
    {
        _projectPath = null;
        Menus.Clear();
        SelectedMenu = null;
        SelectedItem = null;
        RefSuggestions.Clear();
        StatusMessage = null;
    }

    [RelayCommand(CanExecute = nameof(CanAddMenu))]
    private async Task AddMenuAsync()
    {
        var name = await _inputDialog.PromptAsync("New menu", "Menu name:").ConfigureAwait(true);
        if (string.IsNullOrWhiteSpace(name))
            return;

        var menu = new MenuViewModel(new MenuDefinition(name.Trim(), []));
        menu.PropertyChanged += OnMenuPropertyChanged;
        AttachValidation(menu);
        Menus.Add(menu);
        SelectedMenu = menu;
    }

    private bool CanAddMenu() => !string.IsNullOrWhiteSpace(_projectPath);

    [RelayCommand(CanExecute = nameof(HasSelectedMenu))]
    private async Task RenameMenuAsync()
    {
        if (SelectedMenu is null)
            return;

        var name = await _inputDialog.PromptAsync("Rename menu", "Menu name:").ConfigureAwait(true);
        if (string.IsNullOrWhiteSpace(name))
            return;

        SelectedMenu.Name = name.Trim();
    }

    [RelayCommand(CanExecute = nameof(HasSelectedMenu))]
    private void DeleteMenu()
    {
        if (SelectedMenu is null)
            return;

        Menus.Remove(SelectedMenu);
        SelectedMenu = Menus.FirstOrDefault();
    }

    [RelayCommand(CanExecute = nameof(HasSelectedMenu))]
    private void AddItem()
    {
        if (SelectedMenu is null)
            return;

        var item = new MenuItemViewModel(new MenuItemDefinition("New item", MenuLinkType.Ref, null, null, false, []), null);
        AttachValidation(item);
        SelectedMenu.Items.Add(item);
        SelectedItem = item;
    }

    [RelayCommand(CanExecute = nameof(HasSelectedItem))]
    private void DeleteItem()
    {
        if (SelectedItem is null || SelectedMenu is null)
            return;

        RemoveItemFrom(SelectedItem, SelectedMenu.Items);
        if (SelectedItem.Parent is not null)
            SelectedItem.Parent.Children.Remove(SelectedItem);

        SelectedItem = null;
    }

    [RelayCommand(CanExecute = nameof(CanMoveSelectedItemUp))]
    private void MoveUp()
    {
        if (SelectedItem is null)
            return;

        var container = GetContainer(SelectedItem);
        var index = container.IndexOf(SelectedItem);
        if (index > 0)
        {
            container.Move(index, index - 1);
            OnPropertyChanged(nameof(CanSave));
        }
    }

    private bool CanMoveSelectedItemUp => CanMoveItem(SelectedItem, -1);

    [RelayCommand(CanExecute = nameof(CanMoveSelectedItemDown))]
    private void MoveDown()
    {
        if (SelectedItem is null)
            return;

        var container = GetContainer(SelectedItem);
        var index = container.IndexOf(SelectedItem);
        if (index >= 0 && index < container.Count - 1)
        {
            container.Move(index, index + 1);
            OnPropertyChanged(nameof(CanSave));
        }
    }

    private bool CanMoveSelectedItemDown => CanMoveItem(SelectedItem, 1);

    [RelayCommand(CanExecute = nameof(CanIndentSelectedItem))]
    private void Indent()
    {
        if (SelectedItem is null)
            return;

        var container = GetContainer(SelectedItem);
        var index = container.IndexOf(SelectedItem);
        if (index <= 0)
            return;

        var previous = container[index - 1];
        container.RemoveAt(index);
        SelectedItem.Parent = previous;
        previous.Children.Add(SelectedItem);
        previous.IsExpanded = true;
        OnPropertyChanged(nameof(CanSave));
    }

    private bool CanIndentSelectedItem => CanIndent(SelectedItem);

    [RelayCommand(CanExecute = nameof(CanOutdentSelectedItem))]
    private void Outdent()
    {
        if (SelectedItem is null || SelectedItem.Parent is null || SelectedMenu is null)
            return;

        var parent = SelectedItem.Parent;
        var parentContainer = GetContainer(parent);
        var parentIndex = parentContainer.IndexOf(parent);

        parent.Children.Remove(SelectedItem);
        SelectedItem.Parent = parent.Parent;
        parentContainer.Insert(parentIndex + 1, SelectedItem);
        OnPropertyChanged(nameof(CanSave));
    }

    private bool CanOutdentSelectedItem => SelectedItem?.Parent is not null;

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(_projectPath))
            return;

        try
        {
            var definitions = Menus.Select(m => m.ToDefinition()).ToList();
            await Task.Run(() => _menuService.SaveMenus(_projectPath, definitions)).ConfigureAwait(true);
            StatusMessage = "Menus saved.";
        }
#pragma warning disable CA1031
        catch (Exception ex)
        {
            StatusMessage = $"Save failed: {ex.Message}";
        }
#pragma warning restore CA1031
    }

    /// <summary>
    /// Determines whether <paramref name="draggedItem"/> can be dropped relative to
    /// <paramref name="targetItem"/> at the given <paramref name="position"/>.
    /// </summary>
    public static bool CanDrop(MenuItemViewModel draggedItem, MenuItemViewModel? targetItem, DropPosition position)
    {
        ArgumentNullException.ThrowIfNull(draggedItem);

        if (targetItem is null)
            return true;

        if (IsDescendant(targetItem, draggedItem))
            return false;

        return position != DropPosition.Inside || draggedItem != targetItem;
    }

    /// <summary>
    /// Performs the drop. The view calls this from its drag-and-drop event handlers.
    /// </summary>
    public void Drop(MenuItemViewModel draggedItem, MenuItemViewModel? targetItem, DropPosition position)
    {
        ArgumentNullException.ThrowIfNull(draggedItem);

        if (!CanDrop(draggedItem, targetItem, position))
            return;

        DetachFromParent(draggedItem);

        if (targetItem is null || position == DropPosition.Before)
        {
            var targetContainer = targetItem is null ? SelectedMenu!.Items : GetContainer(targetItem);
            var targetIndex = targetItem is null ? targetContainer.Count : targetContainer.IndexOf(targetItem);
            draggedItem.Parent = targetItem;
            targetContainer.Insert(targetIndex, draggedItem);
        }
        else if (position == DropPosition.After)
        {
            var targetContainer = GetContainer(targetItem);
            var targetIndex = targetContainer.IndexOf(targetItem) + 1;
            draggedItem.Parent = targetItem.Parent;
            targetContainer.Insert(targetIndex, draggedItem);
        }
        else
        {
            draggedItem.Parent = targetItem;
            targetItem.Children.Add(draggedItem);
            targetItem.IsExpanded = true;
        }

        SelectedItem = draggedItem;
        OnPropertyChanged(nameof(CanSave));
    }

    partial void OnSelectedMenuChanged(MenuViewModel? value)
    {
        SelectedItem = null;
        if (value is not null)
        {
            value.PropertyChanged += OnMenuPropertyChanged;
            AttachValidation(value);
            foreach (var item in value.Items.DescendantsAndSelf())
                AttachValidation(item);
        }
    }

    partial void OnSelectedItemChanged(MenuItemViewModel? value)
    {
        if (value is not null)
            AttachValidation(value);
    }

    private void OnMenuPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MenuViewModel.Name) || e.PropertyName == nameof(MenuViewModel.HasError))
            OnPropertyChanged(nameof(CanSave));
    }

    private void AttachValidation(MenuViewModel menu)
    {
        menu.PropertyChanged -= OnMenuPropertyChanged;
        menu.PropertyChanged += OnMenuPropertyChanged;
    }

    private void AttachValidation(MenuItemViewModel item)
    {
        item.ValidationRequested -= OnItemValidationRequested;
        item.ValidationRequested += OnItemValidationRequested;
        ValidateItem(item);
    }

    private void OnItemValidationRequested()
    {
        OnPropertyChanged(nameof(CanSave));
    }

    private static void ValidateItem(MenuItemViewModel item)
    {
        if (string.IsNullOrWhiteSpace(item.Title))
        {
            item.Error = "Title is required.";
            return;
        }

        if (item.LinkType == MenuLinkType.Ref && string.IsNullOrWhiteSpace(item.Ref))
        {
            item.Error = "Ref is required.";
            return;
        }

        if (item.LinkType == MenuLinkType.Url && string.IsNullOrWhiteSpace(item.Url))
        {
            item.Error = "URL is required.";
            return;
        }

        item.Error = null;
    }

    private static bool RemoveItemFrom(MenuItemViewModel item, ObservableCollection<MenuItemViewModel> collection)
    {
        foreach (var child in collection)
        {
            if (child == item)
            {
                collection.Remove(item);
                return true;
            }

            if (RemoveItemFrom(item, child.Children))
                return true;
        }

        return false;
    }

    private ObservableCollection<MenuItemViewModel> GetContainer(MenuItemViewModel item)
    {
        if (item.Parent is not null)
            return item.Parent.Children;

        return SelectedMenu?.Items ?? new ObservableCollection<MenuItemViewModel>();
    }

    private bool CanMoveItem(MenuItemViewModel? item, int direction)
    {
        if (item is null || SelectedMenu is null)
            return false;

        var container = GetContainer(item);
        var index = container.IndexOf(item);
        return direction < 0 ? index > 0 : index >= 0 && index < container.Count - 1;
    }

    private bool CanIndent(MenuItemViewModel? item)
    {
        if (item is null || SelectedMenu is null)
            return false;

        var container = item.Parent?.Children ?? SelectedMenu.Items;
        var index = container.IndexOf(item);
        return index > 0;
    }

    private static bool IsDescendant(MenuItemViewModel candidate, MenuItemViewModel ancestor)
    {
        var current = candidate.Parent;
        while (current is not null)
        {
            if (current == ancestor)
                return true;
            current = current.Parent;
        }

        return false;
    }

    private void DetachFromParent(MenuItemViewModel item)
    {
        var container = item.Parent?.Children ?? SelectedMenu?.Items;
        container?.Remove(item);
    }
}
