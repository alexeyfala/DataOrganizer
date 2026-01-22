using Autofac;
using Autofac.Extras.Moq;
using Avalonia.Headless.NUnit;
using AvaloniaEdit;
using AwesomeAssertions;
using CommonTestHelpers.Helpers;
using DataOrganizer.DTO;
using DataOrganizer.Interfaces;
using DataOrganizer.ViewModels;
using NSubstitute;
using Repository.DTO;
using Repository.Interfaces;
using Shared.Interfaces;
using Shared.Services;
using System;
using System.Threading.Tasks;

namespace DataOrganizer.UnitTests.TestTypes;

[TestFixture(Description = $@"Tests of ""{nameof(EmbeddedFileEditorViewModel)}"" type")]
internal class EmbeddedFileEditorViewModelTests
{
	#region Methods
	/// <summary>
	/// Test of <see cref="EmbeddedFileEditorViewModel.EditorLoaded(TextEditor)" />.
	/// </summary>
	[AvaloniaTest]
	public async Task EditorLoaded_Loads_Text_To_Editor()
	{
		// Arrange
		byte[] contents = TestUtils.CreateRandomBytes(10);

		double fontSize = TestUtils.CreateRandomDouble(6.0, 64.0);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			ContentsIsValidPair pair = new()
			{
				Contents = contents,
				IsValid = true
			};

			dbAccess
				.GetFileContentsAsync(Arg.Any<Guid>())
				.Returns(pair);

			FileProperties properties = new()
			{
				CaretPosition = default,
				FontSize = fontSize,
				IsWordWrap = true,
				ScrollOffset = default,
				SelectionLength = default,
				SelectionStart = default
			};

			dbAccess
				.GetFilePropertiesAsync(Arg.Any<Guid>())
				.Returns(new JsonSerializerWrapper().Serialize(properties));

			IJsonSerializerWrapper serializer = Substitute.For<IJsonSerializerWrapper>();

			serializer
				.Deserialize<FileProperties>(Arg.Any<string>())
				.Returns(properties);

			builder.RegisterInstance(serializer);

			builder.RegisterInstance(dbAccess);
		});

		using EmbeddedFileEditorViewModel sut = mock.Create<EmbeddedFileEditorViewModel>();

		TextEditor editor = Substitute.For<TextEditor>();

		// Act
		await sut
			.EditorLoaded(editor)
			.ConfigureAwait(true);

		// Assert
		sut.IsInitialized
			.Should()
			.BeTrue();

		editor.Text
			.Should()
			.Be(IFileEditor.Utf8Encoding.GetString(contents));

		sut.IsWordWrap
			.Should()
			.BeTrue();

		sut.FontSize
			.Should()
			.Be(fontSize);
	}
	#endregion
}
