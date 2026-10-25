using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using EditorDocuments.Services;
using NSubstitute;
using Repository.Interfaces.Database;
using Shared.Common;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EditorDocuments.UnitTests.Services;

[TestFixture(Description = $@"Tests of ""{nameof(EditorStateStore)}"" type")]
internal class EditorStateStoreTests
{
	#region Methods
	/// <summary>
	/// <see cref="EditorStateStore.ReadAsync" />: the state comes from the file in the database.
	/// </summary>
	[Test]
	public async Task ReadAsync_Returns_The_Stored_State()
	{
		// Arrange
		Guid fileId = Guid.NewGuid();

		string state = RandomString.Create(10);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			dbAccess
				.GetFileEditorStateAsync(fileId, Arg.Any<CancellationToken>())
				.Returns(state);

			builder.RegisterInstance(dbAccess);
		});

		EditorStateStore sut = mock.Create<EditorStateStore>();

		// Act
		string? result = await sut.ReadAsync(fileId);

		// Assert
		result
			.Should()
			.Be(state);
	}

	/// <summary>
	/// <see cref="EditorStateStore.WriteAsync" />: the state is written once to the file in the database.
	/// </summary>
	[Test]
	public async Task WriteAsync_Writes_The_State_Of_The_File()
	{
		// Arrange
		Guid fileId = Guid.NewGuid();

		string state = RandomString.Create(10);

		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(dbAccess));

		EditorStateStore sut = mock.Create<EditorStateStore>();

		// Act
		await sut.WriteAsync(fileId, state);

		// Assert
		await dbAccess
			.Received(1)
			.UpdateFileEditorStateAsync(fileId, state, Arg.Any<CancellationToken>());
	}
	#endregion
}
