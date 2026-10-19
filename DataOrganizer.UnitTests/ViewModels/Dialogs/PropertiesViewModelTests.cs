using Autofac;
using Autofac.Extras.Moq;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Threading;
using AwesomeAssertions;
using DataOrganizer.Helpers.Views;
using DataOrganizer.Interfaces.Dialogs;
using DataOrganizer.Services.Dialogs;
using DataOrganizer.ViewModels.Dialogs;
using DialogHostAvalonia;
using System.Threading.Tasks;

namespace DataOrganizer.UnitTests.ViewModels.Dialogs;

[TestFixture(Description = $@"Tests of ""{nameof(PropertiesViewModel)}"" type")]
internal class PropertiesViewModelTests
{
	#region Methods
	/// <summary>
	/// <see cref="PropertiesViewModel.CloseCommand" />: the dialog of the main window closes.
	/// </summary>
	[AvaloniaTest]
	public void CloseCommand_Closes_The_Dialog_Of_The_Main_Window()
	{
		// Arrange
		Window window = new()
		{
			Content = new DialogHost
			{
				Identifier = DialogHostIdentifiers.Main
			}
		};

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder
				.RegisterType<DialogHostCloser>()
				.As<IDialogHostCloser>();
		});

		PropertiesViewModel sut = mock.Create<PropertiesViewModel>();

		window.Show();

		Task dialogClosed = DialogHost.Show(new TextBlock(), DialogHostIdentifiers.Main);

		Dispatcher.UIThread.RunJobs();

		// Act
		sut.CloseCommand.Execute(null);

		bool isClosed = dialogClosed.IsCompleted;

		// Closed before the assertion: a closed window takes its host out of the list every headless test shares.
		window.Close();

		// Assert
		isClosed
			.Should()
			.BeTrue();
	}
	#endregion
}
