using AppInstance.Interfaces;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.LogicalTree;
using DataOrganizer.Interfaces.Dialogs;
using DataOrganizer.Interfaces.Views;
using DataOrganizer.Windows;
using DialogHostAvalonia;
using System;
using System.Collections.Generic;
using System.Linq;
using WindowContracts.Interfaces;

namespace DataOrganizer.Services.Views;

/// <summary>
/// The desktop the application runs on: its open windows and its end.
/// </summary>
public sealed class DesktopAccessor : IAppShutdown, IDialogHostFinder, IWindowCloser, IWindowFinder
{
	#region Data
	/// <inheritdoc cref="Application" />
	private readonly Application _app;

	/// <inheritdoc cref="IProcessTerminator" />
	private readonly IProcessTerminator _processTerminator;
	#endregion

	#region Constructors
	public DesktopAccessor(Application app, IProcessTerminator processTerminator)
	{
		_app = app;

		_processTerminator = processTerminator;
	}
	#endregion

	#region Properties
	/// <summary>
	/// The desktop lifetime of the application; <c>null</c> where there is no desktop.
	/// </summary>
	private IClassicDesktopStyleApplicationLifetime? Desktop => _app.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;

	/// <summary>
	/// The open windows, the first opened first.
	/// </summary>
	private IReadOnlyList<Window> Windows => Desktop?.Windows ?? [];
	#endregion

	#region Methods
	/// <inheritdoc />
	public void CloseAllWindows()
	{
		Windows
			.ToList()
			.ForEach(x => x.Close());
	}

	/// <inheritdoc />
	public DialogHost? FindDialogHost(string identifier)
	{
		return Windows
			.SelectMany(x => x.GetLogicalDescendants().OfType<DialogHost>())
			.FirstOrDefault(x => x.Identifier == identifier);
	}

	/// <inheritdoc />
	public Window? FindMainWindow() => FindWindow<Window>(static x => x is IMainWindow);

	/// <inheritdoc />
	public T? FindWindow<T>(Predicate<T>? condition = null) where T : Window
	{
		return Windows
			.OfType<T>()
			.FirstOrDefault(x => condition?.Invoke(x) != false);
	}

	/// <inheritdoc />
	public void Shutdown()
	{
		if (Desktop is { } desktop)
		{
			desktop.Shutdown();

			return;
		}

		_processTerminator.Terminate();
	}
	#endregion
}
