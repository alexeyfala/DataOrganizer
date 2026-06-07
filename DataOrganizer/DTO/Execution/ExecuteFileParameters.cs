namespace DataOrganizer.DTO.Execution;

public sealed class ExecuteFileParameters : ExecuteFileParametersBase
{
	#region Properties
	/// <summary>
	/// <c>True</c> when the file is read-only.
	/// </summary>
	public required bool IsReadOnly { get; set; }
	#endregion
}
