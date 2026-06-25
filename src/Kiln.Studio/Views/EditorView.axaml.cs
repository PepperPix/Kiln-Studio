namespace Kiln.Studio.Views;

using System.ComponentModel;
using Avalonia.Controls;
using AvaloniaEdit;
using Kiln.Studio.ViewModels;

public partial class EditorView : UserControl
{
    private EditorViewModel? _currentVm;
    private bool _isSyncing;

    public EditorView()
    {
        InitializeComponent();
        BodyEditor.TextChanged += OnBodyEditorTextChanged;
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (_currentVm is not null)
            _currentVm.PropertyChanged -= OnViewModelPropertyChanged;

        _currentVm = DataContext as EditorViewModel;

        if (_currentVm is not null)
        {
            _currentVm.PropertyChanged += OnViewModelPropertyChanged;
            SyncEditorFromVm(_currentVm);
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(EditorViewModel.Body) && !_isSyncing && _currentVm is not null)
        {
            _isSyncing = true;
            BodyEditor.Text = _currentVm.Body;
            _isSyncing = false;
        }
    }

    private void OnBodyEditorTextChanged(object? sender, EventArgs e)
    {
        if (_isSyncing || _currentVm is null)
            return;

        _isSyncing = true;
        _currentVm.Body = BodyEditor.Text;
        _isSyncing = false;
    }

    private void SyncEditorFromVm(EditorViewModel vm)
    {
        _isSyncing = true;
        BodyEditor.Text = vm.Body;
        _isSyncing = false;
    }
}
