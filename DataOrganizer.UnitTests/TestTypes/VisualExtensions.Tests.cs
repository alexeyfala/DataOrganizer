using Avalonia.Controls;
using AwesomeAssertions;
using DataOrganizer.Extensions;
using System.Linq;

namespace DataOrganizer.UnitTests.TestTypes;

[TestFixture(Description = $@"Tests of ""{nameof(VisualExtensions)}"" type")]
internal class VisualExtensionsTests
{
	#region Methods
	/// <summary>
	/// Test of <see cref="VisualExtensions.FindAllVisualParents" />.
	/// </summary>
	[Test]
	public void FindAllVisualParents_Returns_Empty_For_Null_Element()
	{
		// Act
		var result = VisualExtensions.FindAllVisualParents(null);

		// Assert
		result
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// Test of <see cref="VisualExtensions.FindLogicalChild{T}(Avalonia.LogicalTree.ILogical?)" />.
	/// </summary>
	[Test]
	public void FindLogicalChild_Returns_Null_For_Null_Parent()
	{
		// Act
		Control? result = VisualExtensions.FindLogicalChild<Control>(null);

		// Assert
		result
			.Should()
			.BeNull();
	}

	/// <summary>
	/// Test of <see cref="VisualExtensions.FindLogicalChild{T}(Avalonia.LogicalTree.ILogical?, System.Predicate{T})" />.
	/// </summary>
	[Test]
	public void FindLogicalChild_With_Predicate_Returns_Null_For_Null_Parent()
	{
		// Act
		Control? result = VisualExtensions.FindLogicalChild<Control>(null, _ => true);

		// Assert
		result
			.Should()
			.BeNull();
	}

	/// <summary>
	/// Test of <see cref="VisualExtensions.FindLogicalParent{T}(Avalonia.StyledElement?)" />.
	/// </summary>
	[Test]
	public void FindLogicalParent_Returns_Null_For_Null_Element()
	{
		// Act
		Control? result = VisualExtensions.FindLogicalParent<Control>(null);

		// Assert
		result
			.Should()
			.BeNull();
	}

	/// <summary>
	/// Test of <see cref="VisualExtensions.FindLogicalParent{T}(Avalonia.StyledElement?, System.Predicate{T})" />.
	/// </summary>
	[Test]
	public void FindLogicalParent_With_Predicate_Returns_Null_For_Null_Element()
	{
		// Act
		Control? result = VisualExtensions.FindLogicalParent<Control>(null, _ => true);

		// Assert
		result
			.Should()
			.BeNull();
	}

	/// <summary>
	/// Test of <see cref="VisualExtensions.FindVisualChild{T}(Avalonia.Visual?)" />.
	/// </summary>
	[Test]
	public void FindVisualChild_Returns_Null_For_Null_Parent()
	{
		// Act
		Control? result = VisualExtensions.FindVisualChild<Control>(null);

		// Assert
		result
			.Should()
			.BeNull();
	}

	/// <summary>
	/// Test of <see cref="VisualExtensions.FindVisualChild{T}(Avalonia.Visual?, System.Predicate{T})" />.
	/// </summary>
	[Test]
	public void FindVisualChild_With_Predicate_Returns_Null_For_Null_Parent()
	{
		// Act
		Control? result = VisualExtensions.FindVisualChild<Control>(null, _ => true);

		// Assert
		result
			.Should()
			.BeNull();
	}

	/// <summary>
	/// Test of <see cref="VisualExtensions.FindVisualChildren{T}(Avalonia.Visual?)" />.
	/// </summary>
	[Test]
	public void FindVisualChildren_Returns_Empty_For_Null_Parent()
	{
		// Act
		var result = VisualExtensions.FindVisualChildren<Control>(null);

		// Assert
		result
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// Test of <see cref="VisualExtensions.FindVisualChildren{T}(Avalonia.Visual?, System.Predicate{T})" />.
	/// </summary>
	[Test]
	public void FindVisualChildren_With_Predicate_Returns_Empty_For_Null_Parent()
	{
		// Act
		var result = VisualExtensions.FindVisualChildren<Control>(null, _ => true).ToArray();

		// Assert
		result
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// Test of <see cref="VisualExtensions.FindVisualParent{T}" />.
	/// </summary>
	[Test]
	public void FindVisualParent_Returns_Null_For_Null_Element()
	{
		// Act
		Control? result = VisualExtensions.FindVisualParent<Control>(null);

		// Assert
		result
			.Should()
			.BeNull();
	}

	/// <summary>
	/// Test of <see cref="VisualExtensions.HasLogicalParent{T}" />.
	/// </summary>
	[Test]
	public void HasLogicalParent_Returns_False_For_Null_Element()
	{
		// Act
		bool result = VisualExtensions.HasLogicalParent<Control>(null, _ => true);

		// Assert
		result
			.Should()
			.BeFalse();
	}
	#endregion
}
