using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace DataOrganizer.Markup;

/// <summary>
/// XAML markup extension that resolves an instance of <see cref="Type" /> from
/// <see cref="App.Services" />. Used to inject view models directly inside
/// <c>DataTemplate</c> blocks where constructor injection is not available.
/// </summary>
public sealed class ResolveExtension : MarkupExtension
{
	#region Properties
	/// <summary>
	/// Type to resolve from <see cref="App.Services" />.
	/// </summary>
	public Type? Type { get; set; }
	#endregion

	#region Constructors
	public ResolveExtension()
	{
	}

	public ResolveExtension(Type type) => Type = type;
	#endregion

	#region Methods
	/// <inheritdoc />
	public override object ProvideValue(IServiceProvider serviceProvider)
	{
		if (Type is null)
		{
			throw new InvalidOperationException($"{nameof(ResolveExtension)}.{nameof(Type)} must be specified.");
		}

		return App
			.Services
			.GetRequiredService(Type);
	}
	#endregion
}
