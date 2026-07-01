namespace DataOrganizer.DTO;

public sealed class KeyValueInputParameters
{
	#region Properties
	/// <summary>
	/// Text for the default button.
	/// </summary>
	public required string DefaultButtonText { get; init; }

	/// <summary>
	/// Key.
	/// </summary>
	public string? Key { get; init; }

	/// <summary>
	/// Hint for the <see cref="Key" /> input field.
	/// </summary>
	public string? KeyHint { get; init; }

	/// <summary>
	/// <c>True</c> when the <see cref="Key" /> input field is masked with a reveal button.
	/// </summary>
	public bool MaskKeyInput { get; init; }

	/// <summary>
	/// Value.
	/// </summary>
	public string? Value { get; init; }

	/// <summary>
	/// Hint for the <see cref="Value" /> input field.
	/// </summary>
	public string? ValueHint { get; init; }
	#endregion
}
