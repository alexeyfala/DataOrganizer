using Entities.Abstract;
using Entities.Models;
using Microsoft.EntityFrameworkCore;

namespace Repository.DbContexts;

/// <summary>
/// A <see cref="DbContext" /> for Sqlite provider.
/// </summary>
public class SqliteDbContext : DbContext
{
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
		if (EF.IsDesignTime)
		{
			optionsBuilder.UseSqlite();
		}

		base.OnConfiguring(optionsBuilder);
	}

	/// <inheritdoc />
	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		#region Base
		// SQLite does not support sequences or Identity seed/increment, and hence integer key value generation
		// is not supported when using SQLite with the TPC strategy.
		// However, client-side generation or globally unique keys - such as GUIDs - are supported
		// on any database, including SQLite.
		modelBuilder
			.Entity<ExplorerModelBase>()
			.UseTpcMappingStrategy();

		modelBuilder
			.Entity<ExplorerModelBase>()
			.HasIndex(x => x.Id);
		#endregion

		#region Folder
		modelBuilder
			.Entity<FolderModel>()
			.HasIndex(x => x.ParentId);

		modelBuilder
			.Entity<FolderModel>()
			.HasMany(x => x.Children)
			.WithOne(x => x.Parent)
			.HasForeignKey(x => x.ParentId);

		modelBuilder
			.Entity<FolderModel>()
			.ToTable("Folders");
		#endregion

		#region File
		modelBuilder
			.Entity<FileModel>()
			.HasIndex(x => x.ParentId);

		modelBuilder
			.Entity<FileModel>()
			.HasMany(x => x.Hotkeys)
			.WithOne(x => x.Owner)
			.HasForeignKey(x => x.OwnerId);

		modelBuilder
			.Entity<FileModel>()
			.ToTable("Files");
		#endregion

		#region Hotkey
		modelBuilder
			.Entity<HotkeyModel>()
			.HasIndex(x => x.OwnerId);

		modelBuilder
			.Entity<HotkeyModel>()
			.ToTable("Hotkeys");

		modelBuilder
			.Entity<HotkeyModel>()
			.UsePropertyAccessMode(PropertyAccessMode.Property);
		#endregion

		base.OnModelCreating(modelBuilder);
	}
	#endregion
}
