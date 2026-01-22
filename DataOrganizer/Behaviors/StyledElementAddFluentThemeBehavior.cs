using Avalonia;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Themes.Fluent;
using Avalonia.Xaml.Interactivity;
using System;
using System.Linq;

namespace DataOrganizer.Behaviors;

// Nuget: Avalonia.Themes.Fluent

internal sealed class StyledElementAddFluentThemeBehavior : Behavior<StyledElement>
{
	#region Methods
	/// <inheritdoc />
	protected override void OnAttached()
	{
		base.OnAttached();

		if (AssociatedObject is null)
		{
			return;
		}

		Type fluentThemeType = typeof(FluentTheme);

		if (!AssociatedObject
			.Styles
			.Any(x => x.GetType().Equals(fluentThemeType)))
		{
			AssociatedObject
				.Styles
				.Add(new FluentTheme());
		}

		const string uriString = "avares://AvaloniaEdit/Themes/Fluent/AvaloniaEdit.xaml";

		if (AssociatedObject
			.Styles
			.Any(x => x is StyleInclude style && style.Source?.AbsoluteUri == uriString))
		{
			return;
		}

		Uri source = new(uriString);

		StyleInclude style = new(source)
		{
			Source = source
		};

		AssociatedObject
			.Styles
			.Add(style);
	}
	#endregion
}
