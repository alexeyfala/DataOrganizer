using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace DataOrganizer.Markup;

/// <summary>
/// XAML markup extension that resolves an instance of <see cref="Type" />.
/// Used to inject view models directly inside
/// <c>DataTemplate</c> blocks where constructor injection is not available.
/// </summary>
public sealed class ResolveExtension : MarkupExtension
{
	#region Properties
	/// <summary>
	/// Key of a service registered under one; <c>null</c> for a service registered without a key.
	/// </summary>
	public object? Key { get; set; }

	/// <summary>
	/// Public getter/setter is required by the XAML markup-extension contract: the parser
	/// instantiates the extension via reflection from another assembly and may assign the
	/// value either positionally through the constructor (e.g. <c>{markup:Resolve {x:Type …}}</c>)
	/// or by name through the setter (e.g. <c>{markup:Resolve Type={x:Type …}}</c>).
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

		// App.Services is null when the type tree is materialized outside of a running
		// application (e.g., in headless unit tests). Returning null leaves DataContext
		// unset on the target, which matches the legacy NUnit-guard behaviour.
		if (App.Services is not { } services)
		{
			return null!;
		}

		return Key is null
			? services.GetRequiredService(Type)
			: services.GetRequiredKeyedService(Type, Key);
	}
	#endregion
}
