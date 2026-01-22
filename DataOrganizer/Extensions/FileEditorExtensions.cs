using DataOrganizer.Interfaces;
using Entities.Models;
using Repository.DTO;
using Repository.Interfaces;
using Serilog;
using Shared.Extensions;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Extensions;

internal static class FileEditorExtensions
{
	#region Methods
	/// <summary>
	/// Saves <see cref="FileModel.Contents" /> to the database.
	/// </summary>
	public static Task SaveContentsAsync(
		this IFileEditor editor,
		IDbAccess dbAccess,
		ILogger logger,
		byte[] contents,
		CancellationToken token = default)
	{
		logger.LogDebug($@"Saving contents of ""{editor.FileId}"" in the database");

		return UpdatePropertyAsync(
			editor,
			dbAccess,
			propertyName: nameof(FileModel.Contents),
			value: contents,
			isUpdatedDate: true,
			token: token);
	}

	/// <summary>
	/// Saves <see cref="FileModel.Properties" /> to the database.
	/// </summary>
	public static Task SavePropertiesAsync(
		this IFileEditor editor,
		IDbAccess dbAccess,
		ILogger logger,
		string json,
		CancellationToken token = default)
	{
		logger.LogDebug(
			$@"Saving properties of ""{editor.FileId}"" in the database:{json}");

		return UpdatePropertyAsync(
			editor,
			dbAccess,
			propertyName: nameof(FileModel.Properties),
			value: json,
			isUpdatedDate: false,
			token: token);
	}
	#endregion

	#region Service
	/// <summary>
	/// Updates property of <see cref="FileModel" /> in the database.
	/// </summary>
	private static async Task UpdatePropertyAsync<T>(
		IFileEditor editor,
		IDbAccess dbAccess,
		string propertyName,
		T value,
		bool isUpdatedDate,
		CancellationToken token) where T : notnull
	{
		DateTime updatedDate = DateTime.Now;

		PropertyNameValuePair[] properties =
		[
			new PropertyNameValuePair(propertyName, value)
		];

		if (isUpdatedDate)
		{
			properties = [.. properties, new(nameof(FileModel.UpdatedDate), updatedDate)];
		}

		if (!await dbAccess.UpdatePropertiesAsync(
			id: editor.FileId,
			token: token,
			properties).ConfigureAwait(false) || !isUpdatedDate)
		{
			return;
		}

		editor
			.SetUpdatedDateCallback?
			.Invoke(updatedDate);
	}
	#endregion
}
