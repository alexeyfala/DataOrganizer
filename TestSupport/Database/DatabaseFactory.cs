using Repository.Dto;
using Repository.Services.Database;
using Serilog.Core;
using Shared.Interfaces;
using System;
using System.Collections.Generic;
using TestSupport.Common;

namespace TestSupport.Database;

/// <summary>
/// Factory methods that build objects handed out by the database layer.
/// </summary>
public static class DatabaseFactory
{
	#region Methods
	/// <summary>
	/// Creates a <see cref="DatabaseBackup" /> over a random path.
	/// </summary>
	public static DatabaseBackup CreateDatabaseBackup(IFileSystem fileSystem)
	{
		return new(RandomValues.CreateFileName(10), fileSystem, Logger.None);
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
