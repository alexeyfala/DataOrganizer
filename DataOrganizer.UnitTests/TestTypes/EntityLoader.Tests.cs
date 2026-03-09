using Autofac;
using Autofac.Extras.Moq;
using DataOrganizer.Services;
using Mapster;
using MapsterMapper;
using NSubstitute;
using Repository.DbContexts;
using Repository.Interfaces;

namespace DataOrganizer.UnitTests.TestTypes;

[TestFixture(Description = $@"Tests of ""{nameof(EntityLoader)}"" type")]
internal class EntityLoaderTests
{
	#region Methods
	/// <summary>
	/// Test of <see cref="EntityLoader.LoadFromDb" />.
	/// </summary>
	[Test]
	public void LoadFromDb_Does_Work()
	{
		// Arrange
		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IMapper mapper = Substitute.For<IMapper>();

			mapper
				.Config
				.Returns(Substitute.For<TypeAdapterConfig>());

			builder.RegisterInstance(mapper);

			builder.RegisterInstance(dbAccess);
		});

		EntityLoader sut = mock.Create<EntityLoader>();

		// Act
		sut.LoadFromDb(string.Empty);

		// Assert
		dbAccess
			.Received()
			.ClearPool(Arg.Any<SqliteDbContext>());
	}
	#endregion
}
