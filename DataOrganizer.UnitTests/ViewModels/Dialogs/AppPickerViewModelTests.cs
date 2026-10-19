using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using DataOrganizer.Dto.Dialogs;
using DataOrganizer.Interfaces.Dialogs;
using DataOrganizer.ViewModels.Dialogs;
using NSubstitute;
using System.Linq;
using System.Threading.Tasks;

namespace DataOrganizer.UnitTests.ViewModels.Dialogs;

[TestFixture(Description = $@"Tests of ""{nameof(AppPickerViewModel)}"" type")]
internal class AppPickerViewModelTests
{
	#region Methods
	/// <summary>
	/// <see cref="AppPickerViewModel.BrowseCommand" />: the file picker offers one file to choose among the executables.
	/// </summary>
	[Test]
	public async Task BrowseCommand_Offers_One_Executable()
	{
		// Arrange
		FileOpenParameters? parameters = null;

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IFileSystemPicker picker = Substitute.For<IFileSystemPicker>();

			picker.SelectFilesAsync(Arg.Do<FileOpenParameters>(x => parameters = x));

			builder.RegisterInstance(picker);
		});

		AppPickerViewModel sut = mock.Create<AppPickerViewModel>();

		// Act
		await sut.BrowseCommand.ExecuteAsync(null);

		// Assert
		parameters!.AllowMultiple
			.Should()
			.BeFalse();

		parameters.FileTypes.Select(static x => (x.Name, string.Join(", ", x.Patterns)))
			.Should()
			.Equal(("Executable", "*.exe"));
	}
	#endregion
}
