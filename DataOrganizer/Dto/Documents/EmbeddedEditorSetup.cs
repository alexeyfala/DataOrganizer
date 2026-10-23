using Entities.Enums;
using System;

namespace DataOrganizer.Dto.Documents;

/// <summary>
/// The file an embedded editor edits, with the callbacks that report its changes back to the file.
/// </summary>
public sealed class EmbeddedEditorSetup
{
	#region Properties
	/// <summary>
	/// The identifier of the file being edited.
	/// </summary>
	public required Guid FileId { get; init; }

	/// <summary>
	/// Name of the file.
	/// </summary>
	public required string? FileName { get; init; }

	/// <summary>
	/// Editor state to start from.
	/// </summary>
	public required string? InitialEditorState { get; init; }

	/// <summary>
	/// Identifier of the password keeper holding the key of the file.
	/// </summary>
	public required Guid? KeeperId { get; init; }

	/// <summary>
	/// Kind of the file: a text file or a dataset.
	/// </summary>
	public required EntityKind Kind { get; init; }

	/// <summary>
	/// Callback that reports the editor state.
	/// </summary>
	public required Action<string>? SetEditorStateCallback { get; init; }

	/// <summary>
	/// Callback that reports the date of change written with an edit of the contents.
	/// </summary>
	public required Action<DateTime>? SetUpdatedAtCallback { get; init; }
	#endregion
}
