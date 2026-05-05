using CommonTestHelpers.Helpers;
using DataOrganizer.DTO;
using DataOrganizer.Services;

namespace DataOrganizer.UnitTests.TestTypes;

[TestFixture(Description = $@"Tests of ""{nameof(FileChangeTracker)}"" type")]
internal class FileChangeTrackerTests
{
	#region Methods

	#endregion

	#region Service
	/// <summary>
	/// Builds <see cref="TrackChangesParameters" /> for tests with a fresh semaphore and the supplied contents.
	/// </summary>
	private static TrackChangesParameters CreateParameters(string filePath, byte[] contents) => new()
	{
		Contents = contents,
		File = TestUtils.CreateFileDto(),
		FilePath = filePath,
		SessionEncryptedDek = null
	};
	#endregion
}
