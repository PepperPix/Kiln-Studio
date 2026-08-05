namespace Kiln.Studio.ViewModels;

using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Services;

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
    [NotifyCanExecuteChangedFor(nameof(RenameMenuCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteMenuCommand))]
    [NotifyCanExecuteChangedFor(nameof(AddItemCommand))]
    [NotifyCanExecuteChangedFor(nameof(MoveUpCommand))]
    [NotifyCanExecuteChangedFor(nameof(MoveDownCommand))]
    [NotifyCanExecuteChangedFor(nameof(IndentCommand))]
    [NotifyCanExecuteChangedFor(nameof(OutdentCommand))]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private MenuViewModel? _selectedMenu;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSave))]
    [NotifyPropertyChangedFor(nameof(HasSelectedItem))]
    [NotifyPropertyChangedFor(nameof(SelectedItemIsRef))]
    [NotifyPropertyChangedFor(nameof(SelectedItemIsUrl))]
    [NotifyCanExecuteChangedFor(nameof(DeleteItemCommand))]
    [NotifyCanExecuteChangedFor(nameof(MoveUpCommand))]
    [NotifyCanExecuteChangedFor(nameof(MoveDownCommand))]
    [NotifyCanExecuteChangedFor(nameof(IndentCommand))]
    [NotifyCanExecuteChangedFor(nameof(OutdentCommand))]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
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

        foreach (var menu in Menus)
        {
            AttachValidation(menu);
            foreach (var item in menu.Items.DescendantsAndSelf())
                AttachValidation(item);
        }

        SelectedMenu = Menus.FirstOrDefault();
        SelectedItem = null;

        RefSuggestions.Clear();
        foreach (var r in _menuRefProvider.GetCollectionRefs(projectPath))
            RefSuggestions.Add(r);
        foreach (var r in _menuRefProvider.GetItemRefs(projectPath))
            RefSuggestions.Add(r);

        AddMenuCommand.NotifyCanExecuteChanged();
        SaveCommand.NotifyCanExecuteChanged();
    }

    public void ClearProject()
    {
        DetachAllValidation();
        _projectPath = null;
        Menus.Clear();
        SelectedMenu = null;
        SelectedItem = null;
        RefSuggestions.Clear();
        StatusMessage = null;
        AddMenuCommand.NotifyCanExecuteChanged();
        SaveCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand(CanExecute = nameof(CanAddMenu))]
    private async Task AddMenuAsync()
    {
        var name = await _inputDialog.PromptAsync("New menu", "Menu name:").ConfigureAwait(true);
        if (string.IsNullOrWhiteSpace(name))
            return;

        var menu = new MenuViewModel(new MenuDefinition(name.Trim(), []));
        AttachValidation(menu);
        Menus.Add(menu);
        SelectedMenu = menu;
        SaveCommand.NotifyCanExecuteChanged();
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

        DetachValidation(SelectedMenu);
        foreach (var item in SelectedMenu.Items.DescendantsAndSelf())
            DetachValidation(item);

        Menus.Remove(SelectedMenu);
        SelectedMenu = Menus.FirstOrDefault();
        SaveCommand.NotifyCanExecuteChanged();
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
        SaveCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand(CanExecute = nameof(HasSelectedItem))]
    private void DeleteItem()
    {
        var selectedItem = SelectedItem;
        if (selectedItem is null || SelectedMenu is null)
            return;

        DetachValidation(selectedItem);
        foreach (var child in selectedItem.Children.DescendantsAndSelf())
            DetachValidation(child);

        RemoveItemFrom(selectedItem, SelectedMenu.Items);
        SelectedItem = null;
        SaveCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand(CanExecute = nameof(CanMoveSelectedItemUp))]
    private void MoveUp()
    {
        var selectedItem = SelectedItem;
        if (selectedItem is null)
            return;

        var container = GetContainer(selectedItem);
        var index = container.IndexOf(selectedItem);
        if (index > 0)
        {
            container.Move(index, index - 1);
            NotifyMoveCommandCanExecuteChanged();
            OnPropertyChanged(nameof(CanSave));
        }
    }

    private bool CanMoveSelectedItemUp => CanMoveItem(SelectedItem, -1);

    [RelayCommand(CanExecute = nameof(CanMoveSelectedItemDown))]
    private void MoveDown()
    {
        var selectedItem = SelectedItem;
        if (selectedItem is null)
            return;

        var container = GetContainer(selectedItem);
        var index = container.IndexOf(selectedItem);
        if (index >= 0 && index < container.Count - 1)
        {
            container.Move(index, index + 1);
            NotifyMoveCommandCanExecuteChanged();
            OnPropertyChanged(nameof(CanSave));
        }
    }

    private bool CanMoveSelectedItemDown => CanMoveItem(SelectedItem, 1);

    [RelayCommand(CanExecute = nameof(CanIndentSelectedItem))]
    private void Indent()
    {
        var selectedItem = SelectedItem;
        if (selectedItem is null)
            return;

        var container = GetContainer(selectedItem);
        var index = container.IndexOf(selectedItem);
        if (index <= 0)
            return;

        var previous = container[index - 1];
        container.RemoveAt(index);
        selectedItem.Parent = previous;
        previous.Children.Add(selectedItem);
        previous.IsExpanded = true;
        NotifyMoveCommandCanExecuteChanged();
        OnPropertyChanged(nameof(CanSave));
    }

    private bool CanIndentSelectedItem => CanIndent(SelectedItem);

    [RelayCommand(CanExecute = nameof(CanOutdentSelectedItem))]
    private void Outdent()
    {
        var selectedItem = SelectedItem;
        if (selectedItem is null || selectedItem.Parent is null || SelectedMenu is null)
            return;

        var parent = selectedItem.Parent;
        var parentContainer = GetContainer(parent);
        var parentIndex = parentContainer.IndexOf(parent);

        parent.Children.Remove(selectedItem);
        selectedItem.Parent = parent.Parent;
        parentContainer.Insert(parentIndex + 1, selectedItem);
        NotifyMoveCommandCanExecuteChanged();
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
#pragma warning disable CA1031 // Save errors should surface in the menu editor status banner instead of crashing the view.
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
        => MenuEditorDragService.CanDrop(draggedItem, targetItem, position);

    /// <summary>
    /// Performs the drop. The view calls this from its drag-and-drop event handlers.
    /// </summary>
    public void Drop(MenuItemViewModel draggedItem, MenuItemViewModel? targetItem, DropPosition position)
    {
        ArgumentNullException.ThrowIfNull(draggedItem);

        if (!CanDrop(draggedItem, targetItem, position))
            return;

        DetachFromParent(draggedItem);

        var rootItems = SelectedMenu?.Items ?? throw new InvalidOperationException("No menu is selected.");
        var (container, index) = MenuEditorDragService.ResolveDropLocation(draggedItem, targetItem, position, rootItems);

        draggedItem.Parent = position == DropPosition.Inside
            ? targetItem
            : targetItem?.Parent;

        container.Insert(index, draggedItem);

        if (position == DropPosition.Inside && targetItem is not null)
            targetItem.IsExpanded = true;

        SelectedItem = draggedItem;
        NotifyMoveCommandCanExecuteChanged();
        OnPropertyChanged(nameof(CanSave));
        SaveCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedMenuChanged(MenuViewModel? value)
    {
        SelectedItem = null;
        if (value is not null)
        {
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
        {
            OnPropertyChanged(nameof(CanSave));
            SaveCommand.NotifyCanExecuteChanged();
        }
    }

    private void AttachValidation(MenuViewModel menu)
    {
        DetachValidation(menu);
        menu.PropertyChanged += OnMenuPropertyChanged;
    }

    private void AttachValidation(MenuItemViewModel item)
    {
        DetachValidation(item);
        item.ValidationRequested += OnItemValidationRequested;
        ValidateItem(item);
    }

    private void DetachValidation(MenuViewModel menu)
    {
        menu.PropertyChanged -= OnMenuPropertyChanged;
    }

    private void DetachValidation(MenuItemViewModel item)
    {
        item.ValidationRequested -= OnItemValidationRequested;
    }

    private void DetachAllValidation()
    {
        foreach (var menu in Menus)
        {
            DetachValidation(menu);
            foreach (var item in menu.Items.DescendantsAndSelf())
                DetachValidation(item);
        }
    }

    private void OnItemValidationRequested()
    {
        OnPropertyChanged(nameof(CanSave));
        SaveCommand.NotifyCanExecuteChanged();
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

        return SelectedMenu?.Items
            ?? throw new InvalidOperationException("No menu is selected.");
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

    private void NotifyMoveCommandCanExecuteChanged()
    {
        MoveUpCommand.NotifyCanExecuteChanged();
        MoveDownCommand.NotifyCanExecuteChanged();
        IndentCommand.NotifyCanExecuteChanged();
        OutdentCommand.NotifyCanExecuteChanged();
    }

    private void DetachFromParent(MenuItemViewModel item)
    {
        var container = item.Parent?.Children ?? SelectedMenu?.Items;
        container?.Remove(item);
    }
}
