using Autofac;
using Autofac.Extras.Moq;
using Avalonia.Headless.NUnit;
using AvaloniaEdit.Document;
using AwesomeAssertions;
using DataOrganizer.Documents.Editor.Interfaces;
using DataOrganizer.Infrastructure.Diagnostics.Interfaces;
using DataOrganizer.Infrastructure.Diagnostics.Services;
using DataOrganizer.Services.Documents;
using DataOrganizer.Shared.Threading.Interfaces;
using DataOrganizer.Shared.UnitTests.Threading.Fakes;
using DataOrganizer.ViewModels.Windows;
using DataOrganizer.Windows;

namespace DataOrganizer.UnitTests.Windows;

[TestFixture(Description = $@"Tests of ""{nameof(ConsoleWindow)}"" type")]
internal class ConsoleWindowTests
{
	#region Methods
	/// <summary>
	/// <see cref="ConsoleViewModel.Document" />: the records of the console log reach the editor of the window, which keeps
	/// no undo history of them.
	/// </summary>
	[AvaloniaTest]
	public void Document_Reaches_The_Editor()
	{
		// Arrange
		ConsoleLog consoleLog = new();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder
				.RegisterInstance(consoleLog)
				.As<IConsoleLog>();

			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();

			builder
				.RegisterType<AvaloniaLogText>()
				.As<ILogText>();
		});

		using ConsoleViewModel viewModel = mock.Create<ConsoleViewModel>();

		ConsoleWindow sut = new(viewModel);

		// Act
		consoleLog.Write("Record");

		// Assert
		TextDocument document = sut.Editor.Document;

		document.Text
			.Should()
			.Be("Record");

		document.UndoStack.CanUndo
			.Should()
			.BeFalse();
	}
	#endregion
}
