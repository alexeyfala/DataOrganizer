using System.Diagnostics;

namespace Repository.Dto;

/// <summary>
/// A named property of an object as it is shown to the user.
/// </summary>
[DebuggerDisplay($"{nameof(PropertyName)} = {{{nameof(PropertyName)}}}, {nameof(Value)} = {{{nameof(Value)}}}")]
public sealed class PropertyDescription
{
	#region Properties
	/// <summary>
	/// Property name.
	/// </summary>
	public string PropertyName { get; }

	/// <summary>
	/// The value for <see cref="PropertyName" />.
	/// </summary>
	public object? Value { get; }
	#endregion

	#region Constructors
	public PropertyDescription(string propertyName, object? value)
	{
		PropertyName = propertyName;

		Value = value;
	}
	#endregion
}
