using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.NUnit;
using Avalonia.Threading;
using AwesomeAssertions;
using DataOrganizer.Controls;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataOrganizer.UnitTests.Controls;

[TestFixture(Description = $@"Tests of ""{nameof(SecretTextBoxAutomationPeer)}"" type")]
internal class SecretTextBoxAutomationPeerTests
{
	#region Data
	/// <summary>
	/// Text of the boxes of the tests.
	/// </summary>
	private const string Text = "Password1!";
	#endregion

	#region Methods
	/// <summary>
	/// <see cref="AutomationPeer.GetProvider{T}" />: the value is there for a call from another thread, the way the bridge
	/// of the platform asks for patterns.
	/// </summary>
	[AvaloniaTest]
	public void GetProvider_On_Another_Thread_Gives_The_Value()
	{
		// Arrange
		SecretTextBox box = new()
		{
			PasswordChar = '*',
			Text = Text
		};

		AutomationPeer sut = ControlAutomationPeer.CreatePeerForElement(box);

		// Act
		Task<IValueProvider?> request = Task.Run(sut.GetProvider<IValueProvider>);

		// The UI thread keeps running the jobs the request may post to it.
		while (!request.IsCompleted)
		{
			Dispatcher.UIThread.RunJobs();
		}

		// Assert
		request.Result
			.Should()
			.NotBeNull();
	}

	/// <summary>
	/// <see cref="SecretTextBoxAutomationPeer" />: a change of a masked text raises no change of the value, and a change of
	/// an open text does.
	/// </summary>
	[AvaloniaTest]
	public void OwnerPropertyChanged_Of_A_Masked_Box_Carries_No_Text([Values] bool isMasked)
	{
		// Arrange
		List<AutomationPropertyChangedEventArgs> changes = [];

		SecretTextBox box = new()
		{
			PasswordChar = isMasked ? '*' : default
		};

		AutomationPeer sut = ControlAutomationPeer.CreatePeerForElement(box);

		sut.PropertyChanged += (_, e) => changes.Add(e);

		// Act
		box.Text = Text;

		// Assert
		changes
			.Should()
			.HaveCount(isMasked ? 0 : 1);
	}

	/// <summary>
	/// <see cref="IValueProvider.SetValue" />: a new value is written into a masked box.
	/// </summary>
	[AvaloniaTest]
	public void SetValue_Writes_The_Text_Of_A_Masked_Box()
	{
		// Arrange
		SecretTextBox box = new()
		{
			PasswordChar = '*'
		};

		IValueProvider sut = ControlAutomationPeer
			.CreatePeerForElement(box)
			.GetProvider<IValueProvider>()!;

		// Act
		sut.SetValue(Text);

		// Assert
		box.Text
			.Should()
			.Be(Text);
	}

	/// <summary>
	/// <see cref="TemplatedControl.Template" />: a shown box takes the template of the theme of a text box.
	/// </summary>
	[AvaloniaTest]
	public void Template_Comes_From_The_Theme_Of_A_Text_Box()
	{
		// Arrange
		SecretTextBox sut = new();

		Window window = new()
		{
			Content = sut
		};

		// Act
		window.Show();

		Dispatcher.UIThread.RunJobs();

		// Assert
		sut.Template
			.Should()
			.NotBeNull();
	}

	/// <summary>
	/// <see cref="SecretTextBoxAutomationPeer.Value" />: a masked text gives an empty value, with the eye open or closed.
	/// </summary>
	[AvaloniaTest]
	public void Value_Of_A_Masked_Box_Is_Empty([Values] bool isRevealed)
	{
		// Arrange
		SecretTextBox box = new()
		{
			PasswordChar = '*',
			RevealPassword = isRevealed,
			Text = Text
		};

		IValueProvider sut = ControlAutomationPeer
			.CreatePeerForElement(box)
			.GetProvider<IValueProvider>()!;

		// Act
		string? value = sut.Value;

		// Assert
		value
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// <see cref="SecretTextBoxAutomationPeer.Value" />: an open text is the value.
	/// </summary>
	[AvaloniaTest]
	public void Value_Of_An_Open_Box_Is_The_Text()
	{
		// Arrange
		SecretTextBox box = new()
		{
			Text = Text
		};

		IValueProvider sut = ControlAutomationPeer
			.CreatePeerForElement(box)
			.GetProvider<IValueProvider>()!;

		// Act
		string? value = sut.Value;

		// Assert
		value
			.Should()
			.Be(Text);
	}
	#endregion
}
