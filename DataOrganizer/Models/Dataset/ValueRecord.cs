using System.Diagnostics;

namespace DataOrganizer.Models.Dataset;

[DebuggerDisplay($"{nameof(Type)} = {{{nameof(Type)}}}, {nameof(Value)} = {{{nameof(Value)}}}")]
public sealed class ValueRecord : ValueRecordBase;
