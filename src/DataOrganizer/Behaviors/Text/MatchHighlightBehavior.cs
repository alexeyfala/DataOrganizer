using Avalonia;
using Avalonia.Controls;
using Avalonia.Xaml.Interactivity;
using DataOrganizer.Controls.Text;
using System;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;

namespace DataOrganizer.Behaviors.Text;

/// <summary>
/// Highlights in the text of the associated <see cref="TextBlock" /> each match of the query it inherits from an
/// ancestor; without a query the text shows as it is.
/// </summary>
internal sealed class MatchHighlightBehavior : Behavior<TextBlock>
{
	#region Styled Properties
	/// <summary>
	/// Identifies the query attached property, which an ancestor sets for every text block inside it.
	/// </summary>
	public static readonly AttachedProperty<string?> QueryProperty = AvaloniaProperty
		.RegisterAttached<MatchHighlightBehavior, Control, string?>(name: "Query", inherits: true);
	#endregion

	#region Data
	/// <inheritdoc cref="CompositeDisposable" />
	private readonly CompositeDisposable _disposables = [];
	#endregion

	#region Methods
	/// <summary>
	/// Returns the query whose matches the text blocks inside <paramref name="element" /> highlight.
	/// </summary>
	public static string? GetQuery(Control element) => element.GetValue(QueryProperty);

	/// <summary>
	/// Sets the query whose matches the text blocks inside <paramref name="element" /> highlight.
	/// </summary>
	public static void SetQuery(Control element, string? value) => element.SetValue(QueryProperty, value);

	/// <inheritdoc />
	protected override void OnAttached()
	{
		base.OnAttached();

		if (AssociatedObject is null)
		{
			return;
		}

		// A new text clears the runs of the old one, so the runs are built again.
		AssociatedObject
			.GetObservable(TextBlock.TextProperty)
			.Subscribe(_ => Rebuild())
			.DisposeWith(_disposables);

		AssociatedObject
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
	/// Lays the highlighted runs over the text, or removes them while there is no query.
	/// </summary>
	private void Rebuild()
	{
		if (AssociatedObject is not { } target)
		{
			return;
		}

		string? query = GetQuery(target);

		target.Inlines = string.IsNullOrEmpty(query)
			? null
			: TextHighlight.CreateInlines(target.Text ?? string.Empty, query);
	}
	#endregion
}
