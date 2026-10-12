using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using DataOrganizer.Dto.Documents;
using DataOrganizer.Enums.Documents;
using DataOrganizer.Helpers.Security;
using DataOrganizer.Interfaces.Encryption;
using DataOrganizer.Services.Documents;
using Entities.Models;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Repository.Interfaces.Database;
using System;
using System.Threading;
using System.Threading.Tasks;
using TestSupport.Common;

namespace DataOrganizer.UnitTests.Services.Documents;

[TestFixture(Description = $@"Tests of ""{nameof(FileContentsWriter)}"" type")]
internal class FileContentsWriterTests
{
	#region Methods
	/// <summary>
	/// <see cref="FileContentsWriter.WriteAsync" />: an edit is written with the date of change it is made at.
	/// </summary>
	[Test]
	public async Task WriteAsync_Dates_An_Edit()
	{
		// Arrange
		Guid fileId = Guid.NewGuid();

		FakeTimeProvider time = new();

		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			dbAccess
				.UpdateFilePropertiesAsync(default, default!, default)
				.ReturnsForAnyArgs(true);

			builder.RegisterInstance(dbAccess);

			builder.RegisterInstance<TimeProvider>(time);
		});

		FileContentsWriter sut = mock.Create<FileContentsWriter>();

		// Act
		FileContentsWriting result = await sut.WriteAsync(
			fileId,
			null,
			RandomValues.CreateBytes(10),
			FileContentsChange.Edit);

		// Assert
		result
			.Should()
			.Be(new FileContentsWriting
			{
				Status = FileContentsWriteStatus.Written,
				UpdatedAt = time.GetLocalNow().LocalDateTime
			});

		// The contents and the date of change
		await dbAccess.Received(1).UpdateFilePropertiesAsync(
			fileId,
			Arg.Is<Action<UpdateSettersBuilder<FileEntity>>[]>(x => x.Length == 2),
			Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="FileContentsWriter.WriteAsync" />: the contents of a protected file are encrypted with the session key of
	/// its keeper.
	/// </summary>
	[Test]
	public async Task WriteAsync_Encrypts_The_Contents_Under_A_Keeper()
	{
		// Arrange
		Guid keeperId = Guid.NewGuid();

		byte[] contents = RandomValues.CreateBytes(10);

		IContentCipher contentCipher = Substitute.For<IContentCipher>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			dbAccess
				.UpdateFilePropertiesAsync(default, default!, default)
				.ReturnsForAnyArgs(true);

			contentCipher
				.TryEncrypt(keeperId, ContentIdentity.Contents, contents)
				.Returns(RandomValues.CreateBytes(26));

			builder.RegisterInstance(dbAccess);

			builder.RegisterInstance(contentCipher);

			builder
				.RegisterType<FakeTimeProvider>()
				.As<TimeProvider>();
		});

		FileContentsWriter sut = mock.Create<FileContentsWriter>();

		// Act
		FileContentsWriting result = await sut.WriteAsync(
			Guid.NewGuid(),
			keeperId,
			contents,
			FileContentsChange.Edit);

		// Assert
		result.Status
			.Should()
			.Be(FileContentsWriteStatus.Written);

		contentCipher
			.Received(1)
			.TryEncrypt(keeperId, ContentIdentity.Contents, contents);
	}

	/// <summary>
	/// <see cref="FileContentsWriter.WriteAsync" />: a change of how the contents are shown is written without a date of
	/// change.
	/// </summary>
	[Test]
	public async Task WriteAsync_Keeps_The_Date_Of_A_View_Change()
	{
		// Arrange
		Guid fileId = Guid.NewGuid();

		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			dbAccess
				.UpdateFilePropertiesAsync(default, default!, default)
				.ReturnsForAnyArgs(true);

			builder.RegisterInstance(dbAccess);
		});

		FileContentsWriter sut = mock.Create<FileContentsWriter>();

		// Act
		FileContentsWriting result = await sut.WriteAsync(
			fileId,
			null,
			RandomValues.CreateBytes(10),
			FileContentsChange.View);

		// Assert
		result
			.Should()
			.Be(new FileContentsWriting
			{
				Status = FileContentsWriteStatus.Written
			});

		// The contents alone
		await dbAccess.Received(1).UpdateFilePropertiesAsync(
			fileId,
			Arg.Is<Action<UpdateSettersBuilder<FileEntity>>[]>(x => x.Length == 1),
			Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="FileContentsWriter.WriteAsync" />: a failure of the database reaches the caller, which reports it.
	/// </summary>
	[Test]
	public async Task WriteAsync_Lets_A_Database_Failure_Through()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			dbAccess
				.UpdateFilePropertiesAsync(default, default!, default)
				.ThrowsAsyncForAnyArgs(new InvalidOperationException());

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<FakeTimeProvider>()
				.As<TimeProvider>();
		});

		FileContentsWriter sut = mock.Create<FileContentsWriter>();

		// Act
		Func<Task> act = () => sut.WriteAsync(
			Guid.NewGuid(),
			null,
			RandomValues.CreateBytes(10),
			FileContentsChange.Edit);

		// Assert
		await act
			.Should()
			.ThrowAsync<InvalidOperationException>();
	}

	/// <summary>
	/// <see cref="FileContentsWriter.WriteAsync" />: contents that find no file in the database are reported missing, with no
	/// date of change.
	/// </summary>
	[Test]
	public async Task WriteAsync_Reports_A_File_Gone_From_The_Database()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			// No row takes the contents.
			dbAccess
				.UpdateFilePropertiesAsync(default, default!, default)
				.ReturnsForAnyArgs(false);

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<FakeTimeProvider>()
				.As<TimeProvider>();
		});

		FileContentsWriter sut = mock.Create<FileContentsWriter>();

		// Act
		FileContentsWriting result = await sut.WriteAsync(
			Guid.NewGuid(),
			null,
			RandomValues.CreateBytes(10),
			FileContentsChange.Edit);

		// Assert
		result
			.Should()
			.Be(new FileContentsWriting
			{
				Status = FileContentsWriteStatus.Missing
			});
	}

	/// <summary>
	/// <see cref="FileContentsWriter.WriteAsync" />: the ciphertext, a buffer of the writer's own, is wiped after the write,
	/// while the contents stay with the caller as they were.
	/// </summary>
	[Test]
	public async Task WriteAsync_Wipes_The_Ciphertext_But_Not_The_Contents()
	{
		// Arrange
		byte[] contents = [1, 2, 3];

		byte[] ciphertext = [4, 5, 6, 7];

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			dbAccess
				.UpdateFilePropertiesAsync(default, default!, default)
				.ReturnsForAnyArgs(true);

			IContentCipher contentCipher = Substitute.For<IContentCipher>();

			contentCipher
				.TryEncrypt(Arg.Any<Guid>(), Arg.Any<ContentIdentity>(), contents)
				.Returns(ciphertext);

			builder.RegisterInstance(dbAccess);

			builder.RegisterInstance(contentCipher);

			builder
				.RegisterType<FakeTimeProvider>()
				.As<TimeProvider>();
		});

		FileContentsWriter sut = mock.Create<FileContentsWriter>();

		// Act
		await sut.WriteAsync(
			Guid.NewGuid(),
			Guid.NewGuid(),
			contents,
			FileContentsChange.Edit);

		// Assert
		ciphertext
			.Should()
			.OnlyContain(x => x == 0);

		contents
			.Should()
			.Equal(1, 2, 3);
	}

	/// <summary>
	/// <see cref="FileContentsWriter.WriteAsync" />: empty contents of a protected file are written without encryption.
	/// </summary>
	[Test]
	public async Task WriteAsync_Writes_Empty_Contents_Unencrypted_Under_A_Keeper()
	{
		// Arrange
		Guid fileId = Guid.NewGuid();

		IContentCipher contentCipher = Substitute.For<IContentCipher>();

		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			dbAccess
				.UpdateFilePropertiesAsync(default, default!, default)
				.ReturnsForAnyArgs(true);

			builder.RegisterInstance(contentCipher);

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<FakeTimeProvider>()
				.As<TimeProvider>();
		});

		FileContentsWriter sut = mock.Create<FileContentsWriter>();

		// Act
		FileContentsWriting result = await sut.WriteAsync(
			fileId,
			Guid.NewGuid(),
			[],
			FileContentsChange.Edit);

		// Assert
		result.Status
			.Should()
			.Be(FileContentsWriteStatus.Written);

		contentCipher
			.DidNotReceiveWithAnyArgs()
			.TryEncrypt(default, default, default!);

		await dbAccess.Received(1).UpdateFilePropertiesAsync(
			fileId,
			Arg.Any<Action<UpdateSettersBuilder<FileEntity>>[]>(),
			Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="FileContentsWriter.WriteAsync" />: contents the session key refuses to encrypt are not written.
	/// </summary>
	[Test]
	public async Task WriteAsync_Writes_Nothing_When_The_Encryption_Is_Refused()
	{
		// Arrange
		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IContentCipher contentCipher = Substitute.For<IContentCipher>();

			// The cipher swallows the failure and answers with a refusal.
			contentCipher
				.TryEncrypt(default, default, default!)
				.ReturnsForAnyArgs((byte[]?)null);

			builder.RegisterInstance(contentCipher);

			builder.RegisterInstance(dbAccess);
		});

		FileContentsWriter sut = mock.Create<FileContentsWriter>();

		// Act
		FileContentsWriting result = await sut.WriteAsync(
			Guid.NewGuid(),
			Guid.NewGuid(),
			RandomValues.CreateBytes(10),
			FileContentsChange.Edit);

		// Assert
		result
			.Should()
			.Be(new FileContentsWriting
			{
				Status = FileContentsWriteStatus.Closed
			});

		await dbAccess
			.DidNotReceiveWithAnyArgs()
			.UpdateFilePropertiesAsync(default, default!, default);
	}

	/// <summary>
	/// <see cref="FileContentsWriter.WriteAsync" />: the contents of a file with no keeper are written as they are.
	/// </summary>
	[Test]
	public async Task WriteAsync_Writes_The_Contents_As_They_Are_Without_A_Keeper()
	{
		// Arrange
		Guid fileId = Guid.NewGuid();

		IContentCipher contentCipher = Substitute.For<IContentCipher>();

		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			dbAccess
				.UpdateFilePropertiesAsync(default, default!, default)
				.ReturnsForAnyArgs(true);

			builder.RegisterInstance(contentCipher);

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<FakeTimeProvider>()
				.As<TimeProvider>();
		});

		FileContentsWriter sut = mock.Create<FileContentsWriter>();

		// Act
		FileContentsWriting result = await sut.WriteAsync(
			fileId,
			null,
			RandomValues.CreateBytes(10),
			FileContentsChange.Edit);

		// Assert
		result.Status
			.Should()
			.Be(FileContentsWriteStatus.Written);

		contentCipher
			.DidNotReceiveWithAnyArgs()
			.TryEncrypt(default, default, default!);

		await dbAccess.Received(1).UpdateFilePropertiesAsync(
			fileId,
			Arg.Any<Action<UpdateSettersBuilder<FileEntity>>[]>(),
			Arg.Any<CancellationToken>());
	}
	#endregion
}
