namespace DataOrganizer.Helpers.Text;

/// <summary>
/// Placeholder texts used to fill objects created for demonstration: source files of several languages and a paragraph.
/// </summary>
internal static class SampleText
{
	#region Properties
	/// <summary>
	/// A Batch script that prints a greeting and waits for a key, with the line breaks of Windows.
	/// </summary>
	public static string Batch { get; } = """
		@echo off
		setlocal

		REM Prints a short greeting and the time, then waits for a key.
		REM Nothing else happens: no files, no network.

		:: #region Greeting
		call :greet "%USERNAME%"
		:: #endregion

		for %%i in (one two three) do (
		    echo Step %%i
		)

		if "%TIME:~0,2%" lss "12" (
		    echo It is still morning.
		) else (
		    echo The morning is over.
		)

		pause
		endlocal
		goto :eof

		:greet
		    echo Hello, %~1!
		    echo Today is %DATE%.
		    exit /b 0
		""".ReplaceLineEndings("\r\n");

	/// <summary>
	/// A C++ source file of a queue of tasks.
	/// </summary>
	public static string CPlusPlus { get; } = """
		#include <algorithm>
		#include <iostream>
		#include <string>
		#include <vector>

		// A queue of tasks with priorities.
		// Everything stays in memory.

		/*
		 * Tasks with a higher priority come first,
		 * and tasks of the same priority keep their order.
		 */

		namespace samples
		{
		    struct Task
		    {
		        std::string name;
		        int priority;
		    };

		#pragma region Queue
		    class TaskQueue
		    {
		    public:
		        void push(const Task& task)
		        {
		            tasks_.push_back(task);
		            std::stable_sort(tasks_.begin(), tasks_.end(), [](const Task& a, const Task& b)
		            {
		                return a.priority > b.priority;
		            });
		        }

		        void print() const
		        {
		            for (const Task& task : tasks_)
		            {
		                std::cout << task.priority << ' ' << task.name << '\n';
		            }
		        }

		    private:
		        std::vector<Task> tasks_;
		    };
		#pragma endregion
		}

		int main()
		{
		#ifdef _DEBUG
		    std::cout << "Debug build\n";
		#endif
		    samples::TaskQueue queue;
		    queue.push({ "Write the report", 2 });
		    queue.push({ "Answer the mail", 1 });
		    queue.push({ "Fix the build", 3 });
		    queue.print();
		    return 0;
		}
		""".ReplaceLineEndings();

	/// <summary>
	/// A C# source file of a shelf of a shop.
	/// </summary>
	public static string CSharp { get; } = """
		using System;
		using System.Collections.Generic;
		using System.Linq;
		using System.Threading.Tasks;

		namespace Samples.Inventory;

		// Keeps the stock of a small shop in memory.
		// Nothing here touches a disk or a network.

		/// <summary>
		/// An item on a shelf, with its price in cents.
		/// </summary>
		/// <param name="Name">Name of the item.</param>
		/// <param name="Price">Price of one piece, in cents.</param>
		public sealed record Item(string Name, int Price);

		public sealed class Shelf
		{
		    #region Data
		    private readonly Dictionary<string, int> _counts = [];
		    #endregion

		    #region Methods
		    /// <summary>
		    /// Adds pieces of an item and returns the new count.
		    /// </summary>
		    public int Add(Item item, int count = 1)
		    {
		        /*
		         * The count of a new item starts at zero,
		         * so the first call puts it on the shelf.
		         */
		        _counts[item.Name] = _counts.GetValueOrDefault(item.Name) + count;

		        return _counts[item.Name];
		    }

		    /// <summary>
		    /// Returns the names of the items that are running out, the fewest first.
		    /// </summary>
		    public IEnumerable<string> FindLow(int limit)
		    {
		        foreach ((string name, int count) in _counts.OrderBy(x => x.Value))
		        {
		            if (count < limit)
		            {
		                yield return name;
		            }
		        }
		    }
		    #endregion
		}

		public static class Program
		{
		    public static async Task Main()
		    {
		#if DEBUG
		        Console.WriteLine("Debug build");
		#endif
		        Shelf shelf = new();
		        shelf.Add(new Item("Tea", 450), 3);
		        shelf.Add(new Item("Coffee", 890), 12);
		        await Task.Delay(10);
		        Console.WriteLine(string.Join(", ", shelf.FindLow(5)));
		    }
		}
		""".ReplaceLineEndings();

	/// <summary>
	/// An F# source file of shapes and their areas.
	/// </summary>
	public static string FSharp { get; } = """
		module Samples.Geometry

		open System
		open System.Collections.Generic
		open System.Text

		// Shapes and their areas.
		// Every function here is pure.

		/// <summary>
		/// A shape on a plane, measured in metres.
		/// </summary>
		type Shape =
		    | Circle of radius: float
		    | Rectangle of width: float * height: float
		    | Triangle of a: float * b: float * c: float

		(*
		    The formula of Heron gives the area of a triangle
		    from the lengths of its sides.
		*)
		let private heron a b c =
		    let s = (a + b + c) / 2.0
		    sqrt (s * (s - a) * (s - b) * (s - c))

		// #region Areas
		/// <summary>
		/// Returns the area of a shape.
		/// </summary>
		let area shape =
		    match shape with
		    | Circle radius -> Math.PI * radius * radius
		    | Rectangle (width, height) -> width * height
		    | Triangle (a, b, c) -> heron a b c
		// #endregion

		/// <summary>
		/// Describes each shape of a list with its area.
		/// </summary>
		let describe (shapes: Shape list) =
		    let builder = StringBuilder()
		    let seen = HashSet<Shape>()
		    for shape in shapes do
		        if seen.Add shape then
		            builder.AppendLine(sprintf "%A: %.2f" shape (area shape)) |> ignore
		    builder.ToString()

		#if DEBUG
		printfn "%s" (describe [ Circle 1.0; Rectangle (2.0, 3.0); Triangle (3.0, 4.0, 5.0) ])
		#endif
		""".ReplaceLineEndings();

	/// <summary>
	/// A Go source file that counts the words of a text.
	/// </summary>
	public static string Go { get; } = """
		package main

		import (
			"fmt"
			"sort"
			"strings"
		)

		// Word counts of a short text.
		// Only the standard library is used.

		/*
		Words are split at the marks between them
		and counted without regard to case.
		*/

		// region Counting
		func count(text string) map[string]int {
			counts := make(map[string]int)
			for _, word := range strings.FieldsFunc(text, isSeparator) {
				counts[strings.ToLower(word)]++
			}
			return counts
		}

		func isSeparator(r rune) bool {
			return !(r >= 'a' && r <= 'z' || r >= 'A' && r <= 'Z')
		}
		// endregion

		func main() {
			counts := count("The quick fox jumps over the lazy dog. The dog sleeps.")
			words := make([]string, 0, len(counts))
			for word := range counts {
				words = append(words, word)
			}
			sort.Strings(words)
			for _, word := range words {
				if counts[word] > 1 {
					fmt.Printf("%s: %d\n", word, counts[word])
				}
			}
		}
		""".ReplaceLineEndings();

	/// <summary>
	/// A Java source file of a weather station.
	/// </summary>
	public static string Java { get; } = """
		package samples.weather;

		import java.util.ArrayList;
		import java.util.List;
		import java.util.OptionalDouble;
		import java.util.stream.Collectors;

		// Readings of a weather station.
		// The values are made up for the sample.

		/**
		 * A reading of the temperature at an hour of the day.
		 */
		record Reading(int hour, double celsius) {
		}

		public class Main {
		    // region Station
		    private final List<Reading> readings = new ArrayList<>();

		    /**
		     * Adds a reading of the temperature.
		     *
		     * @param hour    the hour of the day
		     * @param celsius the temperature in degrees Celsius
		     */
		    public void add(int hour, double celsius) {
		        readings.add(new Reading(hour, celsius));
		    }

		    /**
		     * Returns the average temperature, when there are readings.
		     */
		    public OptionalDouble average() {
		        return readings.stream().mapToDouble(Reading::celsius).average();
		    }
		    // endregion

		    public static void main(String[] args) {
		        Main station = new Main();
		        for (int hour = 6; hour <= 18; hour += 3) {
		            station.add(hour, 10 + hour / 2.0);
		        }
		        System.out.println(station.readings.stream()
		                .map(reading -> reading.hour() + ":00 " + reading.celsius())
		                .collect(Collectors.joining(", ")));
		        station.average().ifPresent(value -> System.out.printf("Average %.1f%n", value));
		    }
		}
		""".ReplaceLineEndings();

	/// <summary>
	/// A JavaScript file that shows a greeting and waits, in the syntax that Windows Script Host reads as well as
	/// Node.js.
	/// </summary>
	public static string JavaScript { get; } = """
		/**
		 * Shows a short greeting and waits: Windows Script Host shows it in a box
		 * that waits for OK, and Node.js prints it and waits for Enter.
		 * Nothing else happens: no files, no network.
		 */

		// Messages of the sample.
		// They stay in ASCII to show in any console.

		//#region Greeting
		function greeting(hour) {
		  if (hour < 12) {
		    return "Good morning!";
		  }
		  if (hour < 18) {
		    return "Good afternoon!";
		  }
		  return "Good evening!";
		}
		//#endregion

		function show(lines) {
		  var text = lines.join("\n");
		  if (typeof WScript !== "undefined") {
		    WScript.Echo(text);
		  } else {
		    console.log(text);
		    console.log("Press Enter to close...");
		    process.stdin.resume();
		    process.stdin.once("data", function () {
		      process.exit(0);
		    });
		  }
		}

		var now = new Date();
		var lines = [greeting(now.getHours())];
		for (var step = 1; step <= 3; step++) {
		  lines.push("Step " + step);
		}
		show(lines);
		""".ReplaceLineEndings();

	/// <summary>
	/// A paragraph of placeholder text, the same from run to run.
	/// </summary>
	public static string LoremIpsum { get; } = SampleFaker
		.Create()
		.Lorem
		.Paragraph();

	/// <summary>
	/// A PowerShell script that prints a greeting and waits for Enter.
	/// </summary>
	public static string PowerShell { get; } = """
		<#
		    Prints a short greeting and the version, then waits for Enter.
		    Nothing else happens: no files, no network.
		#>

		using namespace System.Globalization
		using namespace System.Text

		# Messages of the sample.
		# They stay in ASCII to print in any console.

		#region Greeting
		function Get-Greeting {
		    param(
		        [string] $Name
		    )

		    $hour = (Get-Date).Hour
		    if ($hour -lt 12) {
		        $part = 'morning'
		    }
		    elseif ($hour -lt 18) {
		        $part = 'afternoon'
		    }
		    else {
		        $part = 'evening'
		    }

		    return "Good $part, $Name!"
		}
		#endregion

		$builder = [StringBuilder]::new()
		[void] $builder.AppendLine((Get-Greeting -Name $env:USERNAME))
		[void] $builder.AppendLine("PowerShell $($PSVersionTable.PSVersion)")
		[void] $builder.AppendLine([CultureInfo]::CurrentCulture.Name)
		Write-Host $builder.ToString()
		Read-Host 'Press Enter to close'
		""".ReplaceLineEndings();

	/// <summary>
	/// A Python script that prints a greeting and waits for Enter.
	/// </summary>
	public static string Python { get; } = """"
		#!/usr/bin/env python3
		"""
		Prints a short greeting and the version, then waits for Enter.
		Nothing else happens: no files, no network.
		"""

		import os
		import sys
		from datetime import datetime

		# Messages of the sample.
		# They stay in ASCII to print in any console.

		# region Greeting
		class Greeter:
		    """Builds the greeting of the sample."""

		    def __init__(self, name):
		        self.name = name

		    def greet(self):
		        hour = datetime.now().hour
		        if hour < 12:
		            part = "morning"
		        elif hour < 18:
		            part = "afternoon"
		        else:
		            part = "evening"
		        return f"Good {part}, {self.name}!"
		# endregion


		def main():
		    name = os.environ.get("USERNAME") or os.environ.get("USER", "friend")
		    print(Greeter(name).greet())
		    print(f"Python {sys.version_info.major}.{sys.version_info.minor}")
		    input("Press Enter to close...")


		if __name__ == "__main__":
		    main()
		"""".ReplaceLineEndings();

	/// <summary>
	/// A Rust source file of a small bank.
	/// </summary>
	public static string Rust { get; } = """
		use std::collections::HashMap;
		use std::fmt;
		use std::io::{self, Write};

		// A tiny bank with its accounts in memory.
		// Amounts are in cents to avoid rounding.

		/*
		 * A transfer fails when the account it comes from
		 * does not hold enough money.
		 */

		/// An error of a transfer.
		#[derive(Debug, PartialEq)]
		enum TransferError {
		    UnknownAccount(String),
		    NotEnoughMoney { needed: u64, available: u64 },
		}

		impl fmt::Display for TransferError {
		    fn fmt(&self, f: &mut fmt::Formatter<'_>) -> fmt::Result {
		        match self {
		            TransferError::UnknownAccount(name) => write!(f, "unknown account {name}"),
		            TransferError::NotEnoughMoney { needed, available } => {
		                write!(f, "needed {needed}, available {available}")
		            }
		        }
		    }
		}

		// region Bank
		#[derive(Default)]
		struct Bank {
		    accounts: HashMap<String, u64>,
		}

		impl Bank {
		    /// Moves money from one account to another.
		    fn transfer(&mut self, from: &str, to: &str, amount: u64) -> Result<(), TransferError> {
		        let available = *self
		            .accounts
		            .get(from)
		            .ok_or_else(|| TransferError::UnknownAccount(from.to_string()))?;
		        if available < amount {
		            return Err(TransferError::NotEnoughMoney { needed: amount, available });
		        }
		        *self.accounts.entry(from.to_string()).or_default() -= amount;
		        *self.accounts.entry(to.to_string()).or_default() += amount;
		        Ok(())
		    }
		}
		// endregion

		fn main() -> io::Result<()> {
		    let mut bank = Bank::default();
		    bank.accounts.insert("alice".into(), 5_000);
		    let result = bank.transfer("alice", "bob", 7_500);
		    writeln!(io::stdout(), "{result:?}")?;
		    Ok(())
		}
		""".ReplaceLineEndings();

	/// <summary>
	/// A shell script that prints a greeting and waits for Enter, with the line breaks of Unix.
	/// </summary>
	public static string Shell { get; } = """
		#!/usr/bin/env bash
		# Prints a short greeting and the time, then waits for Enter.
		# Nothing else happens: no files, no network.

		# Settings would be read here; the empty device keeps the sample harmless.
		source /dev/null
		. /dev/null

		# region Greeting
		greet() {
		  local name="$1"
		  local hour
		  hour=$(date +%H)
		  if [ "$hour" -lt 12 ]; then
		    echo "Good morning, $name!"
		  elif [ "$hour" -lt 18 ]; then
		    echo "Good afternoon, $name!"
		  else
		    echo "Good evening, $name!"
		  fi
		}
		# endregion

		greet "${USER:-friend}"
		for step in one two three; do
		  echo "Step $step"
		done
		read -r -p "Press Enter to close..." _
		""".ReplaceLineEndings("\n");

	/// <summary>
	/// A TypeScript module of a list of tasks.
	/// </summary>
	public static string TypeScript { get; } = """
		import { formatDate } from "./format";
		import { Storage } from "./storage";
		import type { Settings } from "./settings";

		// A list of tasks for the day.
		// Nothing leaves the memory of the page.

		/**
		 * A task that is either done or still open.
		 */
		export interface Task {
		  id: number;
		  title: string;
		  done: boolean;
		  createdAt: Date;
		}

		//#region Store
		export class TaskStore {
		  private readonly tasks: Task[] = [];
		  private nextId = 1;

		  constructor(private readonly storage: Storage, private readonly settings: Settings) {}

		  /**
		   * Adds a task and returns it.
		   */
		  add(title: string): Task {
		    const task: Task = { id: this.nextId++, title, done: false, createdAt: new Date() };
		    this.tasks.push(task);
		    this.storage.save(this.settings.key, this.tasks);
		    return task;
		  }

		  /**
		   * Returns the tasks that are still open, the oldest first.
		   */
		  open(): Task[] {
		    return this.tasks
		      .filter((task) => !task.done)
		      .sort((a, b) => a.createdAt.getTime() - b.createdAt.getTime());
		  }
		}
		//#endregion

		export function describe(store: TaskStore): string[] {
		  return store.open().map((task) => `${task.id}. ${task.title} (${formatDate(task.createdAt)})`);
		}
		""".ReplaceLineEndings();

	/// <summary>
	/// A Visual Basic source file of a small library.
	/// </summary>
	public static string VisualBasic { get; } = """
		Imports System
		Imports System.Collections.Generic
		Imports System.Linq
		Imports System.Text

		Namespace Samples.Library

		    ' Books of a small library, kept in memory.
		    ' Nothing here touches a disk or a network.

		    ''' <summary>
		    ''' A book with its author and the year it came out.
		    ''' </summary>
		    Public Class Book
		        Public Property Title As String
		        Public Property Author As String
		        Public Property Year As Integer
		    End Class

		    Public Module Catalog

		#Region "Data"
		        Private ReadOnly Books As New List(Of Book)
		#End Region

		        ''' <summary>
		        ''' Adds a book to the catalog.
		        ''' </summary>
		        Public Sub Add(title As String, author As String, year As Integer)
		            Books.Add(New Book With {.Title = title, .Author = author, .Year = year})
		        End Sub

		        ''' <summary>
		        ''' Returns the titles of the books that came out before a year, the oldest first.
		        ''' </summary>
		        Public Function FindOlder(year As Integer) As IEnumerable(Of String)
		            Return From book In Books
		                   Where book.Year < year
		                   Order By book.Year
		                   Select book.Title
		        End Function

		        Public Sub Main()
		#If DEBUG Then
		            Console.WriteLine("Debug build")
		#End If
		            Add("Dune", "Frank Herbert", 1965)
		            Add("Solaris", "Stanislaw Lem", 1961)
		            Dim builder As New StringBuilder()
		            For Each title In FindOlder(1964)
		                builder.AppendLine(title)
		            Next
		            Console.Write(builder.ToString())
		        End Sub

		    End Module

		End Namespace
		""".ReplaceLineEndings();
	#endregion
}
