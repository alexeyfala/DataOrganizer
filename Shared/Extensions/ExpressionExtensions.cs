using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Shared.Extensions;

public static class ExpressionExtensions
{
	#region Methods
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

		return Evaluate(body);
	}

	/// <summary>
	/// Computes the value of an expression without runtime compilation.
	/// </summary>
	private static object? Evaluate(Expression? expression)
	{
		return expression switch
		{
			null => null,
			ConstantExpression constant => constant.Value,
			MemberExpression member => GetMemberValue(member.Member, Evaluate(member.Expression)),
			// Fallback for unsupported expression shapes.
			_ => Expression.Lambda(expression).Compile().DynamicInvoke()
		};
	}

	/// <summary>
	/// Reads a field or property value via reflection.
	/// </summary>
	private static object? GetMemberValue(MemberInfo member, object? target)
	{
		return member switch
		{
			FieldInfo field => field.GetValue(target),
			PropertyInfo property => property.GetValue(target),
			_ => null
		};
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
