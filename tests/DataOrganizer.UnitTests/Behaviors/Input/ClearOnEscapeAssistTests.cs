using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using AwesomeAssertions;
using CommunityToolkit.Mvvm.Input;
using DataOrganizer.Behaviors.Input;

namespace DataOrganizer.UnitTests.Behaviors.Input;

[TestFixture(Description = $@"Tests of ""{nameof(ClearOnEscapeAssist)}"" type")]
internal class ClearOnEscapeAssistTests
{
	#region Methods
	/// <summary>
	/// <see cref="ClearOnEscapeAssist.IsEnabledProperty" />: the clearing takes the key before an ancestor that binds Escape.
	/// </summary>
	[AvaloniaTest]
	public void IsEnabled_Clears_Before_An_Ancestor_Binding_Of_Escape()
	{
		// Arrange
		int ancestorRuns = 0;

		TextBox textBox = new()
		{
			Text = "log"
		};

		ClearOnEscapeAssist.SetIsEnabled(textBox, true);

		Border ancestor = new()
		{
			Child = textBox
		};

		ancestor.KeyBindings.Add(new KeyBinding
		{
			Command = new RelayCommand(() => ancestorRuns++),
			Gesture = new KeyGesture(Key.Escape)
		});

		Window window = new()
		{
			Content = ancestor
		};

		window.Show();

		textBox.Focus();

		// Act
		window.KeyPressQwerty(PhysicalKey.Escape, RawInputModifiers.None);

		window.KeyReleaseQwerty(PhysicalKey.Escape, RawInputModifiers.None);

		// Assert
		textBox.Text
			.Should()
			.BeEmpty();

		ancestorRuns
			.Should()
			.Be(0);
	}

	/// <summary>
	/// <see cref="ClearOnEscapeAssist.IsEnabledProperty" />: Escape empties a box that holds text.
	/// </summary>
	[AvaloniaTest]
	public void IsEnabled_Clears_The_Text_On_Escape()
	{
		// Arrange
		TextBox textBox = new()
		{
			Text = "log"
		};

		ClearOnEscapeAssist.SetIsEnabled(textBox, true);

		Window window = new()
		{
			Content = textBox
		};

		window.Show();

		textBox.Focus();

		// Act
		window.KeyPressQwerty(PhysicalKey.Escape, RawInputModifiers.None);

		window.KeyReleaseQwerty(PhysicalKey.Escape, RawInputModifiers.None);

		// Assert
		textBox.Text
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// <see cref="ClearOnEscapeAssist.IsEnabledProperty" />: the search box style turns the clearing on.
	/// </summary>
	[AvaloniaTest]
	public void IsEnabled_Is_Turned_On_By_The_Search_Box_Style()
	{
		// Arrange
		TextBox textBox = new()
		{
			Classes =
			{
				"SearchTextBoxStyle"
			}
		};

		Window window = new()
		{
			Content = textBox
		};

		// Act
		window.Show();

		// Assert
		ClearOnEscapeAssist.GetIsEnabled(textBox)
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="ClearOnEscapeAssist.IsEnabledProperty" />: an empty box passes Escape on to an ancestor that binds it.
	/// </summary>
	[AvaloniaTest]
	public void IsEnabled_Passes_Escape_On_From_An_Empty_Box()
	{
		// Arrange
		int ancestorRuns = 0;

		TextBox textBox = new();

		ClearOnEscapeAssist.SetIsEnabled(textBox, true);

		Border ancestor = new()
		{
			Child = textBox
		};

		ancestor.KeyBindings.Add(new KeyBinding
		{
			Command = new RelayCommand(() => ancestorRuns++),
			Gesture = new KeyGesture(Key.Escape)
		});

		Window window = new()
		{
			Content = ancestor
		};

		window.Show();

		textBox.Focus();

		// Act
		window.KeyPressQwerty(PhysicalKey.Escape, RawInputModifiers.None);

		window.KeyReleaseQwerty(PhysicalKey.Escape, RawInputModifiers.None);

		// Assert
		ancestorRuns
			.Should()
			.Be(1);
	}

	/// <summary>
	/// <see cref="ClearOnEscapeAssist.IsEnabledProperty" />: once turned off, Escape leaves the text.
	/// </summary>
	[AvaloniaTest]
	public void IsEnabled_Turned_Off_Leaves_The_Text()
	{
		// Arrange
		TextBox textBox = new()
		{
			Text = "log"
		};

		ClearOnEscapeAssist.SetIsEnabled(textBox, true);

		Window window = new()
		{
			Content = textBox
		};

		window.Show();

		textBox.Focus();

		ClearOnEscapeAssist.SetIsEnabled(textBox, false);

		// Act
		window.KeyPressQwerty(PhysicalKey.Escape, RawInputModifiers.None);

		window.KeyReleaseQwerty(PhysicalKey.Escape, RawInputModifiers.None);

		// Assert
		textBox.Text
			.Should()
			.Be("log");
	}
	#endregion
}
