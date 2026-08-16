using Avalonia;
using Avalonia.Controls;
using Avalonia.Xaml.Interactivity;
using DataOrganizer.Helpers.Security;
using System;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;

namespace DataOrganizer.Behaviors;

/// <summary>
/// Wipes in place every string the attached <see cref="TextBox" /> replaces.
/// </summary>
/// <remarks>
/// <see cref="TextBox.Text" /> is immutable, so each keystroke leaves the previous value in the heap.
/// The attached input must hold a secret only, and must not bind its text to anything.
/// </remarks>
internal sealed class WipeReplacedTextBehavior : Behavior<TextBox>
{
	#region Data
	/// <inheritdoc cref="CompositeDisposable" />
	private readonly CompositeDisposable _disposables = [];
	#endregion

	#region Event Handlers
	/// <summary>
	/// <see cref="TextBox.TextProperty" /> changed handler.
	/// </summary>
	private void TextProperty_Changed(AvaloniaPropertyChangedEventArgs args)
	{
		if (args.OldValue is not string { Length: > 0 } replaced)
		{
			return;
		}

		// The control already holds the new value, so the replaced one is read by nobody.
		SecureStringHelper.WipeString(replaced);
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	protected override void OnAttached()
	{
		base.OnAttached();

		AssociatedObject?
			.GetPropertyChangedObservable(TextBox.TextProperty)
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
}
