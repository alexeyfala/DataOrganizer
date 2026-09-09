using Entities.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Repository.Converters;
using System;

namespace Repository.DbContexts;

/// <summary>
/// A <see cref="DbContext" /> for Sqlite provider.
/// </summary>
public class SqliteDbContext : DbContext
{
	#region Data
	/// <summary>
	/// Name of the assembly that holds the EF Core migrations for this context.
	/// </summary>
	public const string MigrationsAssemblyName = "Repository.Migrations";
	#endregion

	#region Constructors
	public SqliteDbContext(DbContextOptions<SqliteDbContext> options) : base(options)
	{
		// When used in DI, a constructor with these arguments is required.
	}

	public SqliteDbContext()
	{
		// To be able to create migrations.
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
	{
		if (EF.IsDesignTime && !optionsBuilder.IsConfigured)
		{
			optionsBuilder.UseSqlite(x => x.MigrationsAssembly(MigrationsAssemblyName));
		}

		base.OnConfiguring(optionsBuilder);
	}

	/// <inheritdoc />
	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		ValueConverter<DateTime, DateTime> timeTicksRemoveConverter = new(
			x => new DateTime(x.Ticks - (x.Ticks % TimeSpan.TicksPerSecond), x.Kind),
			x => x);

		#region Base
		// SQLite does not support sequences or Identity seed/increment, and hence integer key value generation
		// is not supported when using SQLite with the TPC strategy.
		// However, client-side generation or globally unique keys - such as GUIDs - are supported
		// on any database, including SQLite.
		modelBuilder
			.Entity<ExplorerItemBase>()
			.UseTpcMappingStrategy();

		modelBuilder
			.Entity<ExplorerItemBase>()
			.HasIndex(x => x.Id);

		modelBuilder
			.Entity<ExplorerItemBase>()
			.Property(x => x.CreatedDate)
			.HasConversion(timeTicksRemoveConverter);

		modelBuilder
			.Entity<ExplorerItemBase>()
			.Property(x => x.UpdatedDate)
			.HasConversion(timeTicksRemoveConverter);
		#endregion

		#region Folder
		modelBuilder
			.Entity<FolderEntity>()
			.HasIndex(x => x.ParentId);

		modelBuilder
			.Entity<FolderEntity>()
			.HasMany(x => x.Children)
			.WithOne(x => x.Parent)
			.HasForeignKey(x => x.ParentId);

		modelBuilder
			.Entity<FolderEntity>()
			.ToTable("Folders");
		#endregion

		#region File
		modelBuilder
			.Entity<FileEntity>()
			.HasIndex(x => x.ParentId);

		modelBuilder
			.Entity<FileEntity>()
			.HasMany(x => x.Hotkeys)
			.WithOne(x => x.Owner)
			.HasForeignKey(x => x.OwnerId);

		modelBuilder
			.Entity<FileEntity>()
			.ToTable("Files");
		#endregion

		#region Hotkey
		modelBuilder
			.Entity<HotkeyEntity>()
			.HasIndex(x => x.OwnerId);

		modelBuilder
			.Entity<HotkeyEntity>()
			.Property(x => x.Code)
			.HasConversion(new KeyCodeNameConverter());

		modelBuilder
			.Entity<HotkeyEntity>()
			.Property(x => x.Mask)
			.HasConversion(new EventMaskNameConverter());

		modelBuilder
			.Entity<HotkeyEntity>()
			.ToTable("Hotkeys");

		modelBuilder
			.Entity<HotkeyEntity>()
			.UsePropertyAccessMode(PropertyAccessMode.Property);
		#endregion

		base.OnModelCreating(modelBuilder);
	}
	#endregion
}
