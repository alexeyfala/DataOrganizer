using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using DataProtection.Helpers;
using DataProtection.Interfaces;
using DataProtection.Services;
using DataProtection.UnitTests.Factories;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Serilog;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using TestSupport.Common;

namespace DataProtection.UnitTests.Services;

[TestFixture(Description = $@"Tests of ""{nameof(DekRewrapper)}"" type")]
internal class DekRewrapperTests
{
	#region Methods
	/// <summary>
	/// <see cref="DekRewrapper.RewrapAsync" />: a cancelled write ends the rewrap quietly, with nothing in the log.
	/// </summary>
	[Test]
	public async Task RewrapAsync_Ends_Quietly_When_The_Write_Is_Cancelled()
	{
		// Arrange
		ILogger logger = Substitute.For<ILogger>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IPasswordDekWrapper encryption = Substitute.For<IPasswordDekWrapper>();

			encryption
				.RewrapIfOutdated(Arg.Any<byte[]>(), Arg.Any<PinnedBuffer>(), Arg.Any<PinnedBuffer>(), Arg.Any<ContentIdentity>())
				.Returns(RandomValues.CreateBytes(20));

			builder.RegisterInstance(encryption);

			builder.RegisterInstance(logger);
		});

		DekRewrapper sut = mock.Create<DekRewrapper>();

		using PinnedBuffer dek = SecretFactory.CreateRandomKey();

		using PinnedBuffer password = SecretFactory.CreatePassword("pw");

		// Act
		Func<Task> act = () => sut.RewrapAsync(
			RandomValues.CreateBytes(20),
			dek,
			password,
			ContentIdentity.Dek,
			(_, _) => throw new OperationCanceledException());

		// Assert
		await act
			.Should()
			.NotThrowAsync();

		logger
			.ReceivedCalls()
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// <see cref="DekRewrapper.RewrapAsync" />: a failed rewrap is logged and ends quietly, with nothing written.
	/// </summary>
	[Test]
	public async Task RewrapAsync_Logs_A_Failed_Rewrap()
	{
		// Arrange
		CryptographicException failure = new();

		ILogger logger = Substitute.For<ILogger>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IPasswordDekWrapper encryption = Substitute.For<IPasswordDekWrapper>();

			encryption
				.RewrapIfOutdated(Arg.Any<byte[]>(), Arg.Any<PinnedBuffer>(), Arg.Any<PinnedBuffer>(), Arg.Any<ContentIdentity>())!
				.Throws(failure);

			builder.RegisterInstance(encryption);

			builder.RegisterInstance(logger);
		});

		DekRewrapper sut = mock.Create<DekRewrapper>();

		using PinnedBuffer dek = SecretFactory.CreateRandomKey();

		using PinnedBuffer password = SecretFactory.CreatePassword("pw");

		List<byte[]> written = [];

		// Act
		await sut.RewrapAsync(
			RandomValues.CreateBytes(20),
			dek,
			password,
			ContentIdentity.Dek,
			(wrapper, _) =>
			{
				written.Add(wrapper);

				return Task.CompletedTask;
			});

		// Assert
		written
			.Should()
			.BeEmpty();

		logger
			.Received(1)
			.Error(failure, Arg.Any<string>(), Arg.Any<string>());
	}

	/// <summary>
	/// <see cref="DekRewrapper.RewrapAsync" />: an outdated wrapper is wrapped again for its content and handed to
	/// the write.
	/// </summary>
	[Test]
	public async Task RewrapAsync_Writes_An_Outdated_Wrapper_Again()
	{
		// Arrange
		byte[] wrapped = RandomValues.CreateBytes(20);

		byte[] rewrapped = RandomValues.CreateBytes(20);

		using PinnedBuffer dek = SecretFactory.CreateRandomKey();

		using PinnedBuffer password = SecretFactory.CreatePassword("pw");

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IPasswordDekWrapper encryption = Substitute.For<IPasswordDekWrapper>();

			encryption
				.RewrapIfOutdated(wrapped, dek, password, ContentIdentity.ClipboardDek)
				.Returns(rewrapped);

			builder.RegisterInstance(encryption);
		});

		DekRewrapper sut = mock.Create<DekRewrapper>();

		List<(byte[] Wrapper, CancellationToken Token)> written = [];

		using CancellationTokenSource source = new();

		// Act
		await sut.RewrapAsync(
			wrapped,
			dek,
			password,
			ContentIdentity.ClipboardDek,
			(wrapper, token) =>
			{
				written.Add((wrapper, token));

				return Task.CompletedTask;
			},
			source.Token);

		// Assert
		written
			.Should()
			.Equal([(rewrapped, source.Token)]);
	}

	/// <summary>
	/// <see cref="DekRewrapper.RewrapAsync" />: a wrapper of the current cost is not written.
	/// </summary>
	[Test]
	public async Task RewrapAsync_Writes_Nothing_For_A_Wrapper_Of_The_Current_Cost()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IPasswordDekWrapper encryption = Substitute.For<IPasswordDekWrapper>();

			// A null rewrap stands for a wrapper already written at the current cost.
			encryption
				.RewrapIfOutdated(Arg.Any<byte[]>(), Arg.Any<PinnedBuffer>(), Arg.Any<PinnedBuffer>(), Arg.Any<ContentIdentity>())
				.Returns((byte[]?)null);

			builder.RegisterInstance(encryption);
		});

		DekRewrapper sut = mock.Create<DekRewrapper>();

		using PinnedBuffer dek = SecretFactory.CreateRandomKey();

		using PinnedBuffer password = SecretFactory.CreatePassword("pw");

		List<byte[]> written = [];

		// Act
		await sut.RewrapAsync(
			RandomValues.CreateBytes(20),
			dek,
			password,
			ContentIdentity.Dek,
			(wrapper, _) =>
			{
				written.Add(wrapper);

				return Task.CompletedTask;
			});

		// Assert
		written
			.Should()
			.BeEmpty();
	}
	#endregion
}
