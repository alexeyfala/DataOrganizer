using Autofac;
using Autofac.Extras.Moq;
using Avalonia.Headless.NUnit;
using AwesomeAssertions;
using DataOrganizer.Interfaces.Runtime;
using DataOrganizer.Interfaces.Storage;
using DataOrganizer.Interfaces.Views;
using DataOrganizer.Services.Views;
using DataOrganizer.UnitTests.Fakes;
using DataOrganizer.ViewModels.Windows;
using NSubstitute;

namespace DataOrganizer.UnitTests.Services.Views;

[TestFixture(Description = $@"Tests of ""{nameof(ConsoleWindowHost)}"" type")]
internal class ConsoleWindowHostTests
{
	#region Methods
	/// <summary>
	/// <see cref="ConsoleWindowHost.Write" />: the text reaches the log of the console before its window opens.
	/// </summary>
	[AvaloniaTest]
	public void Write_Appends_The_Text_To_The_Console_Log()
	{
		// Arrange
		ConsoleViewModel viewModel = new(
			Substitute.For<IAppEnvironment>(),
			Substitute.For<IDirectoryAccessor>(),
			new InlineDispatcherAccessor());

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IViewFactory viewFactory = Substitute.For<IViewFactory>();

			viewFactory
				.CreateViewModel<ConsoleViewModel>()
				.Returns(viewModel);

			builder.RegisterInstance(viewFactory);
		});

		ConsoleWindowHost sut = mock.Create<ConsoleWindowHost>();

		// Act
		sut.Write("record");

		// Assert
		viewModel.Document.Text
			.Should()
			.Be("record");
	}
	#endregion
}
