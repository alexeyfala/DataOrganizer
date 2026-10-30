using AwesomeAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Repository.DbContexts;
using Repository.Extensions;
using Repository.Interceptors;
using Repository.Interfaces.Database;
using Serilog;
using Shared.Interfaces;
using System;
using TestSupport.Common;

namespace Repository.UnitTests.Extensions;

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
