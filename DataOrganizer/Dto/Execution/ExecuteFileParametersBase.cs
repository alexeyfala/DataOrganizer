using DataOrganizer.Dto.Entities;
using System;

namespace DataOrganizer.Dto.Execution;

public abstract class ExecuteFileParametersBase
{
	#region Properties
	/// <inheritdoc cref="FileDto" />
	public required FileDto File { get; init; }

	/// <summary>
	/// Identifier of the password keeper holding the key of the file; <c>null</c> for plain contents.
	/// </summary>
	public Guid? KeeperId { get; init; }
	#endregion
}
