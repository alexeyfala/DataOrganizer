using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Xaml.Interactivity;
using DataOrganizer.Enums;
using DataOrganizer.Helpers.Security;
using System;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;

namespace DataOrganizer.Behaviors;

/// <summary>
/// Observes <see cref="TextBox.Text" /> of the associated <see cref="TextBox" /> and of the
/// confirmation input, and exposes the outcome as validity flags and a strength rating.
/// </summary>
internal sealed class PasswordValidityBehavior : Behavior<TextBox>
{
	#region Properties
	/// <summary>
	/// The input the password is repeated in; <c>null</c> when no confirmation is shown.
	/// </summary>
	public TextBox? ConfirmationInput
	{
		get => GetValue(ConfirmationInputProperty);
		set => SetValue(ConfirmationInputProperty, value);
	}

	/// <summary>
	/// <c>True</c> while the confirmation input holds something other than the password.
	/// </summary>
	public bool IsConfirmationMismatched
	{
		get => GetValue(IsConfirmationMismatchedProperty);
		set => SetValue(IsConfirmationMismatchedProperty, value);
	}

	/// <summary>
	/// <c>True</c> when the password is being set, so it is confirmed and measured against
	/// <see cref="MinimumLength" />.
	/// </summary>
	public bool IsConfirmationRequired
	{
		get => GetValue(IsConfirmationRequiredProperty);
		set => SetValue(IsConfirmationRequiredProperty, value);
	}

	/// <summary>
	/// <c>True</c> when the password input alone satisfies the policy, the confirmation aside.
	/// </summary>
	public bool IsPasswordAccepted
	{
		get => GetValue(IsPasswordAcceptedProperty);
		set => SetValue(IsPasswordAcceptedProperty, value);
	}

	/// <summary>
	/// <c>True</c> while the password input holds fewer characters than <see cref="MinimumLength" />.
	/// </summary>
	public bool IsPasswordTooShort
	{
		get => GetValue(IsPasswordTooShortProperty);
		set => SetValue(IsPasswordTooShortProperty, value);
	}

	/// <summary>
	/// <c>True</c> when the current <see cref="TextBox.Text" /> passes validation
	/// (not empty/whitespace, no leading or trailing space).
	/// </summary>
	public bool IsValid
	{
		get => GetValue(IsValidProperty);
		set => SetValue(IsValidProperty, value);
	}

	/// <summary>
	/// Least number of characters a new password is accepted with.
	/// </summary>
	public int MinimumLength
	{
		get => GetValue(MinimumLengthProperty);
		set => SetValue(MinimumLengthProperty, value);
	}

	/// <summary>
	/// Message reported on the confirmation input while it disagrees with the password.
	/// </summary>
	public string? MismatchMessage
	{
		get => GetValue(MismatchMessageProperty);
		set => SetValue(MismatchMessageProperty, value);
	}

	/// <summary>
	/// Rating of the password being set; <see cref="PasswordStrength.None" /> while an existing
	/// password is entered, as rating that one says nothing.
	/// </summary>
	public PasswordStrength Strength
	{
		get => GetValue(StrengthProperty);
		set => SetValue(StrengthProperty, value);
	}

	/// <summary>
	/// Message reported on the password input while it is shorter than <see cref="MinimumLength" />.
	/// </summary>
	public string? TooShortMessage
	{
		get => GetValue(TooShortMessageProperty);
		set => SetValue(TooShortMessageProperty, value);
	}
	#endregion

	#region Styled Properties
	/// <summary>
	/// Identifies the <see cref="ConfirmationInput" /> avalonia property.
	/// </summary>
	public static readonly StyledProperty<TextBox?> ConfirmationInputProperty = AvaloniaProperty
		.Register<PasswordValidityBehavior, TextBox?>(name: nameof(ConfirmationInput));

	/// <summary>
	/// Identifies the <see cref="IsConfirmationMismatched" /> avalonia property.
	/// </summary>
	public static readonly StyledProperty<bool> IsConfirmationMismatchedProperty = AvaloniaProperty
		.Register<PasswordValidityBehavior, bool>(
			name: nameof(IsConfirmationMismatched),
			defaultBindingMode: BindingMode.OneWayToSource);

	/// <summary>
	/// Identifies the <see cref="IsConfirmationRequired" /> avalonia property.
	/// </summary>
	public static readonly StyledProperty<bool> IsConfirmationRequiredProperty = AvaloniaProperty
		.Register<PasswordValidityBehavior, bool>(name: nameof(IsConfirmationRequired));

	/// <summary>
	/// Identifies the <see cref="IsPasswordAccepted" /> avalonia property.
	/// </summary>
	public static readonly StyledProperty<bool> IsPasswordAcceptedProperty = AvaloniaProperty
		.Register<PasswordValidityBehavior, bool>(
			name: nameof(IsPasswordAccepted),
			defaultBindingMode: BindingMode.OneWayToSource);

	/// <summary>
	/// Identifies the <see cref="IsPasswordTooShort" /> avalonia property.
	/// </summary>
	public static readonly StyledProperty<bool> IsPasswordTooShortProperty = AvaloniaProperty
		.Register<PasswordValidityBehavior, bool>(
			name: nameof(IsPasswordTooShort),
			defaultBindingMode: BindingMode.OneWayToSource);

	/// <summary>
	/// Identifies the <see cref="IsValid" /> avalonia property.
	/// </summary>
	public static readonly StyledProperty<bool> IsValidProperty = AvaloniaProperty.Register<PasswordValidityBehavior, bool>(
		name: nameof(IsValid),
		defaultBindingMode: BindingMode.OneWayToSource);

	/// <summary>
	/// Identifies the <see cref="MinimumLength" /> avalonia property.
	/// </summary>
	public static readonly StyledProperty<int> MinimumLengthProperty = AvaloniaProperty
		.Register<PasswordValidityBehavior, int>(name: nameof(MinimumLength));

	/// <summary>
	/// Identifies the <see cref="MismatchMessage" /> avalonia property.
	/// </summary>
	public static readonly StyledProperty<string?> MismatchMessageProperty = AvaloniaProperty
		.Register<PasswordValidityBehavior, string?>(name: nameof(MismatchMessage));

	/// <summary>
	/// Identifies the <see cref="Strength" /> avalonia property.
	/// </summary>
	public static readonly StyledProperty<PasswordStrength> StrengthProperty = AvaloniaProperty
		.Register<PasswordValidityBehavior, PasswordStrength>(
			name: nameof(Strength),
			defaultBindingMode: BindingMode.OneWayToSource);

	/// <summary>
	/// Identifies the <see cref="TooShortMessage" /> avalonia property.
	/// </summary>
	public static readonly StyledProperty<string?> TooShortMessageProperty = AvaloniaProperty
		.Register<PasswordValidityBehavior, string?>(name: nameof(TooShortMessage));
	#endregion

	#region Data
	/// <summary>
	/// Subscription to the confirmation input, replaced whenever that input changes.
	/// </summary>
	private readonly SerialDisposable _confirmationSubscription = new();

	/// <inheritdoc cref="CompositeDisposable" />
	private readonly CompositeDisposable _disposables = [];
	#endregion

	#region Methods
	/// <inheritdoc />
	protected override void OnAttached()
	{
		base.OnAttached();

		if (AssociatedObject is null)
		{
			return;
		}

		AssociatedObject
			.GetObservable(TextBox.TextProperty)
			.Subscribe(TextProperty_Changed)
			.DisposeWith(_disposables);

		this.GetObservable(ConfirmationInputProperty)
			.Subscribe(ConfirmationInput_Changed)
			.DisposeWith(_disposables);

		this.GetObservable(IsConfirmationRequiredProperty)
			.Subscribe(IsConfirmationRequired_Changed)
			.DisposeWith(_disposables);

		_confirmationSubscription.DisposeWith(_disposables);
	}

	/// <inheritdoc />
	protected override void OnDetaching()
	{
		base.OnDetaching();

		_disposables.Dispose();
	}
	#endregion

	#region Event Handlers
	/// <summary>
	/// <see cref="ConfirmationInputProperty" /> changed handler.
	/// </summary>
	private void ConfirmationInput_Changed(TextBox? input)
	{
		_confirmationSubscription.Disposable = input?
			.GetObservable(TextBox.TextProperty)
			.Subscribe(TextProperty_Changed);

		Validate();
	}

	/// <summary>
	/// <see cref="IsConfirmationRequiredProperty" /> changed handler.
	/// </summary>
	private void IsConfirmationRequired_Changed(bool _) => Validate();

	/// <summary>
	/// <see cref="TextBox.TextProperty" /> changed handler.
	/// </summary>
	private void TextProperty_Changed(string? _) => Validate();
	#endregion

	#region Helpers
	/// <summary>
	/// Raises or clears the validation state of an input, which drives its error styling.
	/// </summary>
	private static void Report(
		TextBox? input,
		bool hasError,
		string? message)
	{
		if (input is null)
		{
			return;
		}

		DataValidationErrors.SetError(input, hasError ? new DataValidationException(message) : null);
	}

	/// <summary>
	/// Recomputes the flags from both inputs; the confirmation and the minimum length
	/// only weigh in while a new password is being set.
	/// </summary>
	private void Validate()
	{
		const char space = ' ';

		string? password = AssociatedObject?.Text;

		string? confirmation = ConfirmationInput?.Text;

		bool isAccepted = !string.IsNullOrWhiteSpace(password)
			&& !password.StartsWith(space)
			&& !password.EndsWith(space);

		bool isConfirmed = string.Equals(
			password,
			confirmation,
			StringComparison.Ordinal);

		IsPasswordTooShort = IsConfirmationRequired
			&& password is { Length: > 0 }
			&& password.Length < MinimumLength;

		IsConfirmationMismatched = IsConfirmationRequired
			&& confirmation is { Length: > 0 }
			&& !isConfirmed;

		IsPasswordAccepted = isAccepted && (!IsConfirmationRequired || password!.Length >= MinimumLength);

		IsValid = IsPasswordAccepted && (!IsConfirmationRequired || isConfirmed);

		Strength = IsConfirmationRequired
			? PasswordStrengthEstimator.Estimate(password)
			: PasswordStrength.None;

		Report(
			AssociatedObject,
			IsPasswordTooShort,
			TooShortMessage);

		Report(
			ConfirmationInput,
			IsConfirmationMismatched,
			MismatchMessage);
	}
	#endregion
}
