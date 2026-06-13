using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avalonia.Xaml.Interactivity;
using DataOrganizer.Helpers.Clipboard;
using System;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using TheArtOfDev.HtmlRenderer.Avalonia;

namespace DataOrganizer.Behaviors;

/// <summary>
/// Highlights the search query inside the associated <see cref="HtmlLabel" /> and scrolls the
/// enclosing <see cref="ScrollViewer" /> so the first match is brought into view.
/// </summary>
internal sealed class HtmlSearchHighlightBehavior : Behavior<HtmlLabel>
{
	#region Properties
	/// <summary>
	/// Search query to highlight; blank restores the original HTML and scroll position.
	/// </summary>
	public string? Query
	{
		get => GetValue(QueryProperty);
		set => SetValue(QueryProperty, value);
	}

	/// <summary>
	/// Original HTML rendered when no query is active.
	/// </summary>
	public string? SourceHtml
	{
		get => GetValue(SourceHtmlProperty);
		set => SetValue(SourceHtmlProperty, value);
	}
	#endregion

	#region Styled Properties
	/// <summary>
	/// Identifies the <see cref="Query" /> avalonia property.
	/// </summary>
	public static readonly StyledProperty<string?> QueryProperty = AvaloniaProperty
		.Register<HtmlSearchHighlightBehavior, string?>(name: nameof(Query));

	/// <summary>
	/// Identifies the <see cref="SourceHtml" /> avalonia property.
	/// </summary>
	public static readonly StyledProperty<string?> SourceHtmlProperty = AvaloniaProperty
		.Register<HtmlSearchHighlightBehavior, string?>(name: nameof(SourceHtml));
	#endregion

	#region Data
	/// <summary>
	/// Id assigned to the first highlighted match and used as the scroll target.
	/// </summary>
	private const string MatchId = "__match";

	/// <summary>
	/// Gap left above the match when scrolling it into view.
	/// </summary>
	private const double ScrollPadding = 8.0;

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

		AssociatedObject.LoadComplete += OnLoadComplete;

		this
			.GetObservable(SourceHtmlProperty)
			.Subscribe(_ => Rebuild())
			.DisposeWith(_disposables);

		this
			.GetObservable(QueryProperty)
			.Subscribe(_ => Rebuild())
			.DisposeWith(_disposables);
	}

	/// <inheritdoc />
	protected override void OnDetaching()
	{
		base.OnDetaching();

		AssociatedObject?.LoadComplete -= OnLoadComplete;

		_disposables.Dispose();
	}
	#endregion

	#region Event Handlers
	/// <summary>
	/// Scrolls the enclosing viewer to the first match once layout for the new HTML has completed.
	/// </summary>
	private void OnLoadComplete(object? sender, HtmlRendererRoutedEventArgs<EventArgs> e)
	{
		if (AssociatedObject?.FindAncestorOfType<ScrollViewer>() is not { } viewer)
		{
			return;
		}

		double target = !string.IsNullOrWhiteSpace(Query) && AssociatedObject.GetElementRectangle(MatchId) is { } rect
			? Math.Max(0.0, rect.Y - ScrollPadding)
			: 0.0;

		// Defer so the viewer adopts the relaid-out content height before the offset is applied.
		Dispatcher.UIThread.Post(
			() => viewer.Offset = viewer.Offset.WithY(target),
			DispatcherPriority.Background);
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Sets the label HTML: the original when idle, a highlighted copy while searching.
	/// </summary>
	private void Rebuild()
	{
		AssociatedObject?.Text = string.IsNullOrWhiteSpace(Query)
			? SourceHtml
			: HtmlMatchHighlighter.Highlight(SourceHtml, Query, MatchId);
	}
	#endregion
}
