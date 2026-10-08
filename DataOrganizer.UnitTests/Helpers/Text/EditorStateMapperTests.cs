using AwesomeAssertions;
using DataOrganizer.Dto.Documents;
using DataOrganizer.Helpers.Text;
using DataOrganizer.Interfaces.Documents;
using NSubstitute;

namespace DataOrganizer.UnitTests.Helpers.Text;

[TestFixture(Description = $@"Tests of ""{nameof(EditorStateMapper)}"" type")]
internal class EditorStateMapperTests
{
	#region Methods
	/// <summary>
	/// <see cref="EditorStateMapper.Apply" />: the values of the editor take those of the state, its view state included.
	/// </summary>
	[Test]
	public void Apply_Gives_The_Values_Those_Of_The_State()
	{
		// Arrange
		IEditorStateValues values = Substitute.For<IEditorStateValues>();

		FileEditorState state = new()
		{
			Bookmarks = [3, 7],
			CaretPosition = new(line: 3, column: 2),
			FoldedBlocks = [12],
			FontSize = 18.5,
			ScrollOffset = new(15, 480),
			SelectionLength = 4,
			SelectionStart = 20,
			ShowEndOfLine = true,
			ShowSpaces = true,
			ShowTabs = true,
			UnfoldedBlocks = [40],
			WordWrap = true
		};

		// Act
		EditorStateMapper.Apply(values, state);

		// Assert
		DocumentViewState expected = new()
		{
			Bookmarks = [3, 7],
			CaretPosition = new(line: 3, column: 2),
			FoldedBlocks = [12],
			ScrollOffset = new(15.0, 480.0),
			SelectionLength = 4,
			SelectionStart = 20,
			UnfoldedBlocks = [40]
		};

		values.ViewState
			.Should()
			.Be(expected);

		values.FontSize
			.Should()
			.Be(18.5);

		values.ShowEndOfLine
			.Should()
			.BeTrue();

		values.ShowSpaces
			.Should()
			.BeTrue();

		values.ShowTabs
			.Should()
			.BeTrue();

		values.WordWrap
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="EditorStateMapper.Apply" />: a stored language is restored, none gives the default one, and a language
	/// without a grammar leaves the text plain.
	/// </summary>
	[TestCase(null, "powershell")]
	[TestCase("bat", "bat")]
	[TestCase(FileEditorState.PlainTextLanguage, null)]
	[TestCase("unknown", null)]
	public void Apply_Restores_The_Chosen_Syntax_Language(string? stored, string? expected)
	{
		// Arrange
		IEditorStateValues values = Substitute.For<IEditorStateValues>();

		values
			.DefaultSyntaxLanguage
			.Returns("powershell");

		FileEditorState state = new()
		{
			SyntaxLanguage = stored
		};

		// Act
		EditorStateMapper.Apply(values, state);

		// Assert
		values.SyntaxLanguage
			.Should()
			.Be(expected);
	}

	/// <summary>
	/// <see cref="EditorStateMapper.Create" />: the state keeps the values of the editor, its view state and the encoding,
	/// with the scroll offset in whole pixels.
	/// </summary>
	[Test]
	public void Create_Keeps_The_Values_Of_The_Editor()
	{
		// Arrange
		IEditorStateValues values = Substitute.For<IEditorStateValues>();

		values
			.DefaultSyntaxLanguage
			.Returns("powershell");

		values.FontSize = 18.5;

		values.ShowEndOfLine = true;

		values.ShowSpaces = true;

		values.ShowTabs = true;

		values.SyntaxLanguage = "bat";

		values.ViewState = new DocumentViewState
		{
			Bookmarks = [3, 7],
			CaretPosition = new(line: 3, column: 2),
			FoldedBlocks = [12],
			ScrollOffset = new(15.6, 480.2),
			SelectionLength = 4,
			SelectionStart = 20,
			UnfoldedBlocks = [40]
		};

		values.WordWrap = true;

		// Act
		FileEditorState state = EditorStateMapper.Create(values, "cp866", isEncrypted: false);

		// Assert
		FileEditorState expected = new()
		{
			Bookmarks = [3, 7],
			CaretPosition = new(line: 3, column: 2),
			Encoding = "cp866",
			FoldedBlocks = [12],
			FontSize = 18.5,
			ScrollOffset = new(15, 480),
			SelectionLength = 4,
			SelectionStart = 20,
			ShowEndOfLine = true,
			ShowSpaces = true,
			ShowTabs = true,
			SyntaxLanguage = "bat",
			UnfoldedBlocks = [40],
			WordWrap = true
		};

		// A struct is compared by its members, or its arrays would be compared by reference.
		state
			.Should()
			.BeEquivalentTo(
				expected,
				options => options.ComparingByMembers<FileEditorState>().WithStrictOrdering());
	}

	/// <summary>
	/// <see cref="EditorStateMapper.Create" />: an encrypted text keeps no folded or unfolded blocks, which would give away
	/// its outline, while its bookmarks stay.
	/// </summary>
	[Test]
	public void Create_Leaves_The_Blocks_Of_An_Encrypted_Text_Out()
	{
		// Arrange
		IEditorStateValues values = Substitute.For<IEditorStateValues>();

		values.ViewState = new DocumentViewState
		{
			Bookmarks = [3, 7],
			CaretPosition = new(line: 3, column: 2),
			FoldedBlocks = [12],
			ScrollOffset = new(15.0, 480.0),
			SelectionLength = 4,
			SelectionStart = 20,
			UnfoldedBlocks = [40]
		};

		// Act
		FileEditorState state = EditorStateMapper.Create(values, encoding: null, isEncrypted: true);

		// Assert
		state.FoldedBlocks
			.Should()
			.BeNull();

		state.UnfoldedBlocks
			.Should()
			.BeNull();

		state.Bookmarks
			.Should()
			.Equal(3, 7);
	}

	/// <summary>
	/// <see cref="EditorStateMapper.Create" />: the default language is stored as no choice, another one by its id, and
	/// plain text under an id of its own.
	/// </summary>
	[TestCase("powershell", "powershell", null)]
	[TestCase("powershell", "bat", "bat")]
	[TestCase("powershell", null, FileEditorState.PlainTextLanguage)]
	[TestCase(null, null, null)]
	[TestCase(null, "bat", "bat")]
	public void Create_Stores_The_Chosen_Syntax_Language(string? defaultLanguage, string? language, string? expected)
	{
		// Arrange
		IEditorStateValues values = Substitute.For<IEditorStateValues>();

		values
			.DefaultSyntaxLanguage
			.Returns(defaultLanguage);

		values.SyntaxLanguage = language;

		// Act
		FileEditorState state = EditorStateMapper.Create(values, encoding: null, isEncrypted: false);

		// Assert
		state.SyntaxLanguage
			.Should()
			.Be(expected);
	}
	#endregion
}
