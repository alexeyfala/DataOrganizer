using DataOrganizer.Database.Repository.Interfaces.Database;
using DataOrganizer.Documents.Editor.Interfaces;
using DataOrganizer.Shared.Extensions;
using Serilog;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Documents.Editor.Services;

/// <inheritdoc cref="IEditorStateStore" />
public sealed class EditorStateStore : IEditorStateStore
{
	#region Data
	/// <inheritdoc cref="IDbAccess" />
	private readonly IDbAccess _dbAccess;

	/// <inheritdoc cref="ILogger" />
	private readonly ILogger _logger;
	#endregion

	#region Constructors
	public EditorStateStore(IDbAccess dbAccess, ILogger logger)
	{
		_dbAccess = dbAccess;

		_logger = logger;
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public Task<string?> ReadAsync(Guid fileId, CancellationToken token = default)
	{
		return _dbAccess.GetFileEditorStateAsync(fileId, token);
	}

	/// <inheritdoc />
	public Task WriteAsync(
		Guid fileId,
		[StringSyntax(StringSyntaxAttribute.Json)] string json,
		CancellationToken token = default)
	{
		_logger.LogDebug(
			$@"Saving the editor state of ""{fileId}"" in the database:{json}");

		return _dbAccess.UpdateFileEditorStateAsync(fileId, json, token);
	}
	#endregion
}
