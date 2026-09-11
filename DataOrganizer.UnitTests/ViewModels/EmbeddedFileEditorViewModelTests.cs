using Autofac;
using Autofac.Extras.Moq;
using Avalonia.Headless.NUnit;
using AvaloniaEdit;
using AwesomeAssertions;
using DataOrganizer.Dto;
using DataOrganizer.Helpers.Text;
using DataOrganizer.ViewModels;
using NSubstitute;
using Repository.Dto;
using Repository.Interfaces.Database;
using Shared.Interfaces;
using Shared.Services;
using System;
using System.Threading.Tasks;
using TestSupport.Common;

namespace DataOrganizer.UnitTests.ViewModels;

[TestFixture(Description = $@"Tests of ""{nameof(EmbeddedFileEditorViewModel)}"" type")]
internal class EmbeddedFileEditorViewModelTests
{
	#region Methods
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
	#endregion
}
