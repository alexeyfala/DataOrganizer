using AwesomeAssertions;
using FileSystemOperations.Services;
using System;
using System.Runtime.InteropServices;
using System.Text;

namespace FileSystemOperations.UnitTests.Services;

[TestFixture(Description = $@"Tests of ""{nameof(LinuxExplorerManager)}"" type")]
internal class LinuxExplorerManagerTests
{
	#region Methods
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
	/// <see cref="LinuxExplorerManager.TitleMatchesFolderName" />: matches an exact title or a "&lt;name&gt; &lt;suffix&gt;" title, and rejects anything else.
	/// </summary>
	[TestCase("Docs", "Docs", ExpectedResult = true)]
	[TestCase("Docs - Files", "Docs", ExpectedResult = true)]
	[TestCase("Docs ", "Docs", ExpectedResult = true)]
	[TestCase("Documents", "Doc", ExpectedResult = false)]
	[TestCase("Other", "Docs", ExpectedResult = false)]
	[TestCase("", "Docs", ExpectedResult = false)]
	[TestCase("Docs", "", ExpectedResult = false)]
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
