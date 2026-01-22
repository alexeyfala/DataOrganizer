using CommunityToolkit.Mvvm.ComponentModel;

namespace DataOrganizer.Abstract;

/// <summary>
/// <inheritdoc cref="DefaultButtonViewModelBase" /><br />With <see cref="Text" /> property.
/// </summary>
public abstract partial class TextWithDefaultButtonViewModelBase : DefaultButtonViewModelBase
{
	#region Auto-Generated Properties
	/// <summary>
	/// Text.
	/// </summary>
	[ObservableProperty]
	private string? _text;
	#endregion
}
