using System.Diagnostics;

namespace Repository.Dto;

/// <summary>
/// A named property of an object as it is shown to the user.
/// </summary>
[DebuggerDisplay($"{nameof(Name)} = {{{nameof(Name)}}}, {nameof(Value)} = {{{nameof(Value)}}}")]
public sealed class PropertyDescription
{
	#region Properties
	/// <summary>
	/// Name of the property as it is shown to the user.
	/// </summary>
	public string Name { get; }

	/// <summary>
	/// The value shown next to <see cref="Name" />.
	/// </summary>
	public object? Value { get; }
	#endregion

	#region Constructors
	public PropertyDescription(string name, object? value)
	{
		Name = name;

		Value = value;
	}
	#endregion
}
