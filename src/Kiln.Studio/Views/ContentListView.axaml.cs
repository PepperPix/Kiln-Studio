using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Kiln.Studio.Views;

public partial class ContentListView : UserControl
{
    public ContentListView()
    {
        InitializeComponent();

        // Avalonia's ListBoxItem selects the item under the pointer on ANY mouse button press,
        // including right-click - so a right-click meant only to open the context menu (see
        // Grid.ContextMenu below) also fires SelectedItem/SelectedEntry, which ShellViewModel
        // reacts to by opening the item in the editor. Intercepting at the Tunnel routing phase
        // (root -> leaf, runs before ListBoxItem's own Bubble-phase selection handler) and marking
        // the event Handled for the right button suppresses the selection change while leaving
        // the separate ContextRequested/ContextMenu-opening event untouched.
        EntryListBox.AddHandler(InputElement.PointerPressedEvent, OnEntryListPointerPressedTunnel, RoutingStrategies.Tunnel);
    }

    private static void OnEntryListPointerPressedTunnel(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(sender as Control).Properties.IsRightButtonPressed)
            e.Handled = true;
    }
}
