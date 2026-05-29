using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input.Platform;
using Avalonia.LogicalTree;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using DataOrganizer.Abstract;
using DataOrganizer.Enums;
using DialogHostAvalonia;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;

namespace DataOrganizer.Extensions;

internal static class ApplicationExtensions
{
	#region Methods
	/// <summary>
	/// Closes all windows in the application.
	/// </summary>
	public static void CloseAllWindows(this Application target)
	{
		if (!HasWindows(target, out IReadOnlyList<Window> windows))
		{
			return;
		}

		windows
			.ToList()
			.ForEach(x => x.Close());
	}

	/// <summary>
	/// Searches for a <see cref="TopLevel.Clipboard" /> among windows already running in the application.
	/// </summary>
	public static IClipboard? FindClipboard(this Application target)
	{
		if (HasWindows(target, out IReadOnlyList<Window> windows))
		{
			return windows[0].Clipboard;
		}

		return null;
	}

	/// <inheritdoc cref="FindDataContext{T}(Application, out Window?)" />
	public static T? FindDataContext<T>(this Application target) => target.FindDataContext<T>(out _);

	/// <summary>
	/// Searches for a <see cref="StyledElement.DataContext" /> of <see cref="Window" />.
	/// </summary>
	public static T? FindDataContext<T>(this Application target, out Window? window)
	{
		window = null;

		if (HasWindows(target, out IReadOnlyList<Window> windows))
		{
			foreach (Window wnd in windows)
			{
				if (wnd.DataContext is T dataContext)
				{
					window = wnd;

					return dataContext;
				}
			}
		}

		return default;
	}

	/// <summary>
	/// Searches for a <see cref="DialogHost" /> in window among those already running in the application.
	/// </summary>
	public static DialogHost? FindDialogHost(this Application target)
	{
		return FindWindow<Window>(target, x => x.DataContext is ViewModelBase)
			.FindLogicalDescendantOfType<DialogHost>();
	}

	/// <summary>
	/// Searches for a <see cref="TopLevel.StorageProvider" /> among windows already running in the application.
	/// </summary>
	public static IStorageProvider? FindStorageProvider(this Application target)
	{
		if (HasWindows(target, out IReadOnlyList<Window> windows))
		{
			return windows[0].StorageProvider;
		}

		return null;
	}

	/// <summary>
	/// Searches for a window by type among those already running in the application.
	/// </summary>
	public static T? FindWindow<T>(this Application target) where T : Window
	{
		return HasWindows(target, out IReadOnlyList<Window> windows)
			? windows.OfType<T>().FirstOrDefault()
			: null;
	}

	/// <summary>
	/// Searches for a window by type that meets a condition among those already running in the application.
	/// </summary>
	public static T? FindWindow<T>(this Application target, Predicate<T> condition) where T : Window
	{
		return HasWindows(target, out IReadOnlyList<Window> windows)
			? windows.OfType<T>().FirstOrDefault(x => condition(x))
			: null;
	}

	/// <summary>
	/// Returns the application icon as <see cref="WindowIcon" />.
	/// </summary>
	public static WindowIcon GetAppIcon()
	{
		using Bitmap bitmap = GetAppIconAsBitmap();

		return new WindowIcon(bitmap);
	}

	/// <summary>
	/// Returns the application icon as a <see cref="Bitmap" />.
	/// </summary>
	public static Bitmap GetAppIconAsBitmap()
	{
		using Stream stream = GetAppIconAsStream();

		return new(stream);
	}

	/// <summary>
	/// Returns the application icon as a <see cref="Stream" />.
	/// </summary>
	public static Stream GetAppIconAsStream()
	{
		return AssetLoader.Open(new Uri($"avares://DataOrganizer/Assets/Logo.ico"));
	}

	/// <summary>
	/// Returns the current application theme.
	/// </summary>
	public static CurrentTheme GetCurrentTheme(this Application target)
	{
		object key = target
			.ActualThemeVariant
			.Key;

		return key is "Dark"
			? CurrentTheme.Dark
			: CurrentTheme.Light;
	}

	/// <summary>
	/// Determines whether a window of a certain type that meets a condition is running in the application.
	/// </summary>
	public static bool IsAnyWindow<T>(this Application target, Predicate<T> condition) where T : Window
	{
		return HasWindows(target, out IReadOnlyList<Window> windows) && windows
			.OfType<T>()
			.Any(x => x.GetType() == typeof(T) && condition(x));
	}

	/// <summary>
	/// Determines whether a window of a certain type is running in the application.
	/// </summary>
	public static bool IsAnyWindow<T>(this Application target) where T : Window
	{
		return HasWindows(target, out IReadOnlyList<Window> windows) && windows
			.OfType<T>()
			.Any(x => x.GetType() == typeof(T));
	}

	/// <summary>
	/// Returns <c>True</c> if <see cref="Application.ApplicationLifetime" /> is <see cref="IClassicDesktopStyleApplicationLifetime" />.
	/// </summary>
	public static bool IsDesktop(
		this Application target,
		[NotNullWhen(true)] out IClassicDesktopStyleApplicationLifetime? desktop)
	{
		desktop = null;

		if (target.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
		{
			desktop = lifetime;

			return true;
		}

		return false;
	}

	/// <summary>
	/// Returns <c>True</c> if there is opened <see cref="DialogHost" /> dialog.
	/// </summary>
	public static bool IsDialogHostOpened(this Application target)
	{
		return FindDialogHost(target) is { } dialogHost && dialogHost.IsOpen;
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Determines whether the application has any running windows.
	/// </summary>
	private static bool HasWindows(Application target, out IReadOnlyList<Window> windows)
	{
		windows = [];

		if (target.IsDesktop(out var desktop) && desktop.Windows.Count != 0)
		{
			windows = desktop.Windows;

			return true;
		}

		return false;
	}
	#endregion
}
