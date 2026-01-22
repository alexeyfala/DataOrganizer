using CommunityToolkit.Mvvm.ComponentModel;
using DataOrganizer.Views;
using System.Collections;

namespace DataOrganizer.ViewModels;

/// <summary>
/// View model for <see cref="ExecutedFilesView" />.
/// </summary>
public sealed partial class ExecutedFilesViewModel : ObservableObject
{
	#region Auto-Generated Properties
	/// <summary>
	/// A reference to collection with executed in operating system files.
	/// </summary>
	[ObservableProperty]
	private IEnumerable? _executedFiles;
	#endregion
}
