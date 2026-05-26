using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Xaml.Interactivity;
using System;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;

namespace DataOrganizer.Behaviors;

/// <summary>
/// Observes <see cref="TextBox.Text" /> of the associated <see cref="TextBox" /> and exposes
/// a boolean validity flag.
/// </summary>
internal sealed class PasswordValidityBehavior : Behavior<TextBox>
{
	#region Properties
	/// <summary>
	/// <c>True</c> when the current <see cref="TextBox.Text" /> passes validation
	/// (not empty/whitespace, no leading or trailing space).
	/// </summary>
	public bool IsValid
	{
		get => GetValue(IsValidProperty);
		set => SetValue(IsValidProperty, value);
	}
	#endregion

	#region Styled Properties
	/// <summary>
	/// Identifies the <see cref="IsValid" /> avalonia property.
	/// </summary>
	public static readonly StyledProperty<bool> IsValidProperty = AvaloniaProperty.Register<PasswordValidityBehavior, bool>(
		name: nameof(IsValid),
		defaultBindingMode: BindingMode.OneWayToSource);
	#endregion

	#region Data
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
	/// <see cref="TextBox.TextProperty" /> changed handler.
	/// </summary>
	private void TextProperty_Changed(string? value)
	{
		const char space = ' ';

		IsValid = !string.IsNullOrWhiteSpace(value)
			&& !value.StartsWith(space)
			&& !value.EndsWith(space);
	}
	#endregion
}
