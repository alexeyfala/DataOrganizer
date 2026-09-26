using Autofac.Extras.Moq;
using Avalonia.Headless.NUnit;
using Avalonia.LogicalTree;
using AwesomeAssertions;
using DataOrganizer.ViewModels;
using DataOrganizer.Views;
using System;
using System.Linq;

namespace DataOrganizer.UnitTests.Views;

[TestFixture(Description = $@"Tests of ""{nameof(EmbeddedFileEditorView)}"" type")]
internal class EmbeddedFileEditorViewTests
{
	#region Methods
	/// <summary>
	/// <see cref="EmbeddedEditorViewModelBase.IsEncrypted" />: the text of an encrypted file reaches the editor
	/// as sensitive.
	/// </summary>
	[AvaloniaTest]
	public void IsEncrypted_Reaches_The_Editor([Values] bool isEncrypted)
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		using EmbeddedFileEditorViewModel viewModel = mock.Create<EmbeddedFileEditorViewModel>();

		viewModel.KeeperId = isEncrypted ? Guid.NewGuid() : null;

		// Act
		EmbeddedFileEditorView sut = new(viewModel);

		// Assert
		sut
			.GetLogicalDescendants()
			.OfType<DocumentEditorView>()
			.Single()
			.IsSensitive
			.Should()
			.Be(isEncrypted);
	}
	#endregion
}
