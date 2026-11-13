using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using DataOrganizer.SystemIntegration.FileExecution.Dto;
using DataOrganizer.SystemIntegration.FileExecution.Interfaces;
using DataOrganizer.SystemIntegration.FileExecution.Services;
using DataOrganizer.TestSupport.Common;
using NSubstitute;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.SystemIntegration.UnitTests.FileExecution.Services;

[TestFixture(Description = $@"Tests of ""{nameof(WindowsAppResolver)}"" type")]
internal class WindowsAppResolverTests
{
	#region Data
	/// <summary>
	/// Path of the application the user picks to open the file.
	/// </summary>
	private const string PickedAppPath = @"C:\Apps\picked.exe";

	/// <summary>
	/// Path of the file to open.
	/// </summary>
	private static readonly string FilePath = TestPath.Combine("Sandbox", "notes.txt");
	#endregion

	#region Methods
	/// <summary>
	/// <see cref="WindowsAppResolver.ResolveAsync" />: a file with an associated program is left to the system, and the
	/// user is not asked.
	/// </summary>
	[Test]
	public async Task ResolveAsync_Leaves_A_File_With_An_Associated_Program_To_The_System()
	{
		// Arrange
		IAppPickerService appPicker = Substitute.For<IAppPickerService>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IFileAssociationService fileAssociation = Substitute.For<IFileAssociationService>();

			fileAssociation
				.HasAssociatedApp(".txt")
				.Returns(true);

			builder.RegisterInstance(appPicker);

			builder.RegisterInstance(fileAssociation);
		});

		WindowsAppResolver sut = mock.Create<WindowsAppResolver>();

		// Act
		AppResolution result = await sut.ResolveAsync(FilePath);

		// Assert
		result
			.Should()
			.Be(new AppResolution(IsCancelled: false, AppPath: null));

		await appPicker
			.DidNotReceive()
			.PickAppAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="WindowsAppResolver.ResolveAsync" />: the user who closes the picker without a program cancels the
	/// opening.
	/// </summary>
	[Test]
	public async Task ResolveAsync_Reports_A_Cancelled_Choice()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IAppPickerService appPicker = Substitute.For<IAppPickerService>();

			IFileAssociationService fileAssociation = Substitute.For<IFileAssociationService>();

			// The user closes the picker without a program.
			appPicker
				.PickAppAsync(FilePath, Arg.Any<CancellationToken>())
				.Returns((AssociatedAppInfo?)null);

			// No program is associated with the extension.
			fileAssociation
				.HasAssociatedApp(".txt")
				.Returns(false);

			builder.RegisterInstance(appPicker);

			builder.RegisterInstance(fileAssociation);
		});

		WindowsAppResolver sut = mock.Create<WindowsAppResolver>();

		// Act
		AppResolution result = await sut.ResolveAsync(FilePath);

		// Assert
		result
			.Should()
			.Be(new AppResolution(IsCancelled: true, AppPath: null));
	}

	/// <summary>
	/// <see cref="WindowsAppResolver.ResolveAsync" />: without an associated program the program the user picks opens the
	/// file.
	/// </summary>
	[Test]
	public async Task ResolveAsync_Returns_The_Picked_Program_Without_An_Associated_Program()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IAppPickerService appPicker = Substitute.For<IAppPickerService>();

			IFileAssociationService fileAssociation = Substitute.For<IFileAssociationService>();

			appPicker
				.PickAppAsync(FilePath, Arg.Any<CancellationToken>())
				.Returns(new AssociatedAppInfo
				{
					AppName = "Picked",
					AppPath = PickedAppPath
				});

			fileAssociation
				.HasAssociatedApp(".txt")
				.Returns(false);

			builder.RegisterInstance(appPicker);

			builder.RegisterInstance(fileAssociation);
		});

		WindowsAppResolver sut = mock.Create<WindowsAppResolver>();

		// Act
		AppResolution result = await sut.ResolveAsync(FilePath);

		// Assert
		result
			.Should()
			.Be(new AppResolution(IsCancelled: false, AppPath: PickedAppPath));
	}
	#endregion
}
