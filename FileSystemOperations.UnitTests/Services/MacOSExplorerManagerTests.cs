using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using FileSystemOperations.Services;
using NSubstitute;
using Shared.Common;
using Shared.Interfaces;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using TestSupport.Common;

namespace FileSystemOperations.UnitTests.Services;

[TestFixture(Description = $@"Tests of ""{nameof(MacOSExplorerManager)}"" type")]
internal class MacOSExplorerManagerTests
{
	#region Data
	/// <summary>
	/// Name of the folder that holds the file.
	/// </summary>
	private const string FolderName = "Docs";

	/// <summary>
	/// Path of the file to reveal.
	/// </summary>
	private static readonly string FilePath = TestPath.Combine(FolderName, "notes.txt");

	/// <summary>
	/// Path of the folder that holds the file.
	/// </summary>
	private static readonly string FolderPath = TestPath.Combine(FolderName);
	#endregion

	#region Methods
	/// <summary>
	/// <see cref="MacOSExplorerManager.TryForegroundFolder" />: no Finder window is brought forward, so the caller opens one.
	/// </summary>
	[Test]
	public void TryForegroundFolder_Brings_No_Window_Forward()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		MacOSExplorerManager sut = mock.Create<MacOSExplorerManager>();

		// Act
		bool result = sut.TryForegroundFolder(FolderPath, FilePath);

		// Assert
		result
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="MacOSExplorerManager.TryRevealFile" />: when the opener cannot start, the caller is told so.
	/// </summary>
	[Test]
	public void TryRevealFile_Fails_When_The_Opener_Cannot_Start()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IProcessLauncher processLauncher = Substitute.For<IProcessLauncher>();

			processLauncher
				.When(x => x.StartProcess(Arg.Any<string>(), Arg.Any<IReadOnlyList<string>>()))
				.Throw(new Win32Exception());

			builder.RegisterInstance(processLauncher);
		});

		MacOSExplorerManager sut = mock.Create<MacOSExplorerManager>();

		// Act
		bool result = sut.TryRevealFile(FilePath);

		// Assert
		result
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="MacOSExplorerManager.TryRevealFile" />: the opener starts with the switch that selects the file in
	/// Finder.
	/// </summary>
	[Test]
	public void TryRevealFile_Starts_The_Opener_With_The_Reveal_Switch()
	{
		// Arrange
		IProcessLauncher processLauncher = Substitute.For<IProcessLauncher>();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(processLauncher));

		MacOSExplorerManager sut = mock.Create<MacOSExplorerManager>();

		// Act
		bool result = sut.TryRevealFile(FilePath);

		// Assert
		result
			.Should()
			.BeTrue();

		processLauncher.Received(1).StartProcess(
			PlatformInfo.FileOpener,
			Arg.Is<IReadOnlyList<string>>(x => x.SequenceEqual(new[] { "-R", FilePath })));
	}
	#endregion
}
