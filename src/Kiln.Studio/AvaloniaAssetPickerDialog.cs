namespace Kiln.Studio;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Kiln.Studio.Services;

/// <summary>
/// Hand-built code-behind Window (no XAML, see AvaloniaNewPageDialog/AvaloniaInputDialog) letting
/// the user either browse the site-wide asset library or upload a new file, choosing a
/// destination (ADR-050). The folder browser (breadcrumb + list + "New folder") is a single set
/// of shared controls reused both when browsing the library for an existing file and when
/// choosing a target folder for a library upload.
/// </summary>
internal sealed class AvaloniaAssetPickerDialog : IAssetPickerDialog
{
    private enum Mode
    {
        Library,
        Upload
    }

    private enum UploadDestinationMode
    {
        PageBundle,
        Library
    }

    private readonly IAssetLibraryService _assetLibraryService;
    private readonly IFilePicker _filePicker;

    public AvaloniaAssetPickerDialog(IAssetLibraryService assetLibraryService, IFilePicker filePicker)
    {
        _assetLibraryService = assetLibraryService;
        _filePicker = filePicker;
    }

    public async Task<AssetPickerResult?> ShowAsync(string projectPath, bool canUploadToPageBundle)
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop
            || desktop.MainWindow is null)
            return null;

        AssetPickerResult? result = null;
        var mode = Mode.Library;
        var uploadDestination = canUploadToPageBundle ? UploadDestinationMode.PageBundle : UploadDestinationMode.Library;
        var currentFolder = "";
        string? pickedFilePath = null;

        // ── Mode switch ──────────────────────────────────────────────────
        var libraryModeButton = new RadioButton { Content = "Browse Library", GroupName = "Mode", IsChecked = true };
        var uploadModeButton = new RadioButton { Content = "Upload New", GroupName = "Mode" };
        var modePanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            Children = { libraryModeButton, uploadModeButton }
        };

        // ── Shared folder browser (library browse + upload-to-library target picking) ──
        var breadcrumb = new TextBlock { Text = "static/" };
        var listBox = new ListBox { Height = 220 };
        var newFolderButton = new Button { Content = "New Folder…" };
        var newFolderNameBox = new TextBox { PlaceholderText = "Folder name", IsVisible = false, MinWidth = 160 };
        var createFolderButton = new Button { Content = "Create", IsVisible = false };
        var cancelFolderButton = new Button { Content = "Cancel", IsVisible = false };
        var newFolderRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            Children = { newFolderButton, newFolderNameBox, createFolderButton, cancelFolderButton }
        };
        var folderBrowserPanel = new StackPanel
        {
            Spacing = 4,
            Children = { breadcrumb, listBox, newFolderRow }
        };

        void RefreshList()
        {
            breadcrumb.Text = string.IsNullOrEmpty(currentFolder) ? "static/" : $"static/{currentFolder}/";

            var items = new List<ListBoxItem>();
            if (!string.IsNullOrEmpty(currentFolder))
                items.Add(new ListBoxItem { Content = "⬆ .." });

            foreach (var entry in _assetLibraryService.ListFolder(projectPath, currentFolder))
            {
                items.Add(new ListBoxItem
                {
                    Content = entry.IsFolder ? $"📁 {entry.Name}" : $"📄 {entry.Name}",
                    Tag = entry
                });
            }

            listBox.ItemsSource = items;
        }

        // ── Upload-only controls ─────────────────────────────────────────
        var chooseFileButton = new Button { Content = "Choose file…" };
        var chosenFileText = new TextBlock { Text = "No file selected", Classes = { "muted" } };
        var pageBundleRadio = new RadioButton
        {
            Content = "Insert into this page",
            GroupName = "UploadDestination",
            IsVisible = canUploadToPageBundle,
            IsChecked = canUploadToPageBundle
        };
        var libraryDestRadio = new RadioButton
        {
            Content = "Save to library",
            GroupName = "UploadDestination",
            IsChecked = !canUploadToPageBundle
        };
        var uploadOnlyPanel = new StackPanel
        {
            Spacing = 8,
            Children =
            {
                new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 8,
                    Children = { chooseFileButton, chosenFileText }
                },
                pageBundleRadio,
                libraryDestRadio
            }
        };

        var bodyPanel = new StackPanel
        {
            Spacing = 12,
            Children = { uploadOnlyPanel, folderBrowserPanel }
        };

        var uploadButton = new Button { Content = "Upload", IsEnabled = false, IsVisible = false };
        var cancelButton = new Button { Content = "Cancel", HorizontalAlignment = HorizontalAlignment.Stretch };
        var buttonPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 8,
            Children = { uploadButton, cancelButton }
        };

        var rootPanel = new StackPanel
        {
            Margin = new Thickness(16),
            Spacing = 12,
            Children = { modePanel, bodyPanel, buttonPanel }
        };

        var dialog = new Window
        {
            Title = "Insert Asset",
            Width = 480,
            SizeToContent = SizeToContent.Height,
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = rootPanel
        };

        void UpdateVisibility()
        {
            var isUpload = mode == Mode.Upload;
            uploadOnlyPanel.IsVisible = isUpload;
            uploadButton.IsVisible = isUpload;
            folderBrowserPanel.IsVisible = mode == Mode.Library
                || (isUpload && uploadDestination == UploadDestinationMode.Library);
        }

        libraryModeButton.Click += (_, _) =>
        {
            mode = Mode.Library;
            UpdateVisibility();
        };

        uploadModeButton.Click += (_, _) =>
        {
            mode = Mode.Upload;
            UpdateVisibility();
        };

        pageBundleRadio.Click += (_, _) =>
        {
            uploadDestination = UploadDestinationMode.PageBundle;
            UpdateVisibility();
        };

        libraryDestRadio.Click += (_, _) =>
        {
            uploadDestination = UploadDestinationMode.Library;
            UpdateVisibility();
        };

        listBox.DoubleTapped += (_, _) =>
        {
            if (listBox.SelectedItem is not ListBoxItem { Tag: var tag })
                return;

            if (tag is AssetLibraryEntry entry)
            {
                if (entry.IsFolder)
                {
                    currentFolder = entry.RelativePath;
                    RefreshList();
                }
                else if (mode == Mode.Library)
                {
                    result = new AssetPickerResult(AssetPickerDestination.Library, entry.RelativePath);
                    dialog.Close();
                }
            }
            else
            {
                // ".." entry: navigate up one level.
                var lastSlash = currentFolder.LastIndexOf('/');
                currentFolder = lastSlash < 0 ? "" : currentFolder[..lastSlash];
                RefreshList();
            }
        };

        newFolderButton.Click += (_, _) =>
        {
            newFolderNameBox.Text = "";
            newFolderNameBox.IsVisible = true;
            createFolderButton.IsVisible = true;
            cancelFolderButton.IsVisible = true;
            newFolderNameBox.Focus();
        };

        createFolderButton.Click += (_, _) =>
        {
            var name = newFolderNameBox.Text;
            if (!string.IsNullOrWhiteSpace(name))
            {
                _assetLibraryService.CreateFolder(projectPath, currentFolder, name);
                RefreshList();
            }

            newFolderNameBox.IsVisible = false;
            createFolderButton.IsVisible = false;
            cancelFolderButton.IsVisible = false;
        };

        cancelFolderButton.Click += (_, _) =>
        {
            newFolderNameBox.IsVisible = false;
            createFolderButton.IsVisible = false;
            cancelFolderButton.IsVisible = false;
        };

        chooseFileButton.Click += OnChooseFileClick;

#pragma warning disable VSTHRD100 // must match RoutedEventHandler's void-returning signature
        async void OnChooseFileClick(object? sender, RoutedEventArgs e)
#pragma warning restore VSTHRD100
        {
            var picked = await _filePicker.PickFileAsync("Select file to upload").ConfigureAwait(true);
            if (picked is null)
                return;

            pickedFilePath = picked;
            chosenFileText.Text = Path.GetFileName(picked);
            uploadButton.IsEnabled = true;
        }

        uploadButton.Click += (_, _) =>
        {
            if (pickedFilePath is null)
                return;

            result = uploadDestination == UploadDestinationMode.PageBundle
                ? new AssetPickerResult(AssetPickerDestination.PageBundle, pickedFilePath)
                : new AssetPickerResult(AssetPickerDestination.Library, _assetLibraryService.Upload(projectPath, currentFolder, pickedFilePath));

            dialog.Close();
        };

        cancelButton.Click += (_, _) => dialog.Close();

        dialog.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Escape)
                dialog.Close();
        };

        RefreshList();
        UpdateVisibility();

        await dialog.ShowDialog(desktop.MainWindow).ConfigureAwait(true);
        return result;
    }
}
