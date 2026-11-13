using DataOrganizer.Database.Repository.Dto;
using DataOrganizer.Database.Repository.Services.Database;
using DataOrganizer.Shared.Interfaces;
using DataOrganizer.TestSupport.Common;
using Serilog.Core;
using System;
using System.Collections.Generic;
using System.IO.Abstractions;

namespace DataOrganizer.TestSupport.Database;

/// <summary>
/// Factory methods that build objects handed out by the database layer.
/// </summary>
public static class DatabaseFactory
{
	#region Methods
	/// <summary>
	/// Creates a <see cref="DatabaseBackup" /> over a random path.
	/// </summary>
	public static DatabaseBackup CreateDatabaseBackup(IFileEraser fileEraser, IFileSystem fileSystem)
	{
		return new(
			TestPath.Combine(RandomValues.CreateFileName(10)),
			fileEraser,
			fileSystem,
			Logger.None);
	}

	/// <summary>
	/// Creates the required number of random <see cref="ValidatedContents" /> objects.
	/// </summary>
	public static IEnumerable<ValidatedContents> CreateValidatedContents(
		int count,
		bool isValid,
		bool generateId = true)
	{
		for (int i = 0; i < count; i++)
		{
			yield return new()
			{
				Id = generateId ? Guid.NewGuid() : default,
				IsValid = isValid
			};
		}
	}
	#endregion
}
