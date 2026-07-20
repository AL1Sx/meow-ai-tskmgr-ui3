using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace meow_ai_tskmgr_ui3.Helpers;

public static class WindowHelper
{
    private static readonly List<Window> _activeWindows = new();

    public static IReadOnlyList<Window> ActiveWindows => _activeWindows;

    public static void TrackWindow(Window window)
    {
        _activeWindows.Add(window);
        window.Closed += (sender, args) => _activeWindows.Remove(window);
    }

    public static Frame? GetRootFrame(Window window)
    {
        if (window.Content is Grid grid)
        {
            foreach (var child in grid.Children)
            {
                if (child is Frame frame)
                    return frame;
            }
        }
        return null;
    }
}
