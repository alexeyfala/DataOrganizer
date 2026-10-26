using DataOrganizer.Interfaces.Documents;
using Repository.Interfaces.Database;
using Serilog;
using Shared.Extensions;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Services.Documents;

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
