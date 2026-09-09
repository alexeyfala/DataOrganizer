using AwesomeAssertions;
using Shared.Extensions;
using System;
using System.Linq.Expressions;

namespace Shared.UnitTests.TestTypes;

[TestFixture(Description = $@"Tests of ""{nameof(ExpressionExtensions)}"" type")]
internal class ExpressionExtensionsTests
{
	#region Methods
	/// <summary>
	/// <see cref="ExpressionExtensions.SetValue{TProp, TValue}" />: sets a nested property resolved through the member chain.
	/// </summary>
	[Test]
	public void SetValue_Sets_Nested_Property()
	{
		// Arrange
		Holder holder = new();

		Expression<Func<string?>> expression = () => holder.Child.Name;

		// Act
		expression.SetValue("nested");

		// Assert
		holder.Child.Name
			.Should()
			.Be("nested");
	}

	/// <summary>
	/// <see cref="ExpressionExtensions.SetValue{TProp, TValue}" />: sets a property reached through a boxing unary conversion.
	/// </summary>
	[Test]
	public void SetValue_Sets_Property_Through_Unary_Conversion()
	{
		// Arrange
		Holder holder = new();

		Expression<Func<object>> expression = () => holder.Number;

		// Act
		expression.SetValue(7);

		// Assert
		holder.Number
			.Should()
			.Be(7);
	}

	/// <summary>
	/// <see cref="ExpressionExtensions.SetValue{TProp, TValue}" />: sets a reference-type property on the referenced instance.
	/// </summary>
	[Test]
	public void SetValue_Sets_Reference_Type_Property()
	{
		// Arrange
		Holder holder = new() { Name = "old" };

		Expression<Func<string?>> expression = () => holder.Name;

		// Act
		expression.SetValue("new");

		// Assert
		holder.Name
			.Should()
			.Be("new");
	}

	/// <summary>
	/// <see cref="ExpressionExtensions.SetValue{TProp, TValue}" />: sets a value-type property on the referenced instance.
	/// </summary>
	[Test]
	public void SetValue_Sets_Value_Type_Property()
	{
		// Arrange
		Holder holder = new();

		Expression<Func<int>> expression = () => holder.Number;

		// Act
		expression.SetValue(42);

		// Assert
		holder.Number
			.Should()
			.Be(42);
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Owner with reference-, value- and nested-typed properties for the set-value tests.
	/// </summary>
	private sealed class Holder
	{
		public Inner Child { get; } = new();

		public string? Name { get; set; }

		public int Number { get; set; }
	}

	/// <summary>
	/// Nested owner used to exercise the member-chain evaluation path.
	/// </summary>
	private sealed class Inner
	{
		public string? Name { get; set; }
	}
	#endregion
}
