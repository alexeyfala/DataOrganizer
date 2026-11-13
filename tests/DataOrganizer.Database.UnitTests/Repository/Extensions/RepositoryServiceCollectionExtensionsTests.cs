using AwesomeAssertions;
using DataOrganizer.Database.Repository.DbContexts;
using DataOrganizer.Database.Repository.Extensions;
using DataOrganizer.Database.Repository.Interceptors;
using DataOrganizer.Database.Repository.Interfaces.Database;
using DataOrganizer.Shared.Interfaces;
using DataOrganizer.TestSupport.Common;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Serilog;
using System;
using System.IO.Abstractions;

namespace DataOrganizer.Database.UnitTests.Repository.Extensions;

[TestFixture(Description = $@"Tests of ""{nameof(RepositoryServiceCollectionExtensions)}"" type")]
internal class RepositoryServiceCollectionExtensionsTests
{
	#region Methods
	/// <summary>
	/// <see cref="RepositoryServiceCollectionExtensions.AddRepository" />: every role of the database is one object, so the
	/// roles share one lock and one refusal of writes.
	/// </summary>
	[Test]
	public void AddRepository_Gives_Every_Role_Of_The_Database_One_Object()
	{
		// Arrange
		ServiceCollection services = [];

		services.AddSingleton(Substitute.For<IFileEraser>());

		services.AddSingleton(Substitute.For<IFileSystem>());

		services.AddSingleton(Substitute.For<ILogger>());

		services.AddSingleton(TimeProvider.System);

		// Nothing opens the file: building the objects does not connect.
		services.AddRepository(static _ => "DataOrganizer.sqlite");

		using ServiceProvider provider = services.BuildServiceProvider();

		// Act
		object[] roles =
		[
			provider.GetRequiredService<IDbAccess>(),
			provider.GetRequiredService<IDbBackupService>(),
			provider.GetRequiredService<IDbConnector>(),
			provider.GetRequiredService<IDbImporter>()
		];

		// Assert
		roles
			.Should()
			.AllSatisfy(x => x.Should().BeSameAs(roles[0]));
	}

	/// <summary>
	/// <see cref="RepositoryServiceCollectionExtensions.AddRepository" />: the context opens the database file through
	/// SQLite, and each of its connections gets the pragmas.
	/// </summary>
	[Test]
	public void AddRepository_Opens_The_Database_File_With_The_Pragma_Interceptor()
	{
		// Arrange
		string filePath = TestPath.Combine("Data", "DataOrganizer.sqlite");

		ServiceCollection services = [];

		services.AddRepository(_ => filePath);

		using ServiceProvider provider = services.BuildServiceProvider();

		// Act
		DbContextOptions options = provider.GetRequiredService<DbContextOptions<SqliteDbContext>>();

		// Assert
		string? connectionString = RelationalOptionsExtension.Extract(options).ConnectionString;

		string dataSource = new SqliteConnectionStringBuilder(connectionString).DataSource;

		dataSource
			.Should()
			.Be(filePath);

		options.FindExtension<CoreOptionsExtension>()!.Interceptors
			.Should()
			.ContainSingle()
			.Which
			.Should()
			.BeOfType<SqlitePragmaInterceptor>();
	}
	#endregion
}
