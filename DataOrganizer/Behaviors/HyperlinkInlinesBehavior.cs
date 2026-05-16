using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Xaml.Interactivity;
using System;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Text.RegularExpressions;

namespace DataOrganizer.Behaviors;

/// <summary>
/// Populates <see cref="TextBlock.Inlines" /> of associated <see cref="TextBlock" />
/// (or <see cref="SelectableTextBlock" />) from <see cref="Text" />, turning detected
/// http(s) URLs into clickable <see cref="HyperlinkButton" /> inlines.
/// </summary>
internal sealed partial class HyperlinkInlinesBehavior : Behavior<TextBlock>
{
	#region Properties
	/// <summary>
	/// Source text. URLs detected inside are rendered as clickable hyperlinks.
	/// </summary>
	public string? Text
	{
		get => GetValue(TextProperty);
		set => SetValue(TextProperty, value);
	}
	#endregion

	#region Styled Properties
	/// <summary>
	/// Identifies the <see cref="Text" /> avalonia property.
	/// </summary>
	public static readonly StyledProperty<string?> TextProperty = AvaloniaProperty
		.Register<HyperlinkInlinesBehavior, string?>(name: nameof(Text));
	#endregion

	#region Data
	/// <summary>
	/// Trailing characters trimmed off a matched URL (commonly stick to URLs in prose).
	/// </summary>
	private const string UrlTrailingTrim = ".,;:!?)]}>'\"";

	/// <summary>
	/// Matches absolute http/https URLs in plain text.
	/// </summary>
	private static readonly Regex UrlRegex = GetUrlRegex();

	/// <inheritdoc cref="CompositeDisposable" />
	private readonly CompositeDisposable _disposables = [];
	#endregion

	#region Event Handlers
	/// <summary>
	/// <see cref="TextProperty" /> changed handler.
	/// </summary>
	private void TextProperty_Changed(string? value) => Rebuild(value);
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
			.GetObservable(TextProperty)
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

	#region Service
	[GeneratedRegex(@"\bhttps?://[^\s<>""]+", RegexOptions.IgnoreCase | RegexOptions.Compiled, "ru-RU")]
	private static partial Regex GetUrlRegex();

	/// <summary>
	/// Strips common trailing punctuation that does not belong to a URL.
	/// </summary>
	private static string TrimTrailingPunctuation(string url)
	{
		while (url.Length > 0 && UrlTrailingTrim.Contains(url[^1]))
		{
			url = url[..^1];
		}

		return url;
	}

	/// <summary>
	/// Rebuilds inlines of <see cref="AssociatedObject" /> from <paramref name="text" />.
	/// </summary>
	private void Rebuild(string? text)
	{
		if (AssociatedObject is null)
		{
			return;
		}

		InlineCollection inlines = [];

		if (!string.IsNullOrEmpty(text))
		{
			int lastIndex = 0;

			foreach (Match match in UrlRegex.Matches(text))
			{
				string url = TrimTrailingPunctuation(match.Value);

				if (url.Length == 0)
				{
					continue;
				}

				if (match.Index > lastIndex)
				{
					inlines.Add(new Run(text[lastIndex..match.Index]));
				}

				if (Uri.TryCreate(url, UriKind.Absolute, out Uri? uri))
				{
					inlines.Add(new InlineUIContainer
					{
						Child = new HyperlinkButton
						{
							Content = url,
							NavigateUri = uri,
							Padding = new Thickness(0.0),
						},
					});
				}
				else
				{
					inlines.Add(new Run(url));
				}

				lastIndex = match.Index + url.Length;
			}

			if (lastIndex < text.Length)
			{
				inlines.Add(new Run(text[lastIndex..]));
			}
		}

		AssociatedObject.Inlines = inlines;
	}
	#endregion
}
