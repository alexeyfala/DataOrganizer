using System.Diagnostics;

namespace Repository.DTO;

/// <summary>
/// The pair of <see cref="PropertyName" /> and <see cref="Value" /> values.
/// </summary>
[DebuggerDisplay($"{nameof(PropertyName)} = {{{nameof(PropertyName)}}}, {nameof(Value)} = {{{nameof(Value)}}}")]
public sealed class PropertyNameValuePair
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
	public PropertyNameValuePair(string propertyName, object? value)
	{
		PropertyName = propertyName;

		Value = value;
	}
	#endregion
}
