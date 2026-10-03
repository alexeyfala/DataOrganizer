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
	/// A C source file of a stack of numbers.
	/// </summary>
	public static string C { get; } = """
		#include <stdio.h>
		#include <stdlib.h>
		#include <string.h>

		// A stack of numbers on the heap.
		// It grows when it runs out of room.

		/*
		 * The stack doubles its room each time it is full,
		 * so a push stays fast on average.
		 */

		typedef struct
		{
		    int *items;
		    size_t count;
		    size_t capacity;
		} Stack;

		#pragma region Stack
		static void push(Stack *stack, int value)
		{
		    if (stack->count == stack->capacity)
		    {
		        stack->capacity = stack->capacity ? stack->capacity * 2 : 4;
		        stack->items = realloc(stack->items, stack->capacity * sizeof(int));
		    }
		    stack->items[stack->count++] = value;
		}

		static int pop(Stack *stack)
		{
		    return stack->items[--stack->count];
		}
		#pragma endregion

		int main(void)
		{
		    Stack stack = { 0 };
		    for (int i = 1; i <= 10; i++)
		    {
		        push(&stack, i * i);
		    }
		    while (stack.count > 0)
		    {
		        printf("%d\n", pop(&stack));
		    }
		    free(stack.items);
		    return 0;
		}
		""".ReplaceLineEndings();

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
	/// A Dart source file of a deck of cards.
	/// </summary>
	public static string Dart { get; } = """
		import 'dart:collection';
		import 'dart:math';

		// A deck of cards that deals hands.
		// The seed of the shuffle keeps the hands the same.

		/*
		 * A deck holds every card once,
		 * and a deal takes the cards from the top.
		 */

		/// A card with its rank and suit.
		class Card {
		  const Card(this.rank, this.suit);

		  final String rank;
		  final String suit;

		  @override
		  String toString() => '$rank$suit';
		}

		class Deck {
		  Deck(int seed) {
		    for (final suit in ['S', 'H', 'D', 'C']) {
		      for (final rank in ['A', 'K', 'Q', 'J', '10']) {
		        _cards.add(Card(rank, suit));
		      }
		    }
		    final shuffled = _cards.toList()..shuffle(Random(seed));
		    _cards
		      ..clear()
		      ..addAll(shuffled);
		  }

		  final Queue<Card> _cards = Queue<Card>();

		  List<Card> deal(int count) => [for (var i = 0; i < count; i++) _cards.removeFirst()];
		}

		void main() {
		  final deck = Deck(42);
		  print(deck.deal(5).join(' '));
		}
		""".ReplaceLineEndings();

	/// <summary>
	/// A Dockerfile that builds the samples in one stage and runs them in another.
	/// </summary>
	public static string Dockerfile { get; } = """
		# Builds the samples and runs them in a small image.
		# The build and the run use two stages.

		FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
		WORKDIR /src
		COPY . .
		RUN dotnet publish App.csproj \
		    --configuration Release \
		    --output /app

		FROM mcr.microsoft.com/dotnet/runtime:10.0
		WORKDIR /app
		COPY --from=build /app .
		ENV DOTNET_ENVIRONMENT=Production
		ENTRYPOINT ["dotnet", "App.dll"]
		""".ReplaceLineEndings();

	/// <summary>
	/// An EditorConfig file of the style of the samples.
	/// </summary>
	public static string EditorConfig { get; } = """
		# The style of the code of the samples.
		# The file nearest to a source file wins.

		root = true

		[*]
		charset = utf-8
		end_of_line = lf
		insert_final_newline = true
		indent_style = space
		indent_size = 4

		[*.{json,yml,yaml}]
		indent_size = 2

		[Makefile]
		indent_style = tab
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
	/// A Git ignore file of the results of a build and the files of editors.
	/// </summary>
	public static string GitIgnore { get; } = """
		# Results of the build.
		bin/
		obj/

		# Files of the editors.
		.vs/
		.idea/
		*.user
		*.suo

		# Files that the samples make.
		*.log
		*.tmp
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
	/// A Lua script that prints a greeting and waits for Enter.
	/// </summary>
	public static string Lua { get; } = """
		-- Prints a short greeting and the time, then waits for Enter.
		-- Nothing else happens: no files, no network.

		local string = require("string")
		local table = require("table")

		--[[
		The greeting depends on the hour of the day,
		as in the other scripts of the samples.
		]]

		local function greeting(hour)
		  if hour < 12 then
		    return "Good morning!"
		  elseif hour < 18 then
		    return "Good afternoon!"
		  else
		    return "Good evening!"
		  end
		end

		local lines = { greeting(tonumber(os.date("%H"))) }
		for step = 1, 3 do
		  table.insert(lines, string.format("Step %d", step))
		end
		print(table.concat(lines, "\n"))
		io.write("Press Enter to close...")
		io.read()
		""".ReplaceLineEndings();

	/// <summary>
	/// A makefile that builds, tests and cleans the samples, with its recipes indented by tabs.
	/// </summary>
	public static string Makefile { get; } = """
		# Builds, tests and cleans the samples.
		# Every target is a phony one.

		include config.mk
		-include local.mk

		CONFIGURATION ?= Debug
		OUTPUT := bin/$(CONFIGURATION)

		.PHONY: all build test clean

		all: build test

		build:
			dotnet build App.csproj --configuration $(CONFIGURATION)

		test: build
			dotnet test --no-build --configuration $(CONFIGURATION)

		clean:
			rm -rf $(OUTPUT)
		""".ReplaceLineEndings();

	/// <summary>
	/// An MSBuild project of a console application.
	/// </summary>
	public static string MsBuild { get; } = """
		<Project Sdk="Microsoft.NET.Sdk">

		  <!--
		    A console application of the samples,
		    with the settings of its build in one group.
		  -->
		  <PropertyGroup>
		    <OutputType>Exe</OutputType>
		    <TargetFramework>net10.0</TargetFramework>
		    <Nullable>enable</Nullable>
		    <ImplicitUsings>enable</ImplicitUsings>
		    <RootNamespace>Samples</RootNamespace>
		  </PropertyGroup>

		  <ItemGroup>
		    <PackageReference Include="Serilog" Version="4.3.0" />
		    <PackageReference Include="Serilog.Sinks.Console" Version="6.0.0" />
		  </ItemGroup>

		  <ItemGroup>
		    <None Update="appsettings.json" CopyToOutputDirectory="PreserveNewest" />
		  </ItemGroup>

		</Project>
		""".ReplaceLineEndings();

	/// <summary>
	/// A Perl script that prints a greeting and waits for Enter.
	/// </summary>
	public static string Perl { get; } = """
		#!/usr/bin/env perl
		# Prints a short greeting and the date, then waits for Enter.
		# Nothing else happens: no files, no network.

		use strict;
		use warnings;
		use POSIX qw(strftime);

		=pod

		The greeting depends on the hour of the day,
		as in the other scripts of the samples.

		=cut

		#region Greeting
		sub greeting {
		    my ($hour) = @_;
		    if ($hour < 12) {
		        return 'Good morning!';
		    }
		    elsif ($hour < 18) {
		        return 'Good afternoon!';
		    }
		    return 'Good evening!';
		}
		#endregion

		my $hour = (localtime)[2];
		print greeting($hour), "\n";
		print strftime('Today is %Y-%m-%d.', localtime), "\n";
		print 'Press Enter to close...';
		my $answer = <STDIN>;
		""".ReplaceLineEndings();

	/// <summary>
	/// A PHP script that prints a greeting and waits for Enter.
	/// </summary>
	public static string Php { get; } = """
		<?php
		/*
		 * Prints a short greeting, then waits for Enter.
		 * Nothing else happens: no files, no network.
		 */

		declare(strict_types=1);

		namespace Samples;

		use DateTimeImmutable;
		use InvalidArgumentException;

		// Messages of the sample.
		// They stay in ASCII to print in any console.

		#region Greeting
		#[\JetBrains\PhpStorm\Pure]
		function greeting(int $hour): string
		{
		    if ($hour < 0 || $hour > 23) {
		        throw new InvalidArgumentException("No such hour: $hour");
		    }
		    if ($hour < 12) {
		        return 'Good morning!';
		    }
		    return $hour < 18 ? 'Good afternoon!' : 'Good evening!';
		}
		#endregion

		$now = new DateTimeImmutable();
		echo greeting((int) $now->format('G')), PHP_EOL;
		echo 'Press Enter to close...';
		fgets(STDIN);
		""".ReplaceLineEndings();

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
	/// A Razor page of the orders of a day.
	/// </summary>
	public static string Razor { get; } = """
		@page
		@using System.Globalization
		@using Samples.Models
		@model IndexModel

		@{
		    ViewData["Title"] = "Orders";
		}

		<!--
		    The orders of the day,
		    each with its total.
		-->

		<h1>@ViewData["Title"]</h1>

		@if (Model.Orders.Count == 0)
		{
		    <p>No orders yet.</p>
		}
		else
		{
		    <table>
		        <thead>
		            <tr>
		                <th>Number</th>
		                <th>Total</th>
		            </tr>
		        </thead>
		        <tbody>
		            @foreach (var order in Model.Orders)
		            {
		                <tr>
		                    <td>@order.Number</td>
		                    <td>@order.Total.ToString("C", CultureInfo.CurrentCulture)</td>
		                </tr>
		            }
		        </tbody>
		    </table>
		}
		""".ReplaceLineEndings();

	/// <summary>
	/// A Ruby script that prints a greeting and waits for Enter.
	/// </summary>
	public static string Ruby { get; } = """
		#!/usr/bin/env ruby
		# Prints a short greeting and the date, then waits for Enter.
		# Nothing else happens: no files, no network.

		require 'date'
		require 'json'

		=begin
		The greeting depends on the hour of the day,
		as in the other scripts of the samples.
		=end

		# A greeting for a person.
		class Greeter
		  def initialize(name)
		    @name = name
		  end

		  def greet
		    hour = Time.now.hour
		    part =
		      if hour < 12
		        'morning'
		      elsif hour < 18
		        'afternoon'
		      else
		        'evening'
		      end
		    "Good #{part}, #{@name}!"
		  end
		end

		puts Greeter.new(ENV.fetch('USERNAME', ENV.fetch('USER', 'friend'))).greet
		puts JSON.generate({ today: Date.today.to_s })
		print 'Press Enter to close...'
		$stdin.gets
		""".ReplaceLineEndings();

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
	/// A Swift source file of a countdown.
	/// </summary>
	public static string Swift { get; } = """
		import Dispatch
		import Foundation

		// A timer that counts down in seconds.
		// The values are made up for the sample.

		/*
		 A countdown ends when it reaches zero,
		 and each tick prints the seconds that are left.
		 */

		/// A countdown of whole seconds.
		struct Countdown {
		    private(set) var seconds: Int

		    mutating func tick() -> Bool {
		        guard seconds > 0 else {
		            return false
		        }
		        seconds -= 1
		        return true
		    }
		}

		enum Level: String, CaseIterable {
		    case low, medium, high
		}

		#if DEBUG
		print("Debug build")
		#endif

		var countdown = Countdown(seconds: 3)
		while countdown.tick() {
		    print("\(countdown.seconds) left")
		}
		for level in Level.allCases {
		    print(level.rawValue)
		}
		""".ReplaceLineEndings();

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
	/// A TypeScript module of React components that show notes.
	/// </summary>
	public static string TypeScriptReact { get; } = """
		import { useState } from "react";
		import type { ReactNode } from "react";
		import { formatDate } from "./format";

		// A card that shows a note and its date.
		// The data stays in the memory of the page.

		/**
		 * The props of a card of a note.
		 */
		interface NoteCardProps {
		  title: string;
		  createdAt: Date;
		  children?: ReactNode;
		}

		//#region Components
		export function NoteCard({ title, createdAt, children }: NoteCardProps) {
		  const [open, setOpen] = useState(false);
		  return (
		    <article className="note">
		      <header onClick={() => setOpen(!open)}>
		        <h2>{title}</h2>
		        <time>{formatDate(createdAt)}</time>
		      </header>
		      {open && <section>{children}</section>}
		    </article>
		  );
		}
		//#endregion

		export default function App() {
		  return (
		    <main>
		      <NoteCard title="Groceries" createdAt={new Date()}>
		        <ul>
		          <li>Milk</li>
		          <li>Bread</li>
		        </ul>
		      </NoteCard>
		    </main>
		  );
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

	/// <summary>
	/// An Avalonia window in XAML with a list of notes and the text of the chosen one.
	/// </summary>
	public static string Xaml { get; } = """
		<Window xmlns="https://github.com/avaloniaui"
		        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
		        x:Class="Samples.MainWindow"
		        Title="Samples"
		        Width="480"
		        Height="320">

		  <!--
		    A list of notes on the left
		    and the text of the chosen note on the right.
		  -->
		  <Grid ColumnDefinitions="200,*">
		    <ListBox x:Name="Notes" Grid.Column="0">
		      <ListBoxItem>First note</ListBoxItem>
		      <ListBoxItem>Second note</ListBoxItem>
		    </ListBox>

		    <StackPanel Grid.Column="1" Margin="8" Orientation="Vertical" Spacing="4">
		      <TextBlock Text="Title" FontWeight="Bold" />
		      <TextBox AcceptsReturn="True" TextWrapping="Wrap" />
		    </StackPanel>
		  </Grid>
		</Window>
		""".ReplaceLineEndings();

	/// <summary>
	/// An XSL transformation that turns a catalog of products into a table of HTML.
	/// </summary>
	public static string Xsl { get; } = """
		<?xml version="1.0" encoding="utf-8"?>
		<!--
		  Turns the catalog of products into a page of HTML,
		  one row of a table for each product.
		-->
		<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
		  <xsl:output method="html" indent="yes" />

		  <xsl:template match="/catalog">
		    <html>
		      <body>
		        <table>
		          <xsl:for-each select="product">
		            <xsl:sort select="name" />
		            <tr>
		              <td><xsl:value-of select="name" /></td>
		              <td><xsl:value-of select="price" /></td>
		            </tr>
		          </xsl:for-each>
		        </table>
		      </body>
		    </html>
		  </xsl:template>
		</xsl:stylesheet>
		""".ReplaceLineEndings();
	#endregion
}
