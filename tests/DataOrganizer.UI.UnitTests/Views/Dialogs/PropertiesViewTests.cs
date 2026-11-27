using Autofac;
using Autofac.Extras.Moq;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using Avalonia.Threading;
using DataOrganizer.Infrastructure.DialogContracts.Interfaces;
using DataOrganizer.UI.ViewModels.Dialogs;
using DataOrganizer.UI.Views.Dialogs;
using NSubstitute;

namespace DataOrganizer.UI.UnitTests.Views.Dialogs;

[TestFixture(Description = $@"Tests of ""{nameof(PropertiesView)}"" type")]
internal sealed class PropertiesViewTests
{
	#region Methods
	/// <summary>
	/// <see cref="PropertiesViewModel.CloseCommand" />: the Escape key closes the dialog.
	/// </summary>
	/// <remarks>
	/// The close rests on Avalonia wiring <see cref="Button.IsCancel" /> to the key, so nothing but a test through the real
	/// input pipeline covers it.
	/// </remarks>
	[AvaloniaTest]
	public void Escape_Closes_The_Dialog()
	{
		// Arrange
		IDialogHostCloser dialogHostCloser = Substitute.For<IDialogHostCloser>();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(dialogHostCloser));

		PropertiesViewModel viewModel = mock.Create<PropertiesViewModel>();

		// The view carries the button on its own; a dialog host would only add a registry shared with every other test.
		Window window = new()
		{
			Content = new PropertiesView(viewModel)
		};

		window.Show();

		Dispatcher
			.UIThread
			.RunJobs();

		// Act
		window.KeyPressQwerty(PhysicalKey.Escape, RawInputModifiers.None);

		window.KeyReleaseQwerty(PhysicalKey.Escape, RawInputModifiers.None);

		Dispatcher
			.UIThread
			.RunJobs();

		window.Close();

		// Assert
		dialogHostCloser
			.Received(1)
			.Close();
	}
	#endregion
}
