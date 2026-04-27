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

internal sealed class TextBlockAutoToolTipBehavior : Behavior<TextBlock>
{
	#region Properties
	/// <summary>
	/// Returns <c>True</c> when the tooltip should not be displayed.
	/// </summary>
	public bool IsDisabled
	{
		get => GetValue(IsDisabledProperty);
		set => SetValue(IsDisabledProperty, value);
	}
	#endregion

	#region Styled Properties
	/// <summary>
	/// Identifies the <see cref="IsDisabled" /> avalonia property.
	/// </summary>
	public static readonly StyledProperty<bool> IsDisabledProperty = AvaloniaProperty
		.Register<TextBlockAutoToolTipBehavior, bool>(name: nameof(IsDisabled));
	#endregion

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
	/// <see cref="IsDisabledProperty" /> changed handler.
	/// </summary>
	private void IsDisabledProperty_Changed(bool value) => SetOrRemoveToolTip();
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
			.GetObservable(IsDisabledProperty)
			.Subscribe(IsDisabledProperty_Changed)
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

	#region Service
	/// <summary>
	/// Sets or removes a <see cref="ToolTip" /> of <see cref="TextBlock" />.
	/// </summary>
	private void SetOrRemoveToolTip()
	{
		if (AssociatedObject is null)
		{
			return;
		}

		if (IsDisabled)
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
