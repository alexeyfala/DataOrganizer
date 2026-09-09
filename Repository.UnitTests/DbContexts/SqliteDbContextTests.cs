using AwesomeAssertions;
using Entities.Enums;
using Entities.Models;
using Microsoft.EntityFrameworkCore;
using Repository.DbContexts;
using Repository.UnitTests.Fixtures;
using SharpHook.Data;
using System;
using System.Data.Common;
using System.Threading.Tasks;

namespace Repository.UnitTests.DbContexts;

[TestFixture(Description = $@"Tests of ""{nameof(SqliteDbContext)}"" type")]
internal class SqliteDbContextTests
{
	#region Methods
	/// <summary>
	/// <see cref="SqliteDbContext" />: loads the files when a hotkey holds a key the library no longer has.
	/// </summary>
	[Test]
	public async Task File_With_Unknown_Key_Name_Is_Loaded()
	{
		// Arrange
		using TestDatabase database = new();

		await AddFileWithHotkeyAsync(database, KeyCode.VcA, EventMask.LeftCtrl);

		await ExecuteAsync(database, "UPDATE Hotkeys SET Code = 'VcKanji'");

		database
			.Context
			.ChangeTracker
			.Clear();

		// Act
		FileEntity[] result = await LoadFilesAsync(database);

		// Assert
		HotkeyEntity hotkey = result
			.Should()
			.ContainSingle()
			.Which
			.Hotkeys
			.Should()
			.ContainSingle()
			.Which;

		hotkey
			.Code
			.Should()
			.Be(KeyCode.VcUndefined);

		hotkey
			.Mask
			.Should()
			.Be(EventMask.LeftCtrl);
	}

	/// <summary>
	/// <see cref="SqliteDbContext" />: loads the files when a hotkey holds a mask the library no longer has.
	/// </summary>
	[Test]
	public async Task File_With_Unknown_Mask_Name_Is_Loaded()
	{
		// Arrange
		using TestDatabase database = new();

		await AddFileWithHotkeyAsync(database, KeyCode.VcA, EventMask.LeftCtrl);

		await ExecuteAsync(database, "UPDATE Hotkeys SET Mask = 'LeftHyper'");

		database
			.Context
			.ChangeTracker
			.Clear();

		// Act
		FileEntity[] result = await LoadFilesAsync(database);

		// Assert
		HotkeyEntity hotkey = result
			.Should()
			.ContainSingle()
			.Which
			.Hotkeys
			.Should()
			.ContainSingle()
			.Which;

		hotkey
			.Code
			.Should()
			.Be(KeyCode.VcA);

		hotkey
			.Mask
			.Should()
			.Be(EventMask.None);
	}

	/// <summary>
	/// <see cref="SqliteDbContext" />: reads a hotkey back by its stored names.
	/// </summary>
	[Test]
	public async Task Hotkey_Is_Read_By_Name()
	{
		// Arrange
		using TestDatabase database = new();

		await AddFileWithHotkeyAsync(database, KeyCode.VcA, EventMask.LeftCtrl);

		database
			.Context
			.ChangeTracker
			.Clear();

		// Act
		FileEntity[] result = await LoadFilesAsync(database);

		// Assert
		HotkeyEntity hotkey = result
			.Should()
			.ContainSingle()
			.Which
			.Hotkeys
			.Should()
			.ContainSingle()
			.Which;

		hotkey
			.Code
			.Should()
			.Be(KeyCode.VcA);

		hotkey
			.Mask
			.Should()
			.Be(EventMask.LeftCtrl);
	}

	/// <summary>
	/// <see cref="SqliteDbContext" />: keeps a hotkey as the names of its key and its mask.
	/// </summary>
	[Test]
	public async Task Hotkey_Is_Stored_By_Name()
	{
		// Arrange
		using TestDatabase database = new();

		await AddFileWithHotkeyAsync(database, KeyCode.VcA, EventMask.LeftCtrl);

		// Act
		object? code = await ExecuteScalarAsync(database, "SELECT Code FROM Hotkeys");

		object? mask = await ExecuteScalarAsync(database, "SELECT Mask FROM Hotkeys");

		// Assert
		code
			.Should()
			.Be("VcA");

		mask
			.Should()
			.Be("LeftCtrl");
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Adds a file that owns a single hotkey.
	/// </summary>
	private static async Task AddFileWithHotkeyAsync(TestDatabase database, KeyCode code, EventMask mask)
	{
		Guid ownerId = Guid.NewGuid();

		FileEntity file = new()
		{
			Id = ownerId,
			Index = 0,
			Name = "file",
			EntityType = EntityKind.File,
			Contents = []
		};

		file
			.Hotkeys
			.Add(new()
			{
				Code = code,
				Id = Guid.NewGuid(),
				Index = 0,
				Mask = mask,
				OwnerId = ownerId
			});

		database
			.Context
			.Add(file);

		await database
			.Context
			.SaveChangesAsync();
	}

	/// <summary>
	/// Creates a command on the connection the context uses.
	/// </summary>
	private static DbCommand CreateCommand(TestDatabase database, string sql)
	{
		DbCommand command = database
			.Context
			.Database
			.GetDbConnection()
			.CreateCommand();

		command.CommandText = sql;

		return command;
	}

	/// <summary>
	/// Runs a statement against the database behind the context.
	/// </summary>
	private static async Task ExecuteAsync(TestDatabase database, string sql)
	{
		await using DbCommand command = CreateCommand(database, sql);

		await command.ExecuteNonQueryAsync();
	}

	/// <summary>
	/// Reads the first column of the first row a query returns.
	/// </summary>
	private static async Task<object?> ExecuteScalarAsync(TestDatabase database, string sql)
	{
		await using DbCommand command = CreateCommand(database, sql);

		return await command.ExecuteScalarAsync();
	}

	/// <summary>
	/// Loads the files together with their hotkeys.
	/// </summary>
	private static Task<FileEntity[]> LoadFilesAsync(TestDatabase database)
	{
		return database
			.Context
			.Set<FileEntity>()
			.Include(x => x.Hotkeys)
			.ToArrayAsync();
	}
	#endregion
}
