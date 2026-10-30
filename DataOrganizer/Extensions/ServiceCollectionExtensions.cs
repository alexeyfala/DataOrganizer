using Microsoft.Extensions.DependencyInjection;
using System;

namespace DataOrganizer.Extensions;

internal static class ServiceCollectionExtensions
{
	#region Methods
	/// <summary>
	/// Registers <typeparamref name="TImplementation" /> as a singleton
	/// <typeparamref name="TService" /> and a <see cref="Lazy{T}" /> wrapper around it.
	/// </summary>
	public static IServiceCollection AddLazySingleton<TService, TImplementation>(this IServiceCollection target)
		where TService : class
		where TImplementation : class, TService
	{
		target.AddSingleton<TService, TImplementation>();

		return AddLazySingleton<TService>(target);
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Registers a singleton <see cref="Lazy{T}" /> wrapper over an already registered
	/// <typeparamref name="TService" />.
	/// </summary>
	private static IServiceCollection AddLazySingleton<TService>(IServiceCollection target)
		where TService : class
	{
		return target.AddSingleton(x => new Lazy<TService>(x.GetRequiredService<TService>));
	}
	#endregion
}
