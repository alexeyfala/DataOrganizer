using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DataOrganizer.Interfaces.Storage;
using Serilog;

namespace DataOrganizer.ViewModels;

/// <summary>
/// View model for <c>NoticeWindow</c>.
/// </summary>
internal sealed partial class NoticeViewModel : ObservableObject
{
	#region Properties
	/// <summary>
	/// Caption of the button that reveals <see cref="FilePath" />.
	/// </summary>
	[ObservableProperty]
	public partial string? ActionCaption { get; set; }

	/// <summary>
	/// Path shown under the message.
	/// </summary>
	[ObservableProperty]
	public partial string? FilePath { get; set; }

	/// <summary>
	/// <c>True</c> keeps the notice above the windows of other applications.
	/// </summary>
	[ObservableProperty]
	public partial bool IsTopmost { get; set; }

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

	#region Commands
	/// <summary>
	/// Opens the directory holding <see cref="FilePath" /> and selects the file in it.
	/// </summary>
	[RelayCommand]
	private void RevealFile()
	{
		if (FilePath is not { Length: > 0 } filePath)
		{
			return;
		}

		_directoryAccessor.RevealFile(filePath, _logger);
	}
	#endregion

	#region Data
	/// <inheritdoc cref="IDirectoryAccessor" />
	private readonly IDirectoryAccessor _directoryAccessor;

	/// <inheritdoc cref="ILogger" />
	private readonly ILogger _logger;
	#endregion

	#region Constructors
	public NoticeViewModel(
		IDirectoryAccessor directoryAccessor,
		ILogger logger)
	{
		_directoryAccessor = directoryAccessor;

		_logger = logger;
	}
	#endregion
}
