using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using DataProtection.Services;
using Hierarchy.Dto;
using Hierarchy.UnitTests.Factories;
using NSubstitute;
using Repository.Interfaces.Database;
using System.Threading;
using System.Threading.Tasks;
using TestSupport.Common;

namespace DataProtection.UnitTests.Services;

[TestFixture(Description = $@"Tests of ""{nameof(KeeperDekWriter)}"" type")]
internal class KeeperDekWriterTests
{
	#region Methods
	/// <summary>
	/// <see cref="KeeperDekWriter.WriteAsync" />: the keeper takes the new wrapped key only when the database takes it,
	/// and the answer says which happened.
	/// </summary>
	[Test]
	public async Task WriteAsync_Takes_The_Key_Only_When_The_Database_Takes_It([Values] bool isWritten)
	{
		// Arrange
		byte[] oldDek = RandomValues.CreateBytes(10);

		byte[] newDek = RandomValues.CreateBytes(10);

		FolderDto keeper = ItemDtoFactory.CreateFolderDto();

		keeper.EncryptedDek = oldDek;

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			dbAccess
				.UpdateEncryptedDekAsync(keeper.Id, newDek, Arg.Any<CancellationToken>())
				.Returns(isWritten);

			builder.RegisterInstance(dbAccess);
		});

		KeeperDekWriter sut = mock.Create<KeeperDekWriter>();

		// Act
		bool result = await sut.WriteAsync(keeper, newDek);

		// Assert
		result
			.Should()
			.Be(isWritten);

		keeper.EncryptedDek
			.Should()
			.BeSameAs(isWritten ? newDek : oldDek);
	}
	#endregion
}
