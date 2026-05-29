using Avalonia;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DataOrganizer.DTO;
using DataOrganizer.Interfaces;
using System.Threading.Tasks;

namespace DataOrganizer.ViewModels;

/// <summary>
/// View model for <c>KeyValueInputView</c>.
/// </summary>
public sealed partial class KeyValueInputViewModel : BooleanAsyncResultViewModel
{
	#region Properties
	/// <summary>
	/// Text for default button.
	/// </summary>
	[ObservableProperty]
	public partial string? DefaultButtonText { get; set; }

	/// <summary>
	/// Specifies the visibility of the <see cref="Value" /> input field.
	/// </summary>
	[ObservableProperty]
	public partial bool IsValueInputVisible { get; set; }

	/// <summary>
	/// Key.
	/// </summary>
	[ObservableProperty]
	[NotifyCanExecuteChangedFor(nameof(DefaultPressedCommand))]
	public partial string? Key { get; set; }

	/// <summary>
	/// Hint for the input field <see cref="Key" />.
	/// </summary>
	[ObservableProperty]
	public partial string? KeyHint { get; set; }

	/// <summary>
	/// Value.
	/// </summary>
	[ObservableProperty]
	public partial string? Value { get; set; }

	/// <summary>
	/// Hint for the input field <see cref="Value" />.
	/// </summary>
	[ObservableProperty]
	public partial string? ValueHint { get; set; }
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
	[RelayCommand(CanExecute = nameof(CanDefaultPressed))]
	private Task DefaultPressed() => SetResultAsync(true);
	#endregion

	#region Constructors
	public KeyValueInputViewModel(
		Application app,
		ITaskExceptionHandler exceptionHandler) : base(app, exceptionHandler)
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

	#region Helpers
	/// <summary>
	/// Validates <see cref="DefaultPressedCommand" />.
	/// </summary>
	private bool CanDefaultPressed() => !string.IsNullOrWhiteSpace(Key);
	#endregion
}
