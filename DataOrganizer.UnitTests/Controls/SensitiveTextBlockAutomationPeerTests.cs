using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Templates;
using Avalonia.Headless.NUnit;
using Avalonia.Styling;
using Avalonia.Threading;
using AwesomeAssertions;
using DataOrganizer.Controls;
using Shared.Common;
using System.Collections.Generic;
using System.Linq;

namespace DataOrganizer.UnitTests.Controls;

[TestFixture(Description = $@"Tests of ""{nameof(SensitiveTextBlockAutomationPeer)}"" type")]
internal class SensitiveTextBlockAutomationPeerTests
{
	#region Methods
	/// <summary>
	/// <see cref="AutomationPeer.GetAutomationControlType" />: a text block is a text.
	/// </summary>
	[AvaloniaTest]
	public void GetAutomationControlType_Is_Text()
	{
		// Arrange
		AutomationPeer sut = ControlAutomationPeer.CreatePeerForElement(new SensitiveTextBlock());

		// Act
		AutomationControlType type = sut.GetAutomationControlType();

		// Assert
		type
			.Should()
			.Be(AutomationControlType.Text);
	}

	/// <summary>
	/// <see cref="AutomationPeer.GetName" />: a sensitive text gives none of itself, from its text or its inlines, and every
	/// sensitive text gets the same name.
	/// </summary>
	[AvaloniaTest]
	public void GetName_Of_A_Sensitive_Text_Gives_None_Of_It([Values] bool isInline)
	{
		// Arrange
		string[] texts = [RandomString.Create(16), RandomString.Create(16)];

		SensitiveTextBlock[] blocks = [.. texts.Select(x => CreateBlock(x, isInline, isSensitive: true))];

		// Act
		string?[] names =
		[
			.. blocks.Select(static x => ControlAutomationPeer
				.CreatePeerForElement(x)
				.GetName())
		];

		// Assert
		names[0]
			.Should()
			.NotBeNullOrEmpty()
			.And
			.NotContain(texts[0]);

		names[1]
			.Should()
			.Be(names[0]);
	}

	/// <summary>
	/// <see cref="AutomationPeer.GetName" />: an ordinary text is the name, from its text or its inlines.
	/// </summary>
	[AvaloniaTest]
	public void GetName_Of_An_Ordinary_Text_Is_The_Text([Values] bool isInline)
	{
		// Arrange
		string text = RandomString.Create(16);

		AutomationPeer sut = ControlAutomationPeer.CreatePeerForElement(CreateBlock(text, isInline, isSensitive: false));

		// Act
		string name = sut.GetName();

		// Assert
		name
			.Should()
			.Be(text);
	}

	/// <summary>
	/// <see cref="AutomationPeer.IsControlElement" />: a text block of a control template is no control element of its own,
	/// as for any text block, and a text block outside a template is one.
	/// </summary>
	[AvaloniaTest]
	public void IsControlElement_Of_A_Template_Part_Is_False([Values] bool isTemplatePart)
	{
		// Arrange
		SensitiveTextBlock block = new();

		Window window = new()
		{
			Content = isTemplatePart
				? new ContentControl
				{
					Template = new FuncControlTemplate<ContentControl>((_, _) => block)
				}
				: block
		};

		window.Show();

		Dispatcher.UIThread.RunJobs();

		AutomationPeer sut = ControlAutomationPeer.CreatePeerForElement(block);

		// Act
		bool isControlElement = sut.IsControlElement();

		// Assert
		isControlElement
			.Should()
			.Be(!isTemplatePart);
	}

	/// <summary>
	/// <see cref="SensitiveTextBlock.IsSensitive" />: a change tells of the new name, which carries no text while the text
	/// is sensitive.
	/// </summary>
	[AvaloniaTest]
	public void IsSensitive_Tells_Of_The_New_Name([Values] bool isSensitive)
	{
		// Arrange
		string text = RandomString.Create(16);

		List<AutomationPropertyChangedEventArgs> changes = [];

		SensitiveTextBlock block = new()
		{
			IsSensitive = !isSensitive,
			Text = text
		};

		AutomationPeer sut = ControlAutomationPeer.CreatePeerForElement(block);

		sut.PropertyChanged += (_, e) => changes.Add(e);

		// Act
		block.IsSensitive = isSensitive;

		// Assert
		AutomationPropertyChangedEventArgs change = changes
			.Should()
			.ContainSingle()
			.Which;

		change.Property
			.Should()
			.Be(AutomationElementIdentifiers.NameProperty);

		change.OldValue
			.Should()
			.BeNull();

		change.NewValue
			.Should()
			.Be(sut.GetName());

		(change.NewValue as string == text)
			.Should()
			.Be(!isSensitive);
	}

	/// <summary>
	/// <see cref="SensitiveTextBlock" />: the styles of a text block reach it.
	/// </summary>
	[AvaloniaTest]
	public void StyleKeyOverride_Lets_The_Styles_Of_A_Text_Block_Reach_It()
	{
		// Arrange
		const double fontSize = 31.0;

		SensitiveTextBlock sut = new()
		{
			Classes =
			{
				"probe"
			}
		};

		Window window = new()
		{
			Content = sut,
			Styles =
			{
				new Style(static x => x
					.OfType<TextBlock>()
					.Class("probe"))
				{
					Setters =
					{
						new Setter(TextBlock.FontSizeProperty, fontSize)
					}
				}
			}
		};

		// Act
		window.Show();

		Dispatcher.UIThread.RunJobs();

		// Assert
		sut.FontSize
			.Should()
			.Be(fontSize);
	}

	/// <summary>
	/// <see cref="TextBlock.Text" />: a change of a sensitive text tells nothing, and a change of an ordinary one tells of
	/// the new name.
	/// </summary>
	[AvaloniaTest]
	public void Text_Of_A_Sensitive_Text_Tells_Nothing([Values] bool isSensitive)
	{
		// Arrange
		string text = RandomString.Create(16);

		List<AutomationPropertyChangedEventArgs> changes = [];

		SensitiveTextBlock block = new()
		{
			IsSensitive = isSensitive
		};

		AutomationPeer sut = ControlAutomationPeer.CreatePeerForElement(block);

		sut.PropertyChanged += (_, e) => changes.Add(e);

		// Act
		block.Text = text;

		// Assert
		object?[] newNames = [.. changes.Select(static x => x.NewValue)];

		object?[] expected = isSensitive
			? []
			: [text];

		newNames
			.Should()
			.Equal(expected);
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Creates a text block that holds <paramref name="text" /> as its text or as an inline.
	/// </summary>
	private static SensitiveTextBlock CreateBlock(
		string text,
		bool isInline,
		bool isSensitive)
	{
		SensitiveTextBlock block = new()
		{
			IsSensitive = isSensitive
		};

		if (isInline)
		{
			block.Inlines = [new Run(text)];
		}
		else
		{
			block.Text = text;
		}

		return block;
	}
	#endregion
}
