using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace DataOrganizer.GuardTests.TestHelpers;

/// <summary>
/// The types a guard knows by name: those of the application by reflection and those of the tests by their source.
/// </summary>
internal static class KnownTypes
{
	#region Methods
	/// <summary>
	/// Tells whether a type of a name is known.
	/// </summary>
	public static bool Contains(string name) => TypeCatalog.Find(name).Length > 0 || TestTypeIndex.Contains(name);

	/// <summary>
	/// Enumerates the names of the interfaces a type of a name implements, an interface counting itself.
	/// </summary>
	public static IEnumerable<string> GetInterfaces(string name)
	{
		string[] found = [.. TypeCatalog.GetInterfaces(name)];

		if (found.Length > 0)
		{
			return found;
		}

		return TestTypeIndex
			.GetBases(name)
			.SelectMany(static x => IsInterfaceName(x) ? [x, .. TypeCatalog.GetInterfaces(x)] : GetInterfaces(x))
			.Distinct();
	}

	/// <summary>
	/// Gets the type of a static member of a type written as <c>Type.Member</c>, or <c>null</c> when it is unknown.
	/// </summary>
	public static string? GetMemberType(string typeAndMember)
	{
		return TestTypeIndex.GetReturnType(typeAndMember) ?? TypeCatalog.GetMemberType(typeAndMember);
	}

	/// <summary>
	/// Enumerates the services the instance constructors of a type of a name take, public or not.
	/// </summary>
	public static IEnumerable<string> GetTakenServices(string name)
	{
		Type[] types = TypeCatalog.Find(name);

		IEnumerable<string> parameters = types.Length > 0
			? types
				.SelectMany(static x => x.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
				.SelectMany(static x => x.GetParameters())
				.Select(static x => TypeCatalog.Format(x.ParameterType))
			: TestTypeIndex.GetConstructorParameters(name);

		return parameters.SelectMany(TypeCatalog.GetTakenServices);
	}

	/// <summary>
	/// Tells whether a name stands for a class the container can build without a registration.
	/// </summary>
	public static bool IsConcreteClass(string name) => TypeCatalog.IsConcreteClass(name) || TestTypeIndex.IsConcreteClass(name);
	#endregion

	#region Helpers
	/// <summary>
	/// Tells whether a name follows the naming of an interface.
	/// </summary>
	private static bool IsInterfaceName(string name) => name.Length > 1 && name[0] == 'I' && char.IsUpper(name[1]);
	#endregion
}
