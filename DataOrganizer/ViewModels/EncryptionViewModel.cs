using CommunityToolkit.Mvvm.ComponentModel;
using DataOrganizer.Views;

namespace DataOrganizer.ViewModels;

/// <summary>
/// View model for <see cref="EncryptionView" />.
/// </summary>
public sealed partial class EncryptionViewModel : ObservableObject
{
	#region Auto-Generated Properties
	/// <summary>
	/// Error related to <see cref="MasterPasswordFilePath" />.
	/// </summary>
	[ObservableProperty]
	private string? _masterPasswordFileError;

	/// <summary>
	/// Information related to <see cref="MasterPasswordFilePath" />.
	/// </summary>
	[ObservableProperty]
	private string? _masterPasswordFileInfo;

	[ObservableProperty]
	private string? _masterPasswordFilePath;
	#endregion
}
