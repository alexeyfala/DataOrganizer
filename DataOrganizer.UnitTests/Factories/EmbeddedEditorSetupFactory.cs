using DataOrganizer.Dto.Documents;
using Entities.Enums;
using System;

namespace DataOrganizer.UnitTests.Factories;

/// <summary>
/// Factory methods that build the setups of embedded editors.
/// </summary>
public static class EmbeddedEditorSetupFactory
{
	#region Methods
	/// <summary>
	/// Creates the setup of an editor of a file.
	/// </summary>
	public static EmbeddedEditorSetup CreateSetup(
		Guid fileId = default,
		string? fileName = null,
		string? initialEditorState = null,
		Guid? keeperId = null,
		EntityKind kind = EntityKind.File,
		Action<string>? setEditorStateCallback = null,
		Action<DateTime>? setUpdatedAtCallback = null)
	{
		return new()
		{
			FileId = fileId,
			FileName = fileName,
			InitialEditorState = initialEditorState,
			KeeperId = keeperId,
			Kind = kind,
			SetEditorStateCallback = setEditorStateCallback,
			SetUpdatedAtCallback = setUpdatedAtCallback
		};
	}
	#endregion
}
