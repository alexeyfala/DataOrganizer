using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using DataOrganizer.Extensions;
using DataOrganizer.Interfaces.Runtime;

namespace DataOrganizer.Services.Views;

/// <summary>
/// The desktop the application runs on.
/// </summary>
public sealed class DesktopAccessor : IAppShutdown
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

	#region Methods
	/// <inheritdoc />
	public void Shutdown()
	{
		if (_app.IsDesktop(out IClassicDesktopStyleApplicationLifetime? desktop))
		{
			desktop.Shutdown();

			return;
		}

		_processTerminator.Terminate();
	}
	#endregion
}
