using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace DataOrganizer.GuardTests.TestHelpers;

/// <summary>
/// Reads from the compiled code of an assembly the classes it builds with <c>new</c>.
/// </summary>
internal static class ConstructorCallReader
{
	#region Data
	/// <summary>
	/// Members a type declares itself, static or not, public or not.
	/// </summary>
	private const BindingFlags DeclaredMembers = BindingFlags.DeclaredOnly
		| BindingFlags.Instance
		| BindingFlags.Static
		| BindingFlags.Public
		| BindingFlags.NonPublic;

	/// <summary>
	/// First byte of every operation code that takes two bytes.
	/// </summary>
	private const byte TwoByteOpCodePrefix = 0xFE;

	/// <summary>
	/// Every operation code of the intermediate language, by its value.
	/// </summary>
	private static readonly FrozenDictionary<short, OpCode> OpCodesByValue = typeof(OpCodes)
		.GetFields(BindingFlags.Public | BindingFlags.Static)
		.Select(static x => (OpCode)x.GetValue(null)!)
		.ToFrozenDictionary(static x => x.Value);
	#endregion

	#region Methods
	/// <summary>
	/// Enumerates the classes whose constructors the code of an assembly calls with <c>new</c>, a generic class as its
	/// definition.
	/// </summary>
	public static IEnumerable<Type> ReadBuiltTypes(Assembly assembly)
	{
		return assembly
			.GetTypes()
			.SelectMany(static x => x.GetMethods(DeclaredMembers).Concat<MethodBase>(x.GetConstructors(DeclaredMembers)))
			.SelectMany(ReadConstructorCalls)
			.Select(static x => x.DeclaringType!)
			.Select(static x => x.IsGenericType ? x.GetGenericTypeDefinition() : x)
			.Distinct();
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Enumerates the constructors a method calls with <c>new</c>.
	/// </summary>
	private static IEnumerable<ConstructorInfo> ReadConstructorCalls(MethodBase method)
	{
		byte[] code = method
			.GetMethodBody()?
			.GetILAsByteArray() ?? [];

		int index = 0;

		while (index < code.Length)
		{
			short value = code[index] == TwoByteOpCodePrefix
				? unchecked((short)((TwoByteOpCodePrefix << 8) | code[index + 1]))
				: code[index];

			OpCode opCode = OpCodesByValue[value];

			index += opCode.Size;

			if (opCode == OpCodes.Newobj)
			{
				yield return (ConstructorInfo)method.Module.ResolveMethod(
					BitConverter.ToInt32(code, index),
					method.DeclaringType!.IsGenericType ? method.DeclaringType.GetGenericArguments() : null,
					method.IsGenericMethod ? method.GetGenericArguments() : null)!;
			}

			index += ReadOperandSize(opCode, code, index);
		}
	}

	/// <summary>
	/// Reads the size in bytes of the operand that follows an operation code.
	/// </summary>
	private static int ReadOperandSize(OpCode opCode, byte[] code, int index)
	{
		return opCode.OperandType switch
		{
			OperandType.InlineNone => 0,
			OperandType.ShortInlineBrTarget or OperandType.ShortInlineI or OperandType.ShortInlineVar => 1,
			OperandType.InlineVar => 2,
			OperandType.InlineI8 or OperandType.InlineR => 8,
			OperandType.InlineSwitch => 4 + (4 * BitConverter.ToInt32(code, index)),
			_ => 4
		};
	}
	#endregion
}
