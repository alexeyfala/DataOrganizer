using DataOrganizer.UI;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace DataOrganizer.GuardTests.TestHelpers;

/// <summary>
/// The assemblies of the application a guard looks into.
/// </summary>
internal static class AppAssemblies
{
	#region Methods
	/// <summary>
	/// Enumerates the assemblies of the application: its own assembly and the assemblies of its projects it references.
	/// </summary>
	public static Assembly[] Enumerate()
	{
		// The projects of the application share the first segment of its namespace.
		string prefix = typeof(App).Namespace!.Split('.')[0];

		Dictionary<string, Assembly> found = [];

		Queue<Assembly> pending = new([typeof(App).Assembly]);

		while (pending.TryDequeue(out Assembly? assembly))
		{
			if (!found.TryAdd(assembly.GetName().Name!, assembly))
			{
				continue;
			}

			foreach (AssemblyName reference in assembly.GetReferencedAssemblies())
			{
				if (reference.Name!.StartsWith(prefix, StringComparison.Ordinal))
				{
					pending.Enqueue(Assembly.Load(reference));
				}
			}
		}

		return [.. found.Values];
	}
	#endregion
}
