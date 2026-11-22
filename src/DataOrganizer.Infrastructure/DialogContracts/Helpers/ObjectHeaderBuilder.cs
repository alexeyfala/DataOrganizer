using DataOrganizer.Shared.Helpers;

namespace DataOrganizer.Infrastructure.DialogContracts.Helpers;

/// <summary>
/// Composes the header of a dialog that acts on an object.
/// </summary>
public static class ObjectHeaderBuilder
{
	#region Methods
	/// <summary>
	/// Composes a header of <paramref name="action" /> followed by the name of the object in quotes.
	/// </summary>
	public static string Build(string action, string name)
	{
		return $"{action} {QuoteBuilder.Build(name)}";
	}
	#endregion
}
