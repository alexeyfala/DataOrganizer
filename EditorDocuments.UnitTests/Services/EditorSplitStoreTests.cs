using AwesomeAssertions;
using EditorDocuments.Services;
using System;

namespace EditorDocuments.UnitTests.Services;

[TestFixture(Description = $@"Tests of ""{nameof(EditorSplitStore)}"" type")]
internal class EditorSplitStoreTests
{
	#region Methods
	/// <summary>
	/// <see cref="EditorSplitStore.Find" />: a file that was never split has no split.
	/// </summary>
	[Test]
	public void Find_Returns_Null_For_A_File_Never_Split()
	{
		// Arrange
		EditorSplitStore sut = new();

		// Act
		double? split = sut.Find(Guid.NewGuid());

		// Assert
		split
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="EditorSplitStore.Find" />: each file gets back the split kept for it.
	/// </summary>
	[Test]
	public void Find_Returns_The_Split_Of_The_File()
	{
		// Arrange
		Guid fileId = Guid.NewGuid();

		EditorSplitStore sut = new();

		sut.Set(fileId, 0.25);

		sut.Set(Guid.NewGuid(), 0.75);

		// Act
		double? split = sut.Find(fileId);

		// Assert
		split
			.Should()
			.Be(0.25);
	}

	/// <summary>
	/// <see cref="EditorSplitStore.Set" />: a file that is no longer split loses the split kept for it.
	/// </summary>
	[Test]
	public void Set_Null_Forgets_The_Split()
	{
		// Arrange
		Guid fileId = Guid.NewGuid();

		EditorSplitStore sut = new();

		sut.Set(fileId, 0.25);

		// Act
		sut.Set(fileId, null);

		// Assert
		sut.Find(fileId)
			.Should()
			.BeNull();
	}
	#endregion
}
