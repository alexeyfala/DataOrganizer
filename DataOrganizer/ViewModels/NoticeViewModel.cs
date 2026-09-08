using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DataOrganizer.Interfaces;
using Serilog;

namespace DataOrganizer.ViewModels;

/// <summary>
/// View model for <c>StartupErrorWindow</c>.
/// </summary>
internal sealed partial class StartupErrorViewModel : ObservableObject
{
	#region Properties
	/// <summary>
	/// Path to the database file.
	/// </summary>
	[ObservableProperty]
	public partial string? DatabaseFilePath { get; set; }

	/// <summary>
	/// Message.
	/// </summary>
	[ObservableProperty]
	public partial string? Message { get; set; }

	/// <summary>
	/// Title.
	/// </summary>
	[ObservableProperty]
	public partial string? Title { get; set; }
	#endregion

	#region Data
	/// <inheritdoc cref="IDirectoryAccessor" />
	private readonly IDirectoryAccessor _directoryAccessor;

	/// <inheritdoc cref="ILogger" />
	private readonly ILogger _logger;
	#endregion

	#region Constructors
	public StartupErrorViewModel(
		IDirectoryAccessor directoryAccessor,
		ILogger logger)
	{
		_directoryAccessor = directoryAccessor;

		_logger = logger;
	}
	#endregion

	#region Commands
	/// <summary>
	/// Opens the directory holding the database and selects the file in it.
	/// </summary>
	[RelayCommand]
	private void OpenDatabaseFolder()
	{
		if (!(DatabaseFilePath is { Length: > 0 } filePath))
		{
			return;
		}

		_directoryAccessor.RevealFile(filePath, _logger);
	}
	#endregion
}
