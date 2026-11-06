using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using FileSystemOperations.Services;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shared.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Testably.Abstractions.Testing;
using TestSupport.Common;

namespace FileSystemOperations.UnitTests.Services;

[TestFixture(Description = $@"Tests of ""{nameof(LinuxExplorerManager)}"" type")]
internal class LinuxExplorerManagerTests
{
	#region Data
	/// <summary>
	/// Entry of the process in <c>/proc</c> that holds its name.
	/// </summary>
	private const string CommPath = "/proc/4242/comm";

	/// <summary>
	/// D-Bus tool of the dbus package, the second way to reveal a file.
	/// </summary>
	private const string DbusSend = "dbus-send";

	/// <summary>
	/// D-Bus tool of GLib, the first way to reveal a file.
	/// </summary>
	private const string Gdbus = "gdbus";

	/// <summary>
	/// Id of the process that owns the window.
	/// </summary>
	private const int Pid = 4242;

	/// <summary>
	/// Folder of the process in <c>/proc</c>.
	/// </summary>
	private const string ProcessFolder = "/proc/4242";

	/// <summary>
	/// Path of the file to reveal.
	/// </summary>
	private static readonly string FilePath = TestPath.Combine("Docs", "notes.txt");
	#endregion

	#region Methods
	/// <summary>
	/// <see cref="LinuxExplorerManager.IsKnownFileManagerProcess" />: the name of the process is compared with the
	/// names of the known file managers exactly, without the line break that ends it.
	/// </summary>
	[TestCase("nautilus", ExpectedResult = true)]
	[TestCase("Thunar", ExpectedResult = true)]
	[TestCase("bash", ExpectedResult = false)]
	[TestCase("NAUTILUS", ExpectedResult = false)]
	public bool IsKnownFileManagerProcess_Matches_The_Exact_Name_Of_A_File_Manager(string name)
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			MockFileSystem files = new();

			files
				.Directory
				.CreateDirectory(ProcessFolder);

			// The kernel ends the name with a line break.
			files
				.File
				.WriteAllText(CommPath, name + "\n");

			builder.RegisterInstance<IFileSystem>(files);
		});

		LinuxExplorerManager sut = mock.Create<LinuxExplorerManager>();

		// Act
		return sut.IsKnownFileManagerProcess(Pid);
	}

	/// <summary>
	/// <see cref="LinuxExplorerManager.IsKnownFileManagerProcess" />: a process that is gone is no file manager.
	/// </summary>
	[Test]
	public void IsKnownFileManagerProcess_Rejects_A_Process_That_Is_Gone()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			// An empty disk: the process has no entry in /proc.
			builder
				.RegisterType<MockFileSystem>()
				.As<IFileSystem>()
				.SingleInstance();
		});

		LinuxExplorerManager sut = mock.Create<LinuxExplorerManager>();

		// Act
		bool result = sut.IsKnownFileManagerProcess(Pid);

		// Assert
		result
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="LinuxExplorerManager.ReadLongs" />: the data of a property of another format gives nothing.
	/// </summary>
	[Test]
	public void ReadLongs_Gives_Nothing_For_Another_Format([Values(8, 16)] int format)
	{
		// Arrange
		byte[] bytes = new byte[IntPtr.Size];

		// Act
		IntPtr[]? result = ReadNative(bytes, x => LinuxExplorerManager.ReadLongs(x, format, 1));

		// Assert
		result
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="LinuxExplorerManager.ReadLongs" />: the data of a 32-bit property comes back as native longs.
	/// </summary>
	[Test]
	public void ReadLongs_Reads_The_Native_Longs_Of_A_32_Bit_Property()
	{
		// Arrange
		IntPtr[] windows = [0x1A00003, 0x2C00007, 0x3E0000B];

		byte[] bytes = MemoryMarshal
			.AsBytes(windows.AsSpan())
			.ToArray();

		// Act
		IntPtr[]? result = ReadNative(bytes, x => LinuxExplorerManager.ReadLongs(x, 32, windows.Length));

		// Assert
		result
			.Should()
			.Equal(windows);
	}

	/// <summary>
	/// <see cref="LinuxExplorerManager.ReadUtf8" />: the data of a string property is decoded as UTF-8.
	/// </summary>
	[Test]
	public void ReadUtf8_Decodes_The_Data_As_Utf8()
	{
		// Arrange
		const string title = "Документы — Files";

		byte[] bytes = Encoding.UTF8.GetBytes(title);

		// Act
		string? result = ReadNative(bytes, x => LinuxExplorerManager.ReadUtf8(x, 8, bytes.Length));

		// Assert
		result
			.Should()
			.Be(title);
	}

	/// <summary>
	/// <see cref="LinuxExplorerManager.ReadUtf8" />: an empty string property gives nothing.
	/// </summary>
	[Test]
	public void ReadUtf8_Gives_Nothing_For_An_Empty_Property()
	{
		// Act
		string? result = ReadNative([0], x => LinuxExplorerManager.ReadUtf8(x, 8, 0));

		// Assert
		result
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="LinuxExplorerManager.TitleMatchesFolderName" />: matches an exact title or a "&lt;name&gt; &lt;suffix&gt;" title, and rejects anything else,
	/// a title shorter than the name or a name left empty included.
	/// </summary>
	[TestCase("Docs", "Docs", ExpectedResult = true)]
	[TestCase("Docs - Files", "Docs", ExpectedResult = true)]
	[TestCase("Docs ", "Docs", ExpectedResult = true)]
	[TestCase("Documents", "Doc", ExpectedResult = false)]
	[TestCase("Other", "Docs", ExpectedResult = false)]
	[TestCase("Do", "Docs", ExpectedResult = false)]
	[TestCase("", "Docs", ExpectedResult = false)]
	[TestCase("Docs", "", ExpectedResult = false)]
	[TestCase(" Docs", "", ExpectedResult = false)]
	public bool TitleMatchesFolderName_Matches_Exact_And_Prefixed_Titles(string title, string folderName)
	{
		return LinuxExplorerManager.TitleMatchesFolderName(title, folderName);
	}

	/// <summary>
	/// <see cref="LinuxExplorerManager.TryRevealFile" />: when neither D-Bus tool reveals the file, the caller is told so.
	/// </summary>
	[Test]
	public void TryRevealFile_Fails_When_Neither_Tool_Reveals_The_File()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			MockFileSystem files = new();

			IProcessRunner processRunner = Substitute.For<IProcessRunner>();

			files
				.Directory
				.CreateDirectory(Path.GetDirectoryName(FilePath)!);

			files
				.File
				.WriteAllBytes(FilePath, []);

			processRunner
				.Run(Arg.Any<string>(), Arg.Any<IReadOnlyList<string>>(), Arg.Any<TimeSpan>())
				.Returns(1);

			builder.RegisterInstance<IFileSystem>(files);

			builder.RegisterInstance(processRunner);
		});

		LinuxExplorerManager sut = mock.Create<LinuxExplorerManager>();

		// Act
		bool result = sut.TryRevealFile(FilePath);

		// Assert
		result
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="LinuxExplorerManager.TryRevealFile" />: when gdbus fails or hangs, dbus-send reveals the file.
	/// </summary>
	[TestCase(1)]
	[TestCase(null)]
	public void TryRevealFile_Falls_Back_To_Dbus_Send_When_Gdbus_Fails(int? gdbusExitCode)
	{
		// Arrange
		IProcessRunner processRunner = Substitute.For<IProcessRunner>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			MockFileSystem files = new();

			files
				.Directory
				.CreateDirectory(Path.GetDirectoryName(FilePath)!);

			files
				.File
				.WriteAllBytes(FilePath, []);

			processRunner
				.Run(Gdbus, Arg.Any<IReadOnlyList<string>>(), Arg.Any<TimeSpan>())
				.Returns(gdbusExitCode);

			processRunner
				.Run(DbusSend, Arg.Any<IReadOnlyList<string>>(), Arg.Any<TimeSpan>())
				.Returns(0);

			builder.RegisterInstance<IFileSystem>(files);

			builder.RegisterInstance(processRunner);
		});

		LinuxExplorerManager sut = mock.Create<LinuxExplorerManager>();

		// Act
		bool result = sut.TryRevealFile(FilePath);

		// Assert
		result
			.Should()
			.BeTrue();

		processRunner.Received(1).Run(
			DbusSend,
			Arg.Is<IReadOnlyList<string>>(x => x.Contains($"array:string:{new Uri(FilePath).AbsoluteUri}")),
			Arg.Any<TimeSpan>());
	}

	/// <summary>
	/// <see cref="LinuxExplorerManager.TryRevealFile" />: without gdbus, dbus-send reveals the file.
	/// </summary>
	[Test]
	public void TryRevealFile_Falls_Back_To_Dbus_Send_When_Gdbus_Is_Missing()
	{
		// Arrange
		IProcessRunner processRunner = Substitute.For<IProcessRunner>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			MockFileSystem files = new();

			files
				.Directory
				.CreateDirectory(Path.GetDirectoryName(FilePath)!);

			files
				.File
				.WriteAllBytes(FilePath, []);

			processRunner
				.Run(Gdbus, Arg.Any<IReadOnlyList<string>>(), Arg.Any<TimeSpan>())
				.Throws(new Win32Exception());

			processRunner
				.Run(DbusSend, Arg.Any<IReadOnlyList<string>>(), Arg.Any<TimeSpan>())
				.Returns(0);

			builder.RegisterInstance<IFileSystem>(files);

			builder.RegisterInstance(processRunner);
		});

		LinuxExplorerManager sut = mock.Create<LinuxExplorerManager>();

		// Act
		bool result = sut.TryRevealFile(FilePath);

		// Assert
		result
			.Should()
			.BeTrue();

		processRunner.Received(1).Run(
			DbusSend,
			Arg.Is<IReadOnlyList<string>>(x => x.Contains($"array:string:{new Uri(FilePath).AbsoluteUri}")),
			Arg.Any<TimeSpan>());
	}

	/// <summary>
	/// <see cref="LinuxExplorerManager.TryRevealFile" />: gdbus reveals the file, and dbus-send is not run.
	/// </summary>
	[Test]
	public void TryRevealFile_Reveals_The_File_Through_Gdbus()
	{
		// Arrange
		IProcessRunner processRunner = Substitute.For<IProcessRunner>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			MockFileSystem files = new();

			files
				.Directory
				.CreateDirectory(Path.GetDirectoryName(FilePath)!);

			files
				.File
				.WriteAllBytes(FilePath, []);

			processRunner
				.Run(Gdbus, Arg.Any<IReadOnlyList<string>>(), Arg.Any<TimeSpan>())
				.Returns(0);

			builder.RegisterInstance<IFileSystem>(files);

			builder.RegisterInstance(processRunner);
		});

		LinuxExplorerManager sut = mock.Create<LinuxExplorerManager>();

		// Act
		bool result = sut.TryRevealFile(FilePath);

		// Assert
		result
			.Should()
			.BeTrue();

		processRunner.Received(1).Run(
			Gdbus,
			Arg.Is<IReadOnlyList<string>>(x => x.Contains($"['{new Uri(FilePath).AbsoluteUri}']")),
			Arg.Any<TimeSpan>());

		processRunner.DidNotReceive().Run(
			DbusSend,
			Arg.Any<IReadOnlyList<string>>(),
			Arg.Any<TimeSpan>());
	}

	/// <summary>
	/// <see cref="LinuxExplorerManager.TryRevealFile" />: a file that is gone runs no tool.
	/// </summary>
	[Test]
	public void TryRevealFile_Runs_No_Tool_For_A_File_That_Is_Gone()
	{
		// Arrange
		IProcessRunner processRunner = Substitute.For<IProcessRunner>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder.RegisterInstance(processRunner);

			// An empty disk: the file is not there.
			builder
				.RegisterType<MockFileSystem>()
				.As<IFileSystem>()
				.SingleInstance();
		});

		LinuxExplorerManager sut = mock.Create<LinuxExplorerManager>();

		// Act
		bool result = sut.TryRevealFile(FilePath);

		// Assert
		result
			.Should()
			.BeFalse();

		processRunner
			.DidNotReceiveWithAnyArgs()
			.Run(default!, default!, default);
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Hands bytes to a reader in native memory, the way X11 hands the data of a window property.
	/// </summary>
	private static T? ReadNative<T>(byte[] bytes, Func<IntPtr, T?> read)
	{
		IntPtr data = Marshal.AllocHGlobal(bytes.Length);

		try
		{
			Marshal.Copy(
				bytes,
				0,
				data,
				bytes.Length);

			return read(data);
		}
		finally
		{
			Marshal.FreeHGlobal(data);
		}
	}
	#endregion
}
