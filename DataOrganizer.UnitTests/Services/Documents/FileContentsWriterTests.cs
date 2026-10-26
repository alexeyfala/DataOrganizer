using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using DataOrganizer.Dto.Documents;
using DataOrganizer.Enums.Documents;
using DataOrganizer.Services.Documents;
using DataProtection.Helpers;
using DataProtection.Interfaces;
using Entities.Models;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Repository.Enums;
using Repository.Interfaces;
using Repository.Interfaces.Database;
using Repository.Services;
using Repository.Services.Database;
using System;
using System.Threading.Tasks;
using TestSupport.Common;
using TestSupport.Database;
using TestSupport.Models;

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
		using TestDatabase database = new();

		FileEntity file = EntityFactory.CreateFile(contents: RandomValues.CreateBytes(10));

		byte[] contents = RandomValues.CreateBytes(10);

		FakeTimeProvider time = new();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder.RegisterInstance(database.Context);

			builder.RegisterInstance<TimeProvider>(time);

			builder
				.RegisterType<DbAccess>()
				.As<IDbAccess>();

			builder
				.RegisterType<DbContextService>()
				.As<IDbContextService>();

			builder
				.RegisterType<FileRepository>()
				.As<IFileRepository>();
		});

		IDbAccess dbAccess = mock.Create<IDbAccess>();

		await dbAccess.AddFilesAsync([file]);

		FileContentsWriter sut = mock.Create<FileContentsWriter>();

		// Act
		FileContentsWriting result = await sut.WriteAsync(
			file.Id,
			null,
			contents,
			FileContentsChange.Edit);

		// Assert
		result
			.Should()
			.Be(new FileContentsWriting
			{
				Status = FileContentsWriteStatus.Written,
				UpdatedAt = time.GetLocalNow().LocalDateTime
			});

		FileEntity[] stored = await dbAccess.GetAllFilesAsync(OptionalFileProperties.Contents);

		stored
			.Should()
			.ContainSingle()
			.Which
			.Should()
			.BeEquivalentTo(new
			{
				Contents = contents,
				UpdatedAt = time.GetLocalNow().LocalDateTime
			});
	}

	/// <summary>
	/// <see cref="FileContentsWriter.WriteAsync" />: the contents of a protected file are encrypted with the session key of
	/// its keeper, and the ciphertext is what the database gets.
	/// </summary>
	[Test]
	public async Task WriteAsync_Encrypts_The_Contents_Under_A_Keeper()
	{
		// Arrange
		using TestDatabase database = new();

		FileEntity file = EntityFactory.CreateFile();

		Guid keeperId = Guid.NewGuid();

		byte[] contents = RandomValues.CreateBytes(10);

		byte[] ciphertext = RandomValues.CreateBytes(26);

		// The writer wipes its ciphertext after the write.
		byte[] expected = [.. ciphertext];

		IContentCipher contentCipher = Substitute.For<IContentCipher>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			contentCipher
				.TryEncrypt(keeperId, ContentIdentity.Contents, contents)
				.Returns(ciphertext);

			builder.RegisterInstance(contentCipher);

			builder.RegisterInstance(database.Context);

			builder
				.RegisterType<DbAccess>()
				.As<IDbAccess>();

			builder
				.RegisterType<DbContextService>()
				.As<IDbContextService>();

			builder
				.RegisterType<FakeTimeProvider>()
				.As<TimeProvider>();

			builder
				.RegisterType<FileRepository>()
				.As<IFileRepository>();
		});

		IDbAccess dbAccess = mock.Create<IDbAccess>();

		await dbAccess.AddFilesAsync([file]);

		FileContentsWriter sut = mock.Create<FileContentsWriter>();

		// Act
		FileContentsWriting result = await sut.WriteAsync(
			file.Id,
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

		FileEntity[] stored = await dbAccess.GetAllFilesAsync(OptionalFileProperties.Contents);

		stored
			.Should()
			.ContainSingle()
			.Which
			.Contents
			.Should()
			.Equal(expected);
	}

	/// <summary>
	/// <see cref="FileContentsWriter.WriteAsync" />: a change of how the contents are shown is written without a date of
	/// change.
	/// </summary>
	[Test]
	public async Task WriteAsync_Keeps_The_Date_Of_A_View_Change()
	{
		// Arrange
		using TestDatabase database = new();

		FileEntity file = EntityFactory.CreateFile(contents: RandomValues.CreateBytes(10));

		// The database keeps dates to the second.
		DateTime updatedAt = new(2020, 1, 2, 3, 4, 5);

		file.UpdatedAt = updatedAt;

		byte[] contents = RandomValues.CreateBytes(10);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder.RegisterInstance(database.Context);

			builder
				.RegisterType<DbAccess>()
				.As<IDbAccess>();

			builder
				.RegisterType<DbContextService>()
				.As<IDbContextService>();

			builder
				.RegisterType<FileRepository>()
				.As<IFileRepository>();
		});

		IDbAccess dbAccess = mock.Create<IDbAccess>();

		await dbAccess.AddFilesAsync([file]);

		FileContentsWriter sut = mock.Create<FileContentsWriter>();

		// Act
		FileContentsWriting result = await sut.WriteAsync(
			file.Id,
			null,
			contents,
			FileContentsChange.View);

		// Assert
		result
			.Should()
			.Be(new FileContentsWriting
			{
				Status = FileContentsWriteStatus.Written
			});

		FileEntity[] stored = await dbAccess.GetAllFilesAsync(OptionalFileProperties.Contents);

		stored
			.Should()
			.ContainSingle()
			.Which
			.Should()
			.BeEquivalentTo(new
			{
				Contents = contents,
				UpdatedAt = updatedAt
			});
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
				.UpdateFileContentsAsync(default, default!, default, default)
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
		using TestDatabase database = new();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder.RegisterInstance(database.Context);

			builder
				.RegisterType<DbAccess>()
				.As<IDbAccess>();

			builder
				.RegisterType<DbContextService>()
				.As<IDbContextService>();

			builder
				.RegisterType<FakeTimeProvider>()
				.As<TimeProvider>();

			builder
				.RegisterType<FileRepository>()
				.As<IFileRepository>();
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
				.UpdateFileContentsAsync(default, default!, default, default)
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
		using TestDatabase database = new();

		FileEntity file = EntityFactory.CreateFile(contents: RandomValues.CreateBytes(10));

		IContentCipher contentCipher = Substitute.For<IContentCipher>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder.RegisterInstance(contentCipher);

			builder.RegisterInstance(database.Context);

			builder
				.RegisterType<DbAccess>()
				.As<IDbAccess>();

			builder
				.RegisterType<DbContextService>()
				.As<IDbContextService>();

			builder
				.RegisterType<FakeTimeProvider>()
				.As<TimeProvider>();

			builder
				.RegisterType<FileRepository>()
				.As<IFileRepository>();
		});

		IDbAccess dbAccess = mock.Create<IDbAccess>();

		await dbAccess.AddFilesAsync([file]);

		FileContentsWriter sut = mock.Create<FileContentsWriter>();

		// Act
		FileContentsWriting result = await sut.WriteAsync(
			file.Id,
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

		FileEntity[] stored = await dbAccess.GetAllFilesAsync(OptionalFileProperties.Contents);

		stored
			.Should()
			.ContainSingle()
			.Which
			.Contents
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// <see cref="FileContentsWriter.WriteAsync" />: contents the session key refuses to encrypt are not written.
	/// </summary>
	[Test]
	public async Task WriteAsync_Writes_Nothing_When_The_Encryption_Is_Refused()
	{
		// Arrange
		using TestDatabase database = new();

		byte[] original = RandomValues.CreateBytes(10);

		FileEntity file = EntityFactory.CreateFile(contents: original);

		// The database keeps dates to the second.
		DateTime updatedAt = new(2020, 1, 2, 3, 4, 5);

		file.UpdatedAt = updatedAt;

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IContentCipher contentCipher = Substitute.For<IContentCipher>();

			// The cipher swallows the failure and answers with a refusal.
			contentCipher
				.TryEncrypt(default, default, default!)
				.ReturnsForAnyArgs((byte[]?)null);

			builder.RegisterInstance(contentCipher);

			builder.RegisterInstance(database.Context);

			builder
				.RegisterType<DbAccess>()
				.As<IDbAccess>();

			builder
				.RegisterType<DbContextService>()
				.As<IDbContextService>();

			builder
				.RegisterType<FileRepository>()
				.As<IFileRepository>();
		});

		IDbAccess dbAccess = mock.Create<IDbAccess>();

		await dbAccess.AddFilesAsync([file]);

		FileContentsWriter sut = mock.Create<FileContentsWriter>();

		// Act
		FileContentsWriting result = await sut.WriteAsync(
			file.Id,
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

		FileEntity[] stored = await dbAccess.GetAllFilesAsync(OptionalFileProperties.Contents);

		stored
			.Should()
			.ContainSingle()
			.Which
			.Should()
			.BeEquivalentTo(new
			{
				Contents = original,
				UpdatedAt = updatedAt
			});
	}

	/// <summary>
	/// <see cref="FileContentsWriter.WriteAsync" />: the contents of a file with no keeper are written as they are.
	/// </summary>
	[Test]
	public async Task WriteAsync_Writes_The_Contents_As_They_Are_Without_A_Keeper()
	{
		// Arrange
		using TestDatabase database = new();

		FileEntity file = EntityFactory.CreateFile(contents: RandomValues.CreateBytes(10));

		byte[] contents = RandomValues.CreateBytes(10);

		IContentCipher contentCipher = Substitute.For<IContentCipher>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder.RegisterInstance(contentCipher);

			builder.RegisterInstance(database.Context);

			builder
				.RegisterType<DbAccess>()
				.As<IDbAccess>();

			builder
				.RegisterType<DbContextService>()
				.As<IDbContextService>();

			builder
				.RegisterType<FakeTimeProvider>()
				.As<TimeProvider>();

			builder
				.RegisterType<FileRepository>()
				.As<IFileRepository>();
		});

		IDbAccess dbAccess = mock.Create<IDbAccess>();

		await dbAccess.AddFilesAsync([file]);

		FileContentsWriter sut = mock.Create<FileContentsWriter>();

		// Act
		FileContentsWriting result = await sut.WriteAsync(
			file.Id,
			null,
			contents,
			FileContentsChange.Edit);

		// Assert
		result.Status
			.Should()
			.Be(FileContentsWriteStatus.Written);

		contentCipher
			.DidNotReceiveWithAnyArgs()
			.TryEncrypt(default, default, default!);

		FileEntity[] stored = await dbAccess.GetAllFilesAsync(OptionalFileProperties.Contents);

		stored
			.Should()
			.ContainSingle()
			.Which
			.Contents
			.Should()
			.Equal(contents);
	}
	#endregion
}
