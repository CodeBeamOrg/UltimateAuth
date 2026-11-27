using AwesomeAssertions;
using CodeBeam.UltimateAuth.Client;
using CodeBeam.UltimateAuth.Client.Abstractions;
using CodeBeam.UltimateAuth.Client.Infrastructure;
using Moq;

namespace CodeBeam.UltimateAuth.Tests.Unit.Client;

public class UAuthRefreshCoordinatorTests
{
    private readonly Mock<IUAuthRefreshExecutor> _executor = new();
    private readonly Mock<IUAuthTokenStore> _tokenStore = new();

    private UAuthRefreshCoordinator CreateSut() => new(_executor.Object, _tokenStore.Object);

    [Fact]
    public async Task RefreshAsync_ShouldExecuteRefresh()
    {
        var expected = new RefreshResult
        {
            IsSuccess = true,
            Status = 204
        };

        _executor
            .Setup(x => x.ExecuteAsync(
                UAuthRefreshTrigger.Reactive))
            .ReturnsAsync(expected);

        var sut = CreateSut();

        var result = await sut.RefreshAsync(
            UAuthRefreshTrigger.Reactive,
            TestContext.Current.CancellationToken);

        result.Should().BeSameAs(expected);

        _executor.Verify(
            x => x.ExecuteAsync(UAuthRefreshTrigger.Reactive),
            Times.Once);
    }

    [Fact]
    public async Task ConcurrentRefreshes_ShouldShareExecution()
    {
        var completion = new TaskCompletionSource<RefreshResult>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        _executor
            .Setup(x => x.ExecuteAsync(
                It.IsAny<UAuthRefreshTrigger>()))
            .Returns(completion.Task);

        var sut = CreateSut();
        var ct = TestContext.Current.CancellationToken;

        var first = sut.RefreshAsync(
            UAuthRefreshTrigger.Reactive, ct);

        var second = sut.RefreshAsync(
            UAuthRefreshTrigger.Proactive, ct);

        var third = sut.RefreshAsync(
            UAuthRefreshTrigger.Background, ct);

        var expected = new RefreshResult
        {
            IsSuccess = true,
            Status = 204
        };

        completion.SetResult(expected);

        var results = await Task.WhenAll(
            first, second, third);

        results.Should().OnlyContain(
            x => ReferenceEquals(x, expected));

        _executor.Verify(
            x => x.ExecuteAsync(
                It.IsAny<UAuthRefreshTrigger>()),
            Times.Once);
    }

    [Fact]
    public async Task CompletedRefresh_ShouldAllowNewExecution()
    {
        _executor
            .Setup(x => x.ExecuteAsync(
                It.IsAny<UAuthRefreshTrigger>()))
            .ReturnsAsync(new RefreshResult
            {
                IsSuccess = true,
                Status = 204
            });

        var sut = CreateSut();
        var ct = TestContext.Current.CancellationToken;

        await sut.RefreshAsync(
            UAuthRefreshTrigger.Reactive, ct);

        await sut.RefreshAsync(
            UAuthRefreshTrigger.Reactive, ct);

        _executor.Verify(
            x => x.ExecuteAsync(
                It.IsAny<UAuthRefreshTrigger>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task CancelledWaiter_ShouldNotCancelSharedRefresh()
    {
        var completion = new TaskCompletionSource<RefreshResult>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        _executor
            .Setup(x => x.ExecuteAsync(
                It.IsAny<UAuthRefreshTrigger>()))
            .Returns(completion.Task);

        var sut = CreateSut();

        using var cts = new CancellationTokenSource();

        var first = sut.RefreshAsync(
            UAuthRefreshTrigger.Reactive,
            cts.Token);

        var second = sut.RefreshAsync(
            UAuthRefreshTrigger.Reactive,
            TestContext.Current.CancellationToken);

        cts.Cancel();

        Func<Task> wait = async () => await first;

        await wait.Should()
            .ThrowAsync<OperationCanceledException>();

        second.IsCompleted.Should().BeFalse();

        completion.SetResult(new RefreshResult
        {
            IsSuccess = true,
            Status = 204
        });

        var result = await second;

        result.IsSuccess.Should().BeTrue();

        _executor.Verify(
            x => x.ExecuteAsync(
                It.IsAny<UAuthRefreshTrigger>()),
            Times.Once);
    }
}
