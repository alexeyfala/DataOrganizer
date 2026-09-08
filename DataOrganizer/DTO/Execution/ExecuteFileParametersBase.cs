using DataOrganizer.DTO.Entities;
using System;

namespace DataOrganizer.DTO.Execution;

public abstract class ExecuteFileParametersBase
{
	#region Properties
	/// <inheritdoc cref="FileModelDto" />
	public required FileModelDto File { get; init; }

	/// <summary>
	/// Identifier of the password keeper holding the key of the file; <c>null</c> for plain contents.
	/// </summary>
	public Guid? KeeperId { get; init; }
	#endregion
}
