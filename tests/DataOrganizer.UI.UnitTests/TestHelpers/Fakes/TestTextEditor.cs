using DataOrganizer.UI.Controls.Text;
using System;

namespace DataOrganizer.UI.UnitTests.TestHelpers.Fakes;

/// <summary>
/// Minimal concrete <see cref="TextEditorBase" />.
/// </summary>
internal sealed class TestTextEditor : TextEditorBase
{
	#region Constructors
	/// <summary>
	/// Sets up the editor with its pauses on the system clock.
	/// </summary>
	public TestTextEditor()
	{
	}

	/// <summary>
	/// Sets up the editor with its pauses on <paramref name="timeProvider" />.
	/// </summary>
	public TestTextEditor(TimeProvider timeProvider) : base(timeProvider)
	{
	}
	#endregion
}
