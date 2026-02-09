using CommunityToolkit.Mvvm.ComponentModel;
using DataOrganizer.Abstract;
using DataOrganizer.Views;

namespace DataOrganizer.ViewModels;

/// <summary>
/// View model for <see cref="MultilineTextEditView" />.
/// </summary>
public sealed partial class MultilineTextEditViewModel : DefaultButtonViewModelBase
{
	#region Auto-Generated Properties
	/// <summary>
	/// Text.
	/// </summary>
	[ObservableProperty]
	private string? _text;
	#endregion

	#region Methods
	/// <inheritdoc cref="" />
	protected override bool CanExecuteDefaultPressed() => true;
	#endregion
}
