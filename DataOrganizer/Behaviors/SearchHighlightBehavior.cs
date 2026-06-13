using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Xaml.Interactivity;
using DataOrganizer.Helpers.Clipboard;
using System;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;

namespace DataOrganizer.Behaviors;

/// <summary>
/// Populates <see cref="TextBlock.Inlines" /> of the associated <see cref="TextBlock" /> with an
/// excerpt of <see cref="SourceText" /> centered on the first match of <see cref="Query" />.
/// </summary>
internal sealed class SearchHighlightBehavior : Behavior<TextBlock>
{
	#region Properties
	/// <summary>
	/// Search query the excerpt is centered on; blank shows the full collapsed text.
	/// </summary>
	public string? Query
	{
		get => GetValue(QueryProperty);
		set => SetValue(QueryProperty, value);
	}

	/// <summary>
	/// Source text the excerpt is built from.
	/// </summary>
	public string? SourceText
	{
		get => GetValue(SourceTextProperty);
		set => SetValue(SourceTextProperty, value);
	}
	#endregion

	#region Styled Properties
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

		this
			.GetObservable(SourceTextProperty)
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

		_disposables.Dispose();
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Rebuilds inlines of <see cref="AssociatedObject" /> from the current source text and query.
	/// </summary>
	private void Rebuild()
	{
		if (AssociatedObject is null)
		{
			return;
		}

		InlineCollection inlines = [];

		string snippet = SearchSnippet.Build(SourceText, Query);

		if (snippet.Length > 0)
		{
			inlines.Add(new Run(snippet));
		}

		AssociatedObject.Inlines = inlines;
	}
	#endregion
}
