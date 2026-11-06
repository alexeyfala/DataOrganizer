using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using FileSystemOperations.Services;
using System;
using System.IO.Abstractions;
using System.Runtime.InteropServices;
using System.Text;
using Testably.Abstractions.Testing;

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
	/// Id of the process that owns the window.
	/// </summary>
	private const int Pid = 4242;

	/// <summary>
	/// Folder of the process in <c>/proc</c>.
	/// </summary>
	private const string ProcessFolder = "/proc/4242";
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
