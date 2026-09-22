using Autofac;
using Autofac.Extras.Moq;
using Avalonia.Headless.NUnit;
using AvaloniaEdit;
using AwesomeAssertions;
using DataOrganizer.Dto;
using DataOrganizer.Helpers.Text;
using DataOrganizer.Interfaces.Diagnostics;
using DataOrganizer.Messages.Editor;
using DataOrganizer.ViewModels;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Repository.Dto;
using Repository.Interfaces.Database;
using Shared.Interfaces;
using Shared.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TestSupport.Common;

namespace DataOrganizer.UnitTests.ViewModels;

[TestFixture(Description = $@"Tests of ""{nameof(EmbeddedFileEditorViewModel)}"" type")]
internal class EmbeddedFileEditorViewModelTests
{
	#region Methods
	/// <summary>
	/// <see cref="EmbeddedFileEditorViewModel.EditorLoaded" />: a read the database could not answer closes
	/// the editor for changes.
	/// </summary>
	[AvaloniaTest]
	public async Task EditorLoaded_Closes_The_Editor_When_The_Read_Fails()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			dbAccess
				.GetFileContentsAsync(Arg.Any<Guid>())
				.ThrowsAsync(new InvalidOperationException());

			builder.RegisterInstance(dbAccess);
		});

		using EmbeddedFileEditorViewModel sut = mock.Create<EmbeddedFileEditorViewModel>();

		TextEditor editor = Substitute.For<TextEditor>();

		// Act
		await sut.EditorLoaded(editor);

		// Assert
		sut.IsContentUnavailable
			.Should()
			.BeTrue();

		sut.IsEditingEnabled
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="EmbeddedFileEditorViewModel.EditorLoaded" />: loads the file contents into the editor and applies the stored editor state (font size, word wrap).
	/// </summary>
	[AvaloniaTest]
	public async Task EditorLoaded_Loads_Text_To_Editor()
	{
		// Arrange
		byte[] contents = RandomValues.CreateBytes(10);

		double fontSize = RandomValues.CreateDouble(6.0, 64.0);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			ValidatedContents fileContents = new()
			{
				Contents = [.. contents],
				IsValid = true
			};

			dbAccess
				.GetFileContentsAsync(Arg.Any<Guid>())
				.Returns(fileContents);

			FileEditorState state = new()
			{
				CaretPosition = default,
				FontSize = fontSize,
				WordWrap = true,
				ScrollOffset = default,
				SelectionLength = default,
				SelectionStart = default
			};

			dbAccess
				.GetFileEditorStateAsync(Arg.Any<Guid>())
				.Returns(new SystemTextJsonSerializer().Serialize(state));

			IJsonSerializer serializer = Substitute.For<IJsonSerializer>();

			serializer
				.Deserialize<FileEditorState>(Arg.Any<string>())
				.Returns(state);

			builder.RegisterInstance(serializer);

			builder.RegisterInstance(dbAccess);
		});

		using EmbeddedFileEditorViewModel sut = mock.Create<EmbeddedFileEditorViewModel>();

		TextEditor editor = Substitute.For<TextEditor>();

		// Act
		await sut.EditorLoaded(editor);

		// Assert
		sut.IsInitialized
			.Should()
			.BeTrue();

		editor.Text
			.Should()
			.Be(TextDefaults.Encoding.GetString(contents));

		sut.WordWrap
			.Should()
			.BeTrue();

		sut.FontSize
			.Should()
			.Be(fontSize);
	}

	/// <summary>
	/// <see cref="EmbeddedFileEditorViewModel.EditorLoaded" />: contents read successfully open the document for editing.
	/// </summary>
	[AvaloniaTest]
	public async Task EditorLoaded_Opens_The_Document_For_Editing()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			ValidatedContents fileContents = new()
			{
				Contents = RandomValues.CreateBytes(10),
				IsValid = true
			};

			dbAccess
				.GetFileContentsAsync(Arg.Any<Guid>())
				.Returns(fileContents);

			dbAccess
				.GetFileEditorStateAsync(Arg.Any<Guid>())
				.Returns((string?)null);

			builder.RegisterInstance(dbAccess);
		});

		using EmbeddedFileEditorViewModel sut = mock.Create<EmbeddedFileEditorViewModel>();

		TextEditor editor = Substitute.For<TextEditor>();

		// Act
		await sut.EditorLoaded(editor);

		sut.IsEditingEnabled
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="EmbeddedEditorViewModelBase.Receive(FlushEditorsMessage)" />: a flush that arrives while the contents
	/// are loading queues nothing, so the still empty editor never overwrites the file.
	/// </summary>
	[AvaloniaTest]
	public async Task Receive_Flush_While_Loading_Writes_Nothing()
	{
		// Arrange
		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		TaskCompletionSource<ValidatedContents> read = new();

		List<Task> watched = [];

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			dbAccess
				.GetFileContentsAsync(Arg.Any<Guid>())
				.Returns(read.Task);

			dbAccess
				.GetFileEditorStateAsync(Arg.Any<Guid>())
				.Returns((string?)null);

			ITaskExceptionHandler exceptionHandler = Substitute.For<ITaskExceptionHandler>();

			exceptionHandler.Watch(Arg.Do<Task>(watched.Add));

			builder.RegisterInstance(dbAccess);

			builder.RegisterInstance(exceptionHandler);
		});

		using EmbeddedFileEditorViewModel sut = mock.Create<EmbeddedFileEditorViewModel>();

		TextEditor editor = Substitute.For<TextEditor>();

		Task loading = sut.EditorLoaded(editor);

		FlushEditorsMessage flush = new();

		// Act
		sut.Receive(flush);

		IReadOnlyCollection<bool> responses = await flush.GetResponsesAsync();

		read.SetResult(new()
		{
			Contents = RandomValues.CreateBytes(10),
			IsValid = true
		});

		await loading;

		// Completing the save channel lets its consumer run to the end.
		sut.Dispose();

		await Task.WhenAll(watched);

		// Assert
		await dbAccess
			.DidNotReceiveWithAnyArgs()
			.UpdateFilePropertiesAsync(default, default!, default);

		responses
			.Should()
			.Equal(true);
	}
	#endregion
}
