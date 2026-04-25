using Avalonia;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DataOrganizer.DTO;
using DataOrganizer.Interfaces;
using DataOrganizer.Views;
using System.Threading.Tasks;

namespace DataOrganizer.ViewModels;

/// <summary>
/// View model for <see cref="KeyValueInputView" />.
/// </summary>
public sealed partial class KeyValueInputViewModel : BooleanAsyncResultViewModel
{
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

	#region Constructors
	public KeyValueInputViewModel(
		Application app,
		ITaskExceptionHandler handler) : base(app, handler)
	{
	}
	#endregion

	#region Methods
	/// <summary>
	/// Performs initialization.
	/// </summary>
	public void Initialize(KeyValueInputParameters parameters)
	{
		DefaultButtonText = parameters.DefaultButtonText;

		Key = parameters.Key;

		KeyHint = parameters.KeyHint;

		Value = parameters.Value;

		ValueHint = parameters.ValueHint;

		IsValueInputVisible = !string.IsNullOrEmpty(parameters.ValueHint);
	}
	#endregion

	#region Service
	/// <summary>
	/// Validates <see cref="DefaultPressedCommand" />.
	/// </summary>
	private bool CanExecuteDefaultPressed() => !string.IsNullOrWhiteSpace(Key);
	#endregion
}
