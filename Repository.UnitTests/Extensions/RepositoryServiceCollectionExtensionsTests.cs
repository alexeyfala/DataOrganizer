using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Repository.Extensions;
using Repository.Interfaces.Database;
using Serilog;
using Shared.Interfaces;
using System;

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
	#endregion
}
