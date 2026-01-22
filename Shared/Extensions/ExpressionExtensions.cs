using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Shared.Extensions;

public static class ExpressionExtensions
{
	#region Methods
	/// <summary>
	/// Returns the value from an object's property.
	/// </summary>
	/// <typeparam name="T">Property type.</typeparam>
	/// <param name="expression">Property in the form of a lambda expression.</param>
	public static object? GetValue<T>(this Expression<Func<T>> expression)
	{
		PropertyInfo? propertyInfo = expression.GetPropertyInfo();

		object? entity = expression.GetEntityReference();

		return propertyInfo?.GetValue(entity);
	}

	/// <summary>
	/// Sets a value to a property of an object.
	/// </summary>
	/// <typeparam name="TProp">Property type.</typeparam>
	/// <typeparam name="TValue">Value type.</typeparam>
	/// <param name="expression">Property in the form of a lambda expression.</param>
	/// <param name="value">Value.</param>
	public static void SetValue<TProp, TValue>(this Expression<Func<TProp>> expression, TValue? value)
	{
		PropertyInfo? propertyInfo = expression.GetPropertyInfo();

		object? entity = expression.GetEntityReference();

		propertyInfo?.SetValue(entity, value);
	}

	/// <summary>
	/// Sets the property to the given value, then resets it to the default value.
	/// </summary>
	public static void Switch<T>(this Expression<Func<T>> expression, T value)
	{
		expression.SetValue(value);

		expression.SetValue(default(T));
	}
	#endregion

	#region Service
	/// <summary>
	/// Returns a reference to the property's owner object.
	/// </summary>
	/// <typeparam name="T">Property type.</typeparam>
	/// <param name="expression">Property in the form of a lambda expression.</param>
	private static object? GetEntityReference<T>(this Expression<Func<T>> expression)
	{
		MemberExpression? pExpression;

		if (expression.Body is MemberExpression memberExpression)
		{
			pExpression = memberExpression;
		}
		else
		{
			UnaryExpression? unaryExpression = expression.Body as UnaryExpression;

			Expression? operand = unaryExpression?.Operand;

			pExpression = operand as MemberExpression;
		}

		Expression? body = pExpression?.Expression switch
		{
			MemberExpression member => member,
			ConstantExpression constantExpression => constantExpression,
			_ => null
		};

		return body is not null
			? Expression.Lambda(body).Compile().DynamicInvoke()
			: null;
	}

	/// <summary>
	/// Returns information about a property.
	/// </summary>
	/// <typeparam name="T">Property type.</typeparam>
	/// <param name="expression">Property in the form of a lambda expression.</param>
	private static PropertyInfo? GetPropertyInfo<T>(this Expression<Func<T>> expression)
	{
		if (expression.Body is MemberExpression propertyBody)
		{
			return propertyBody.Member as PropertyInfo;
		}

		UnaryExpression? body = expression.Body as UnaryExpression;

		Expression? operand = body?.Operand;

		MemberExpression? memberExpression = operand as MemberExpression;

		MemberInfo? member = memberExpression?.Member;

		return member as PropertyInfo;
	}
	#endregion
}
