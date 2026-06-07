using AwesomeAssertions;
using Shared.Extensions;
using System;

namespace Shared.UnitTests.TestTypes;

[TestFixture(Description = $@"Tests of ""{nameof(GenericExtensions)}"" type")]
internal class GenericExtensionsTests
{
	#region Methods
	/// <summary>
	/// <see cref="GenericExtensions.CopyPropertiesTo{T}(T, string[])" />: returns a new instance copying all properties except the ignored ones.
	/// </summary>
	[Test]
	public void CopyPropertiesTo_Returns_New_Instance_With_All_Properties_Except_Ignored_Array()
	{
		// Arrange
		Source source = new() { Name = "alpha", Number = 42, Note = "secret" };

		// Act
		Source result = source.CopyPropertiesTo(nameof(Source.Note));

		// Assert
		result
			.Should()
			.NotBeSameAs(source);

		result.Name
			.Should()
			.Be("alpha");

		result.Number
			.Should()
			.Be(42);

		result.Note
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="GenericExtensions.CopyPropertiesTo{T}(T, string)" />: returns a new instance copying all properties except the single ignored one.
	/// </summary>
	[Test]
	public void CopyPropertiesTo_Returns_New_Instance_With_All_Properties_Except_Single_Ignored()
	{
		// Arrange
		Source source = new() { Name = "alpha", Number = 42, Note = "secret" };

		// Act
		Source result = source.CopyPropertiesTo(ignored: nameof(Source.Number));

		// Assert
		result.Name
			.Should()
			.Be("alpha");

		result.Number
			.Should()
			.Be(0);

		result.Note
			.Should()
			.Be("secret");
	}

	/// <summary>
	/// <see cref="GenericExtensions.CopyPropertiesTo{T}(T, T)" />: copies all writable properties from source to target of the same type.
	/// </summary>
	[Test]
	public void CopyPropertiesTo_Same_Type_Copies_All_Writable_Properties()
	{
		// Arrange
		Source source = new() { Name = "alpha", Number = 42, Note = "note" };

		Source target = new() { Name = "old", Number = 0, Note = "old" };

		// Act
		source.CopyPropertiesTo(target);

		// Assert
		target
			.Should()
			.BeEquivalentTo(source);
	}

	/// <summary>
	/// <see cref="GenericExtensions.CopyPropertiesTo{T}(T, T)" />: does nothing and does not throw when the source is null.
	/// </summary>
	[Test]
	public void CopyPropertiesTo_Same_Type_Does_Nothing_When_Source_Is_Null()
	{
		// Arrange
		Source? source = null;

		Source target = new() { Name = "kept", Number = 1, Note = "kept" };

		// Act
		Action act = () => source!.CopyPropertiesTo(target);

		// Assert
		act
			.Should()
			.NotThrow();

		target.Name
			.Should()
			.Be("kept");
	}

	/// <summary>
	/// <see cref="GenericExtensions.CopyPropertiesTo{T}(T, T)" />: does nothing and does not throw when the target is null.
	/// </summary>
	[Test]
	public void CopyPropertiesTo_Same_Type_Does_Nothing_When_Target_Is_Null()
	{
		// Arrange
		Source source = new() { Name = "alpha", Number = 1, Note = "n" };

		Source? target = null;

		// Act
		Action act = () => source.CopyPropertiesTo(target!);

		// Assert
		act
			.Should()
			.NotThrow();
	}

	/// <summary>
	/// <see cref="GenericExtensions.CopyPropertiesTo{TSource, TTarget}(TSource, TTarget, string[])" />: copies only the named properties to the target, leaving the rest untouched.
	/// </summary>
	[Test]
	public void CopyPropertiesTo_With_Names_Copies_Only_Specified_Properties()
	{
		// Arrange
		Source source = new() { Name = "alpha", Number = 42, Note = "stay" };

		Target target = new() { Name = "old", Number = 0, Comment = "keep" };

		// Act
		source.CopyPropertiesTo(target, nameof(Source.Name), nameof(Source.Number));

		// Assert
		target.Name
			.Should()
			.Be("alpha");

		target.Number
			.Should()
			.Be(42);

		target.Comment
			.Should()
			.Be("keep");
	}

	/// <summary>
	/// <see cref="GenericExtensions.CopyPropertiesTo{TSource, TTarget}(TSource, TTarget, string[])" />: is a no-op leaving the target unchanged when no names are given.
	/// </summary>
	[Test]
	public void CopyPropertiesTo_With_Names_Is_NoOp_When_Names_Empty()
	{
		// Arrange
		Source source = new() { Name = "alpha", Number = 42, Note = "stay" };

		Target target = new() { Name = "untouched", Number = -1, Comment = "untouched" };

		// Act
		source.CopyPropertiesTo<Source, Target>(target);

		// Assert
		target.Name
			.Should()
			.Be("untouched");

		target.Number
			.Should()
			.Be(-1);

		target.Comment
			.Should()
			.Be("untouched");
	}

	/// <summary>
	/// <see cref="GenericExtensions.CopyPropertiesTo{TSource, TTarget}(TSource, TTarget, string[])" />: skips read-only target properties without throwing.
	/// </summary>
	[Test]
	public void CopyPropertiesTo_With_Names_Skips_Read_Only_Target_Properties()
	{
		// Arrange
		Source source = new() { Name = "alpha", Number = 42, Note = "n" };

		ReadOnlyTarget target = new("initial");

		// Act + Assert (should not throw, just skip the read-only property)
		Action act = () => source.CopyPropertiesTo(target, nameof(Source.Name));

		act
			.Should()
			.NotThrow();

		target.Name
			.Should()
			.Be("initial");
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Target with a read-only Name property to verify skip behaviour.
	/// </summary>
	public sealed class ReadOnlyTarget
	{
		public ReadOnlyTarget(string name) => Name = name;

		public string Name { get; }
	}

	/// <summary>
	/// Source/target with all-writable public properties of different types for cross-type copy tests.
	/// </summary>
	public sealed class Source
	{
		public string? Name { get; set; }

		public string? Note { get; set; }

		public int Number { get; set; }
	}

	/// <summary>
	/// Target with overlapping properties (Name, Number) and an own one (Comment).
	/// </summary>
	public sealed class Target
	{
		public string? Comment { get; set; }

		public string? Name { get; set; }

		public int Number { get; set; }
	}
	#endregion
}
