using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DataOrganizer.Abstract;
using DataOrganizer.Views;
using System.Threading.Tasks;

namespace DataOrganizer.ViewModels;

/// <summary>
/// View model for <see cref="KeyValueInputView" />.
/// </summary>
public sealed partial class KeyValueInputViewModel : BooleanAsyncResultViewModelBase
{
	#region Properties
	/// <summary>
	/// Returns <c>True</c> if initialization has completed.
	/// </summary>
	/// <remarks>
	/// For test purposes.
	/// </remarks>
	public bool IsInitialized { get; private set; }
	#endregion

	#region Auto-Generated Properties
	/// <summary>
	/// Text for default button.
	/// </summary>
	[ObservableProperty]
	private string? _defaultButtonText;

	/// <summary>
	/// Specifies the visibility of the <see cref="Value" /> input field.
	/// </summary>
	[ObservableProperty]
	private bool _isValueInputVisible;

	/// <summary>
	/// Key.
	/// </summary>
	[ObservableProperty]
	[NotifyCanExecuteChangedFor(nameof(DefaultPressedCommand))]
	private string? _key;

	/// <summary>
	/// Hint for the input field <see cref="Key" />.
	/// </summary>
	[ObservableProperty]
	private string? _keyHint;

	/// <summary>
	/// Value.
	/// </summary>
	[ObservableProperty]
	private string? _value;

	/// <summary>
	/// Hint for the input field <see cref="Value" />.
	/// </summary>
	[ObservableProperty]
	private string? _valueHint;
	#endregion

	#region Auto-Generated Commands
	/// <summary>
	/// Cancel.
	/// </summary>
	[RelayCommand]
	private Task Cancel() => SetResultAsync(false);

	/// <summary>
	/// Handles default button pressed.
	/// </summary>
	[RelayCommand(CanExecute = nameof(CanExecuteDefaultPressed))]
	private Task DefaultPressed() => SetResultAsync(true);
	#endregion

	#region Methods
	/// <summary>
	/// Performs initialization.
	/// </summary>
	public void Initialize(
		string defaultButtonText,
		string? key = null,
		string? keyHint = null,
		string? value = null,
		string? valueHint = null)
	{
		DefaultButtonText = defaultButtonText;

		Key = key;

		KeyHint = keyHint;

		Value = value;

		ValueHint = valueHint;

		IsValueInputVisible = !string.IsNullOrEmpty(valueHint);

		IsInitialized = true;
	}
	#endregion

	#region Service
	/// <summary>
	/// Validates <see cref="DefaultPressedCommand" />.
	/// </summary>
	private bool CanExecuteDefaultPressed() => !string.IsNullOrWhiteSpace(Key);
	#endregion
}
