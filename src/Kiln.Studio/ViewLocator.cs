using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Kiln.Studio.ViewModels;
using System;

namespace Kiln.Studio;

public class ViewLocator : IDataTemplate
{
    public Control? Build(object? data)
    {
        if (data is null) return null;

        var name = data.GetType().FullName!
            .Replace(".ViewModels.", ".Views.", StringComparison.Ordinal)
            .Replace("ViewModel", "Window", StringComparison.Ordinal);

        var type = Type.GetType(name);
        if (type != null) return (Control)Activator.CreateInstance(type)!;

        return new TextBlock { Text = "View not found: " + name };
    }

    public bool Match(object? data) => data is ViewModelBase;
}
