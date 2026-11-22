using NUnit.Framework;
using System;

namespace DataOrganizer.TestSupport.Guards;

/// <summary>
/// Puts a test into the <c>Guard</c> category: it reads the files of the repository instead of
/// exercising the application, so a run can leave it out with <c>--filter TestCategory!=Guard</c>.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class GuardAttribute : CategoryAttribute;
