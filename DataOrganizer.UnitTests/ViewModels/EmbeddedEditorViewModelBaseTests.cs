using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using CommunityToolkit.Mvvm.Messaging;
using DataOrganizer.Interfaces.Runtime;
using DataOrganizer.UnitTests.Factories;
using DataOrganizer.ViewModels;
using Diagnostics.Interfaces;
using EditorDocuments.Interfaces;
using EditorMessages.Messages;
using FileContents.Dto;
using FileContents.Enums;
using FileContents.Interfaces;
using Notifications.Interfaces;
using NSubstitute;
using Serilog;
using Shared.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TestSupport.Common;

namespace DataOrganizer.UnitTests.ViewModels;

[TestFixture(Description = $@"Tests of ""{nameof(EmbeddedEditorViewModelBase)}"" type")]
internal class EmbeddedEditorViewModelBaseTests
{
	#region Methods
	/// <summary>
	/// <see cref="EmbeddedEditorViewModelBase.IsEditingEnabled" />: turning the read-only mode on closes the editor
	/// and is reported, so that the editor follows it.
	/// </summary>
	[Test]
	public void IsEditingEnabled_Follows_The_Read_Only_Mode()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		TestEditor sut = mock.Create<TestEditor>();

		sut.MarkInitialized();

		List<string?> changed = [];

		sut.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

		// Act
		sut.Receive(new EditorReadOnlyModeChangedMessage(true));

		// Assert
		sut.IsEditingEnabled
			.Should()
			.BeFalse();

		changed
			.Should()
			.Contain(nameof(EmbeddedEditorViewModelBase.IsEditingEnabled));
	}

	/// <summary>
	/// <see cref="EmbeddedEditorViewModelBase.IsEditingEnabled" />: the editor stays closed for changes
	/// until its contents are loaded.
	/// </summary>
	[Test]
	public void IsEditingEnabled_Is_False_Before_The_Contents_Are_Loaded()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		// Act
		TestEditor sut = mock.Create<TestEditor>();

		// Assert
		sut.IsEditingEnabled
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="EmbeddedEditorViewModelBase.IsEditingEnabled" />: the end of loading opens the editor
	/// and is reported, so that the editor follows it.
	/// </summary>
	[Test]
	public void IsEditingEnabled_Is_Reported_When_The_Editor_Is_Initialized()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		TestEditor sut = mock.Create<TestEditor>();

		List<string?> changed = [];

		sut.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

		// Act
		sut.MarkInitialized();

		// Assert
		sut.IsEditingEnabled
			.Should()
			.BeTrue();

		changed
			.Should()
			.Contain(nameof(EmbeddedEditorViewModelBase.IsEditingEnabled));
	}

	/// <summary>
	/// <see cref="EmbeddedEditorViewModelBase.IsReadOnly" />: the changes that wait for a pause in typing are queued
	/// once, before the read-only mode turns on, and not when it turns off.
	/// </summary>
	[Test]
	public void IsReadOnly_Queues_The_Pending_Changes_Before_It_Turns_On()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		TestEditor sut = mock.Create<TestEditor>();

		// Act
		sut.Receive(new EditorReadOnlyModeChangedMessage(true));

		sut.Receive(new EditorReadOnlyModeChangedMessage(false));

		// Assert
		sut.QueuedWhileReadOnly
			.Should()
			.Equal(false);
	}

	/// <summary>
	/// <see cref="EmbeddedEditorViewModelBase.SaveContentsAsync" />: a write without a date of change, as of a view change,
	/// reports no date.
	/// </summary>
	[Test]
	public async Task SaveContentsAsync_Reports_No_Date_When_None_Is_Written()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IFileContentsWriter writer = Substitute.For<IFileContentsWriter>();

			writer
				.WriteAsync(default, default, default!, default, default)
				.ReturnsForAnyArgs(new FileContentsWriting
				{
					Status = FileContentsWriteStatus.Written
				});

			builder.RegisterInstance(writer);
		});

		TestEditor sut = mock.Create<TestEditor>();

		DateTime? reported = null;

		sut.Initialize(EmbeddedEditorSetupFactory.CreateSetup(setUpdatedAtCallback: x => reported = x));

		// Act
		await sut.InvokeSaveContentsAsync(RandomValues.CreateBytes(10), FileContentsChange.View);

		// Assert
		reported
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="EmbeddedEditorViewModelBase.SaveContentsAsync" />: the contents go to the writer with the file and its
	/// keeper, and the date of change written with an edit is reported.
	/// </summary>
	[Test]
	public async Task SaveContentsAsync_Reports_The_Date_Of_Change_Of_An_Edit()
	{
		// Arrange
		byte[] contents = RandomValues.CreateBytes(10);

		DateTime updatedAt = new(2026, 10, 12, 18, 30, 0);

		IFileContentsWriter writer = Substitute.For<IFileContentsWriter>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			writer
				.WriteAsync(default, default, default!, default, default)
				.ReturnsForAnyArgs(new FileContentsWriting
				{
					Status = FileContentsWriteStatus.Written,
					UpdatedAt = updatedAt
				});

			builder.RegisterInstance(writer);
		});

		TestEditor sut = mock.Create<TestEditor>();

		DateTime? reported = null;

		sut.Initialize(EmbeddedEditorSetupFactory.CreateSetup(
			fileId: Guid.NewGuid(),
			keeperId: Guid.NewGuid(),
			setUpdatedAtCallback: x => reported = x));

		// Act
		bool isWritten = await sut.InvokeSaveContentsAsync(contents, FileContentsChange.Edit);

		// Assert
		isWritten
			.Should()
			.BeTrue();

		reported
			.Should()
			.Be(updatedAt);

		await writer.Received(1).WriteAsync(
			sut.FileId,
			sut.KeeperId,
			contents,
			FileContentsChange.Edit,
			Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="EmbeddedEditorViewModelBase.SaveContentsAsync" />: a file gone from the database is told apart from contents
	/// the key of the session refused, and neither is saved.
	/// </summary>
	[Test]
	public async Task SaveContentsAsync_Tells_A_Missing_File_Apart_From_Refused_Contents()
	{
		// Arrange
		List<string> errors = [];

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IFileContentsWriter writer = Substitute.For<IFileContentsWriter>();

			INotificationService notification = Substitute.For<INotificationService>();

			writer
				.WriteAsync(default, default, default!, default, default)
				.ReturnsForAnyArgs(
					new FileContentsWriting
					{
						Status = FileContentsWriteStatus.Closed
					},
					new FileContentsWriting
					{
						Status = FileContentsWriteStatus.Missing
					});

			notification.ShowErrorSnackbar(Arg.Do<string>(errors.Add), Arg.Any<string>());

			builder.RegisterInstance(notification);

			builder.RegisterInstance(writer);
		});

		TestEditor sut = mock.Create<TestEditor>();

		// Act
		bool[] written =
		[
			await sut.InvokeSaveContentsAsync(RandomValues.CreateBytes(10), FileContentsChange.Edit),
			await sut.InvokeSaveContentsAsync(RandomValues.CreateBytes(10), FileContentsChange.Edit)
		];

		// Assert
		written
			.Should()
			.Equal(false, false);

		errors
			.Should()
			.HaveCount(2)
			.And
			.OnlyHaveUniqueItems();
	}
	#endregion
}

// TestEditor is a top-level internal type so the CommunityToolkit messenger source generator
// (IMessengerRegisterAllGenerator), which emits a RegisterAll for every IRecipient<T> recipient,
// can access it — a private nested type would be inaccessible to the generated code.

/// <summary>
/// Minimal concrete editor exposing the protected members under test.
/// </summary>
internal sealed class TestEditor : EmbeddedEditorViewModelBase
{
	public TestEditor(
		IDbFailureReporter dbFailureReporter,
		IEditorStateStore editorStateStore,
		IFileContentsOpener fileContentsOpener,
		IFileContentsWriter fileContentsWriter,
		IJsonSerializer jsonSerializer,
		ILogger logger,
		IMessenger messenger,
		INotificationService notification,
		ISessionReadOnlyMode sessionReadOnlyMode,
		ITaskExceptionHandler exceptionHandler) : base(
			dbFailureReporter,
			editorStateStore,
			fileContentsOpener,
			fileContentsWriter,
			jsonSerializer,
			logger,
			messenger,
			notification,
			sessionReadOnlyMode,
			exceptionHandler)
	{
	}

	/// <summary>
	/// For each queuing of the pending changes, <c>true</c> when the read-only mode was already on.
	/// </summary>
	public List<bool> QueuedWhileReadOnly { get; } = [];

	public Task<bool> InvokeSaveContentsAsync(byte[] contents, FileContentsChange change)
	{
		return SaveContentsAsync(contents, change);
	}

	public void MarkInitialized() => IsInitialized = true;

	/// <inheritdoc />
	protected override Task LoadContentsAsync(byte[] contents) => Task.CompletedTask;

	/// <inheritdoc />
	protected override void QueuePendingChanges() => QueuedWhileReadOnly.Add(IsReadOnly);
}
