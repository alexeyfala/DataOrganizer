using Cysharp.Text;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

namespace Shared.Extensions;

public static class GenericExtensions
{
	#region Methods
	/// <summary>
	/// Converts an object into an array along with other objects.
	/// </summary>
	public static T[] AsArray<T>(this T entity, params T[] others) => [.. entity.ToEnumerable(others)];

	/// <summary>
	/// Copies the values ​​of writable passed properties in objects of different types via reflection.
	/// </summary>
	public static void CopyPropertiesTo<TSource, TTarget>(
		this TSource source,
		TTarget target,
		params string[] propertyNames) where TSource : class where TTarget : class
	{
		if (propertyNames.Length == 0)
		{
			return;
		}

		Type targetType = target.GetType();

		foreach (PropertyInfo sourceProperty in source
			.GetType()
			.GetProperties(BindingFlags.Public | BindingFlags.Instance)
			.Where(x => propertyNames.Contains(x.Name)))
		{
			if (!GetWritableProperty(
				targetType,
				sourceProperty.Name,
				out PropertyInfo? targetProperty))
			{
				continue;
			}

			targetProperty.SetValue(target, sourceProperty.GetValue(source));
		}
	}

	/// <summary>
	/// Copies the values ​​of writable properties of two objects of the same type via reflection.
	/// </summary>
	public static void CopyPropertiesTo<T>(this T source, T target)
	{
		if (source is null || target is null)
		{
			return;
		}

		Type targetType = target.GetType();

		foreach (PropertyInfo sourceProperty in source
			.GetType()
			.GetProperties(BindingFlags.Public | BindingFlags.Instance))
		{
			if (!GetWritableProperty(
				targetType,
				sourceProperty.Name,
				out PropertyInfo? targetProperty))
			{
				continue;
			}

			targetProperty.SetValue(target, sourceProperty.GetValue(source));
		}
	}

	/// <summary>
	/// Creates object and copies the values ​​of writable properties to it from source via reflection.
	/// </summary>
	public static T CopyPropertiesTo<T>(
		this T source,
		params string[] ignored) where T : class, new()
	{
		return CopyPropertiesTo(source, x => !ignored.Contains(x));
	}

	/// <summary>
	/// Creates object and copies the values ​​of writable properties to it from source via reflection.
	/// </summary>
	public static T CopyPropertiesTo<T>(
		this T source,
		string ignored) where T : class, new()
	{
		return CopyPropertiesTo(source, x => !string.Equals(x, ignored));
	}

	/// <summary>
	/// Returns the values ​​of the object's public properties as a string.
	/// </summary>
	/// <remarks>
	/// If property names are not passed, all public properties of the object are used.
	/// </remarks>
	public static string GetPropertyValues<T>(this T target,
		bool insertNewLine,
		params string[] propertyNames)
	{
		Utf16ValueStringBuilder builder = ZString.CreateStringBuilder();

		try
		{
			if (target?.GetType() is not { } type)
			{
				return string.Empty;
			}

			const BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Public;

			PropertyInfo[] properties = propertyNames.Length > 0
				? [.. type
					.GetProperties(bindingFlags)
					.Where(x => propertyNames.Contains(x.Name))
					.OrderBy(x => Array.IndexOf(propertyNames, x.Name))]
				: type.GetProperties(bindingFlags);

			if (insertNewLine)
			{
				builder.AppendLine();
			}

			properties.ForEachFor((property, i) =>
			{
				try
				{
					object? value;

					try
					{
						value = property.GetValue(target);
					}
					catch (Exception ex)
					{
						value = ex.Message;

						if (ex.InnerException is { } inner)
						{
							value +=
								$" {nameof(Exception.InnerException)} ({inner.GetType()}): {inner.Message}";
						}
					}

					if (value is null)
					{
						value = "null";
					}
					else if (value is string)
					{
						value = $@"""{value}""";
					}
					else if (value.GetType().IsEnum && value is Enum enumValue)
					{
						object numberValue = Convert.ChangeType(value, enumValue.GetTypeCode());

						value = Convert.ChangeType(value, TypeCode.String);

						value = $"{value} {{enum number value: {numberValue}}}";
					}

					AppendPropertyData(
						ref builder,
						property.Name,
						value,
						i == properties.Length - 1);
				}
				catch (Exception ex)
				{
					Trace.WriteLine(ex);
				}
			});

			return builder.ToString();
		}
		finally
		{
			builder.Dispose();
		}
	}

	/// <summary>
	/// Returns a .Net representation of the type.
	/// </summary>
	public static string? GetTypeKind(this Type type)
	{
		if (type.IsClass)
		{
			return "class";
		}

		if (type.IsValueType)
		{
			return "struct";
		}

		if (type.IsInterface)
		{
			return "interface";
		}

		return type.IsEnum
			? "enum"
			: null;
	}

	/// <summary>
	/// Returns <c>True</c> if the value is the default value for its type.
	/// </summary>
	/// <remarks>
	/// null for classes, null (empty) for Nullable structs, zero, false, etc. for other structs
	///</remarks>
	public static bool IsDefault<T>([NotNullWhen(false)] this T argument) => EqualityComparer<T>.Default.Equals(argument, default);

	/// <summary>
	/// Returns <c>True</c> if the value is not the default value for its type.
	/// </summary>
	/// <remarks>
	/// null for classes, null (empty) for Nullable structs, zero, false, etc. for other structs
	///</remarks>
	public static bool IsNotDefault<T>([NotNullWhen(true)] this T argument) => !argument.IsDefault();

	/// <summary>
	/// Sets the value of a property via reflection.
	/// </summary>
	public static void SetPropertyValue<TTarget, TValue>(
		this TTarget target,
		string propertyName,
		TValue value) where TTarget : notnull
	{
		if (target.GetType().GetProperty(propertyName) is not { } property)
		{
			return;
		}

		property.SetValue(target, value);
	}

	/// <summary>
	/// Converts an object into a sequence along with other objects.
	/// </summary>
	public static IEnumerable<T> ToEnumerable<T>(this T entity, params T[] others)
	{
		yield return entity;

		foreach (T item in others)
		{
			yield return item;
		}
	}

	/// <summary>
	/// Converts an object into a sequence along with other objects.
	/// </summary>
	public static IEnumerable<T> ToEnumerable<T>(this T entity, IEnumerable<T> others)
	{
		yield return entity;

		foreach (T item in others)
		{
			yield return item;
		}
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Adds property data.
	/// </summary>
	private static void AppendPropertyData<T>(
		ref Utf16ValueStringBuilder builder,
		string propertyName,
		T value,
		bool isLastProperty)
	{
		builder.Append(propertyName);

		builder.Append(" = ");

		builder.Append(value);

		if (isLastProperty)
		{
			return;
		}

		builder.AppendLine(",");
	}

	/// <summary>
	/// Creates object and copies the values ​​of writable properties to it from source via reflection.
	/// </summary>
	private static T CopyPropertiesTo<T>(T source, Predicate<string> condition) where T : class, new()
	{
		T target = new();

		Type targetType = target.GetType();

		foreach (PropertyInfo sourceProperty in source
			.GetType()
			.GetProperties(BindingFlags.Public | BindingFlags.Instance)
			.Where(x => condition(x.Name)))
		{
			if (!GetWritableProperty(
				targetType,
				sourceProperty.Name,
				out PropertyInfo? targetProperty))
			{
				continue;
			}

			targetProperty.SetValue(target, sourceProperty.GetValue(source));
		}

		return target;
	}

	/// <summary>
	/// Returns a writable property <see cref="PropertyInfo" /> with the specified name.
	/// </summary>
	private static bool GetWritableProperty(
		Type targetType,
		string propertyName,
		[NotNullWhen(true)] out PropertyInfo? targetProperty)
	{
		targetProperty = default;

		if (targetType.GetProperty(propertyName) is not { CanWrite: true } property)
		{
			return false;
		}

		targetProperty = property;

		return true;

	}
	#endregion
}
