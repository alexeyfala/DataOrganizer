using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avalonia.Xaml.Interactivity;
using DataOrganizer.Helpers.Clipboard;
using System;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;

namespace DataOrganizer.Behaviors;

/// <summary>
/// Renders <see cref="SourceText" /> in the associated <see cref="TextBlock" />: full text trimmed
/// with an ellipsis while <see cref="Query" /> is blank, or every match highlighted and the first one
/// scrolled into view while a query is active.
/// </summary>
internal sealed class SearchHighlightBehavior : Behavior<TextBlock>
{
	#region Properties
	/// <summary>
	/// Brush painted behind each query match.
	/// </summary>
	public IBrush? HighlightBrush
	{
		get => GetValue(HighlightBrushProperty);
		set => SetValue(HighlightBrushProperty, value);
	}

	/// <summary>
	/// Brush for the text of each query match; pinned so the match stays readable on
	/// <see cref="HighlightBrush" /> regardless of the active theme's text color.
	/// </summary>
	public IBrush? HighlightForeground
	{
		get => GetValue(HighlightForegroundProperty);
		set => SetValue(HighlightForegroundProperty, value);
	}

	/// <summary>
	/// Search query whose matches are highlighted; blank shows the full ellipsis-trimmed text.
	/// </summary>
	public string? Query
	{
		get => GetValue(QueryProperty);
		set => SetValue(QueryProperty, value);
	}

	/// <summary>
	/// Source text rendered into the associated text block.
	/// </summary>
	public string? SourceText
	{
		get => GetValue(SourceTextProperty);
		set => SetValue(SourceTextProperty, value);
	}
	#endregion

	#region Styled Properties
	/// <summary>
	/// Identifies the <see cref="HighlightBrush" /> avalonia property.
	/// </summary>
	public static readonly StyledProperty<IBrush?> HighlightBrushProperty = AvaloniaProperty
		.Register<SearchHighlightBehavior, IBrush?>(name: nameof(HighlightBrush));

	/// <summary>
	/// Identifies the <see cref="HighlightForeground" /> avalonia property.
	/// </summary>
	public static readonly StyledProperty<IBrush?> HighlightForegroundProperty = AvaloniaProperty
		.Register<SearchHighlightBehavior, IBrush?>(name: nameof(HighlightForeground));

	/// <summary>
	/// Identifies the <see cref="Query" /> avalonia property.
	/// </summary>
	public static readonly StyledProperty<string?> QueryProperty = AvaloniaProperty
		.Register<SearchHighlightBehavior, string?>(name: nameof(Query));

	/// <summary>
	/// Identifies the <see cref="SourceText" /> avalonia property.
	/// </summary>
	public static readonly StyledProperty<string?> SourceTextProperty = AvaloniaProperty
		.Register<SearchHighlightBehavior, string?>(name: nameof(SourceText));
	#endregion

	#region Data
	/// <summary>
	/// Vertical gap kept above the first match after scrolling it into view.
	/// </summary>
	private const double MatchTopMargin = 4.0;

	/// <inheritdoc cref="CompositeDisposable" />
	private readonly CompositeDisposable _disposables = [];
	#endregion

	#region Event Handlers
	/// <summary>
	/// <see cref="Visual.AttachedToVisualTree" /> handler: rebuilds now that the <see cref="ScrollViewer" />
	/// ancestor is reachable.
	/// </summary>
	private void OnAttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e) => Rebuild();
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

		this
			.GetObservable(SourceTextProperty)
			.Subscribe(_ => Rebuild())
			.DisposeWith(_disposables);

		this
			.GetObservable(QueryProperty)
			.Subscribe(_ => Rebuild())
			.DisposeWith(_disposables);

		this
			.GetObservable(HighlightBrushProperty)
			.Subscribe(_ => Rebuild())
			.DisposeWith(_disposables);

		this
			.GetObservable(HighlightForegroundProperty)
			.Subscribe(_ => Rebuild())
			.DisposeWith(_disposables);

		// Re-run once the ScrollViewer ancestor is reachable, which a virtualized container lacks at OnAttached.
		AssociatedObject.AttachedToVisualTree += OnAttachedToVisualTree;

		Disposable
			.Create(this, static self => self.AssociatedObject!.AttachedToVisualTree -= self.OnAttachedToVisualTree)
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
	/// Resets the vertical scroll offset to the top.
	/// </summary>
	private static void ResetScroll(ScrollViewer? scroll) => scroll?.Offset = scroll.Offset.WithY(0.0);

	/// <summary>
	/// Applies the resting presentation: vertically centered, ellipsis-trimmed, scrolling disabled.
	/// </summary>
	private static void SetRestingLayout(TextBlock target, ScrollViewer? scroll)
	{
		target.TextTrimming = TextTrimming.CharacterEllipsis;

		target.VerticalAlignment = VerticalAlignment.Center;

		scroll?.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
	}

	/// <summary>
	/// Applies the search presentation: top-aligned, untrimmed, scrollable (bar hidden).
	/// </summary>
	private static void SetSearchLayout(TextBlock target, ScrollViewer? scroll)
	{
		target.TextTrimming = TextTrimming.None;

		target.VerticalAlignment = VerticalAlignment.Top;

		scroll?.VerticalScrollBarVisibility = ScrollBarVisibility.Hidden;
	}

	/// <summary>
	/// Builds the inlines for <paramref name="text" />, wrapping each match of <paramref name="query" />
	/// in a highlighted <see cref="Run" />.
	/// </summary>
	private InlineCollection BuildInlines(string text, string? query)
	{
		InlineCollection inlines = [];

		foreach (SearchHighlight.Segment segment in SearchHighlight.SplitSegments(text, query))
		{
			Run run = new(segment.Text);

			if (segment.IsMatch)
			{
				run.Background = HighlightBrush;

				run.Foreground = HighlightForeground;
			}

			inlines.Add(run);
		}

		return inlines;
	}

	/// <summary>
	/// Rebuilds the associated text block from the current source text and query, switching between the
	/// resting (ellipsis) and search (highlighted, scrollable) presentations.
	/// </summary>
	private void Rebuild()
	{
		if (AssociatedObject is not { } target)
		{
			return;
		}

		ScrollViewer? scroll = target.FindAncestorOfType<ScrollViewer>();

		string text = SourceText?.Trim() ?? string.Empty;

		if (string.IsNullOrWhiteSpace(Query))
		{
			SetRestingLayout(target, scroll);

			target.Inlines = BuildInlines(text, query: null);

			ResetScroll(scroll);

			return;
		}

		// Search: full text, every match highlighted, the first one scrolled into view.
		SetSearchLayout(target, scroll);

		target.Inlines = BuildInlines(text, Query);

		ScrollToFirstMatch(target, scroll, text, Query!);
	}

	/// <summary>
	/// Scrolls the parent viewer so the first match of <paramref name="query" /> in
	/// <paramref name="text" /> is brought near the top, deferred until after the new content is laid out.
	/// </summary>
	private void ScrollToFirstMatch(
		TextBlock target,
		ScrollViewer? scroll,
		string text,
		string query)
	{
		if (scroll is null)
		{
			return;
		}

		int matchStart = text.IndexOf(query, StringComparison.OrdinalIgnoreCase);

		if (matchStart < 0)
		{
			ResetScroll(scroll);

			return;
		}

		// Defer: the new inlines and search layout must be measured / arranged before the match position is read.
		Dispatcher.UIThread.Post(() =>
		{
			if (!ReferenceEquals(AssociatedObject, target))
			{
				return;
			}

			Rect rect = target.TextLayout.HitTestTextPosition(matchStart);

			double maxOffset = Math.Max(0.0, scroll.Extent.Height - scroll.Viewport.Height);

			double y = Math.Clamp(rect.Y - MatchTopMargin, 0.0, maxOffset);

			scroll.Offset = scroll.Offset.WithY(y);
		}, DispatcherPriority.Loaded);
	}
	#endregion
}
