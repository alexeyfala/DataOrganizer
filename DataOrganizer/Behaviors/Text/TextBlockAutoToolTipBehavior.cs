using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Xaml.Interactivity;
using System;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;

namespace DataOrganizer.Behaviors;

/// <summary>
/// Trims the associated <see cref="TextBlock" /> with an ellipsis and shows its full text
/// in a tooltip while the text stays trimmed. The tooltip is suppressed while <see cref="Behavior.IsEnabled" />
/// is <c>false</c>.
/// </summary>
internal sealed class TextBlockAutoToolTipBehavior : Behavior<TextBlock>
{
	#region Data
	/// <inheritdoc cref="CompositeDisposable" />
	private readonly CompositeDisposable _disposables = [];
	#endregion

	#region Event Handlers
	/// <summary>
	/// <see cref="Control.SizeChanged" /> handler of <see cref="TextBlock" />.
	/// </summary>
	private void AssociatedObject_SizeChanged(object? sender, SizeChangedEventArgs e) => SetOrRemoveToolTip();

	/// <summary>
	/// <see cref="TextBlock.TextProperty" /> changed handler of <see cref="AssociatedObject" />.
	/// </summary>
	private void AssociatedObject_TextProperty_Changed(string? value) => SetOrRemoveToolTip();

	/// <summary>
	/// <see cref="Behavior.IsEnabledProperty" /> changed handler.
	/// </summary>
	private void IsEnabledProperty_Changed(bool value) => SetOrRemoveToolTip();
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

		AssociatedObject.TextTrimming = TextTrimming.CharacterEllipsis;

		this
			.GetObservable(IsEnabledProperty)
			.Subscribe(IsEnabledProperty_Changed)
			.DisposeWith(_disposables);

		AssociatedObject
			.GetObservable(TextBlock.TextProperty)
			.Subscribe(AssociatedObject_TextProperty_Changed)
			.DisposeWith(_disposables);

		AssociatedObject.SizeChanged += AssociatedObject_SizeChanged;

		Disposable
			.Create(() => AssociatedObject.SizeChanged -= AssociatedObject_SizeChanged)
			.DisposeWith(_disposables);
	}

	/// <inheritdoc />
	protected override void OnDetaching()
	{
		base.OnDetaching();

		_disposables.Dispose();
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Sets or removes a <see cref="ToolTip" /> of <see cref="TextBlock" />.
	/// </summary>
	private void SetOrRemoveToolTip()
	{
		if (AssociatedObject is null)
		{
			return;
		}

		if (!IsEnabled)
		{
			if (ToolTip.GetTip(AssociatedObject) is not null)
			{
				ToolTip.SetTip(AssociatedObject, null);
			}

			return;
		}

		bool isTrimmed = AssociatedObject
			.TextLayout
			.TextLines
			.Any(x => x.HasCollapsed);

		if (isTrimmed)
		{
			ToolTip.SetTip(AssociatedObject, AssociatedObject.Text);
		}
		else
		{
			ToolTip.SetTip(AssociatedObject, null);
		}
	}
	#endregion
}
