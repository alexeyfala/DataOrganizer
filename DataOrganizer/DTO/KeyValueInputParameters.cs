namespace DataOrganizer.DTO;

public sealed class KeyValueInputParameters
{
	#region Properties
	/// <summary>
	/// Text for default button.
	/// </summary>
	public required string DefaultButtonText { get; init; }

	/// <summary>
	/// Key.
	/// </summary>
	public string? Key { get; init; }

	/// <summary>
	/// Key hint.
	/// </summary>
	public string? KeyHint { get; init; }

	/// <summary>
	/// Value.
	/// </summary>
	public string? Value { get; init; }

	/// <summary>
	/// Value hint.
	/// </summary>
	public string? ValueHint { get; init; }
	#endregion
}
