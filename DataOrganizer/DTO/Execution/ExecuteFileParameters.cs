namespace DataOrganizer.DTO.Execution;

public sealed class ExecuteFileParameters : ExecuteFileParametersBase
{
	#region Properties
	/// <summary>
	/// A contents of the file, in plain text.
	/// </summary>
	public required byte[] Contents { get; init; }

	/// <summary>
	/// <c>True</c> when the file is read-only.
	/// </summary>
	public required bool IsReadOnly { get; set; }
	#endregion
}
