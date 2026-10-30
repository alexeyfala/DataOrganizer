using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Headless.NUnit;
using AwesomeAssertions;
using DataOrganizer.Enums;
using DataOrganizer.Templates.Selectors;

namespace DataOrganizer.UnitTests.Templates.Selectors;

[TestFixture(Description = $@"Tests of ""{nameof(ContentKindTemplateSelector)}"" type")]
internal class ContentKindTemplateSelectorTests
{
	#region Methods
	/// <summary>
	/// <see cref="ContentKindTemplateSelector.Build" />: a kind without a template gets an empty control.
	/// </summary>
	[AvaloniaTest]
	public void Build_Gives_An_Empty_Control_To_A_Kind_Without_A_Template()
	{
		// Arrange
		ContentKindTemplateSelector sut = new()
		{
			Templates =
			{
				new()
				{
					Kind = RightSideSheetContentKind.CopyHistory,
					Template = new FuncDataTemplate<object>((_, _) => new TextBlock())
				}
			}
		};

		// Act
		Control? result = sut.Build(RightSideSheetContentKind.None);

		// Assert
		result
			.Should()
			.BeOfType<Control>();
	}

	/// <summary>
	/// <see cref="ContentKindTemplateSelector.Build" />: each kind is built by its own template, whatever their order.
	/// </summary>
	[AvaloniaTest]
	public void Build_Uses_The_Template_Of_The_Kind()
	{
		// Arrange
		ContentKindTemplateSelector sut = new()
		{
			Templates =
			{
				new()
				{
					Kind = RightSideSheetContentKind.ExecutingFiles,
					Template = new FuncDataTemplate<object>((_, _) => new TextBlock())
				},
				new()
				{
					Kind = RightSideSheetContentKind.CopyHistory,
					Template = new FuncDataTemplate<object>((_, _) => new Border())
				}
			}
		};

		// Act
		Control? copyHistory = sut.Build(RightSideSheetContentKind.CopyHistory);

		Control? executingFiles = sut.Build(RightSideSheetContentKind.ExecutingFiles);

		// Assert
		copyHistory
			.Should()
			.BeOfType<Border>();

		executingFiles
			.Should()
			.BeOfType<TextBlock>();
	}

	/// <summary>
	/// <see cref="ContentKindTemplateSelector.Match" />: every value of the kinds' type matches, a value of another type
	/// does not.
	/// </summary>
	[TestCase(RightSideSheetContentKind.None, ExpectedResult = true)]
	[TestCase(RightSideSheetContentKind.ExecutingFiles, ExpectedResult = true)]
	[TestCase(FavoritesPopupContentKind.CopyHistory, ExpectedResult = false)]
	[TestCase(null, ExpectedResult = false)]
	public bool Match_Accepts_The_Values_Of_The_Kind_Type(object? data)
	{
		// Arrange
		ContentKindTemplateSelector sut = new()
		{
			Templates =
			{
				new()
				{
					Kind = RightSideSheetContentKind.CopyHistory
				}
			}
		};

		// Act
		return sut.Match(data);
	}
	#endregion
}
