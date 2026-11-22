using System.Collections.Generic;

namespace DataOrganizer.GuardTests.Dto;

/// <summary>
/// The declarations of the types of the test sources.
/// </summary>
internal sealed class Declarations
{
	#region Properties
	/// <summary>
	/// Types each test type derives from or implements, by the name of the type.
	/// </summary>
	public Dictionary<string, string[]> Bases { get; } = [];

	/// <summary>
	/// Test types that are neither static nor abstract.
	/// </summary>
	public HashSet<string> ConcreteClasses { get; } = [];

	/// <summary>
	/// Parameter types of each constructor of each test type, by the name of the type.
	/// </summary>
	public Dictionary<string, List<string[]>> Constructors { get; } = [];

	/// <summary>
	/// Return types of the static methods of the test types, by <c>Type.Method</c>.
	/// </summary>
	public Dictionary<string, string> Returns { get; } = [];
	#endregion
}
