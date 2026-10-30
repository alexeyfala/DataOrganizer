using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using Diagnostics.Interfaces;
using Entities.Enums;
using FileContents.Dto;
using FileContents.Enums;
using FileContents.Interfaces;
using FileContents.Services;
using Notifications.Interfaces;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TestSupport.Common;

namespace FileContents.UnitTests.Services;

[TestFixture(Description = $@"Tests of ""{nameof(FileContentsOpener)}"" type")]
internal class FileContentsOpenerTests
{
	#region Methods
	/// <summary>
	/// <see cref="FileContentsOpener.OpenAsync" />: opened contents come back, and nothing is told.
	/// </summary>
	[Test]
	public async Task OpenAsync_Gives_The_Opened_Contents()
	{
		// Arrange
		Guid fileId = Guid.NewGuid();

		Guid keeperId = Guid.NewGuid();

		byte[] contents = RandomValues.CreateBytes(10);

		INotificationService notification = Substitute.For<INotificationService>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IFileContentsReader reader = Substitute.For<IFileContentsReader>();

			reader
				.ReadUnlockedAsync(fileId, keeperId)
				.Returns(new FileContentsReading
				{
					Contents = contents,
					Status = FileContentsStatus.Opened
				});

			builder.RegisterInstance(notification);

			builder.RegisterInstance(reader);
		});

		FileContentsOpener sut = mock.Create<FileContentsOpener>();

		// Act
		byte[]? result = await sut.OpenAsync(fileId, keeperId, "a.txt", EntityKind.File);

		// Assert
		result
			.Should()
			.BeSameAs(contents);

		notification
			.DidNotReceiveWithAnyArgs()
			.ShowErrorSnackbar(default!, default!);
	}

	/// <summary>
	/// <see cref="FileContentsOpener.OpenAsync" />: a read the database could not answer goes to the reporter of database
	/// failures, and nothing comes back.
	/// </summary>
	[Test]
	public async Task OpenAsync_Hands_A_Failed_Read_To_The_Failure_Reporter()
	{
		// Arrange
		Guid fileId = Guid.NewGuid();

		InvalidOperationException failure = new();

		IDbFailureReporter failureReporter = Substitute.For<IDbFailureReporter>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IFileContentsReader reader = Substitute.For<IFileContentsReader>();

			reader
				.ReadUnlockedAsync(fileId, null)
				.ThrowsAsync(failure);

			builder.RegisterInstance(failureReporter);

			builder.RegisterInstance(reader);
		});

		FileContentsOpener sut = mock.Create<FileContentsOpener>();

		// Act
		byte[]? result = await sut.OpenAsync(fileId, null, "a.txt", EntityKind.File);

		// Assert
		result
			.Should()
			.BeNull();

		failureReporter
			.Received(1)
			.Report(failure, Arg.Any<string>());
	}

	/// <summary>
	/// <see cref="FileContentsOpener.OpenAsync" />: the report of a failed read names the file in quotes.
	/// </summary>
	[Test]
	public async Task OpenAsync_Names_The_File_In_The_Failure_Report()
	{
		// Arrange
		Guid fileId = Guid.NewGuid();

		IDbFailureReporter failureReporter = Substitute.For<IDbFailureReporter>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IFileContentsReader reader = Substitute.For<IFileContentsReader>();

			reader
				.ReadUnlockedAsync(fileId, null)
				.ThrowsAsync(new InvalidOperationException());

			builder.RegisterInstance(failureReporter);

			builder.RegisterInstance(reader);
		});

		FileContentsOpener sut = mock.Create<FileContentsOpener>();

		// Act
		await sut.OpenAsync(fileId, null, "a.txt", EntityKind.File);

		// Assert
		failureReporter
			.Received(1)
			.Report(Arg.Any<Exception>(), Arg.Is<string>(static x => x.EndsWith(@" ""a.txt""", StringComparison.Ordinal)));
	}

	/// <summary>
	/// <see cref="FileContentsOpener.OpenAsync" />: damaged contents and contents the key left closed give nothing back, and
	/// each shows an error of its own.
	/// </summary>
	[Test]
	public async Task OpenAsync_Tells_Damaged_Contents_Apart_From_Closed_Ones()
	{
		// Arrange
		Guid fileId = Guid.NewGuid();

		List<string> errors = [];

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			INotificationService notification = Substitute.For<INotificationService>();

			IFileContentsReader reader = Substitute.For<IFileContentsReader>();

			notification.ShowErrorSnackbar(Arg.Do<string>(errors.Add), Arg.Any<string>());

			reader
				.ReadUnlockedAsync(fileId, null)
				.Returns(
					new FileContentsReading
					{
						Status = FileContentsStatus.Damaged
					},
					new FileContentsReading
					{
						Status = FileContentsStatus.Closed
					});

			builder.RegisterInstance(notification);

			builder.RegisterInstance(reader);
		});

		FileContentsOpener sut = mock.Create<FileContentsOpener>();

		// Act
		byte[]?[] results =
		[
			await sut.OpenAsync(fileId, null, "a.txt", EntityKind.Dataset),
			await sut.OpenAsync(fileId, null, "a.txt", EntityKind.Dataset)
		];

		// Assert
		results
			.Should()
			.AllSatisfy(static x => x.Should().BeNull());

		errors
			.Should()
			.HaveCount(2)
			.And
			.OnlyHaveUniqueItems();
	}
	#endregion
}
