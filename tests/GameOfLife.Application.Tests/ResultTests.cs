using GameOfLife.Application.Common;

namespace GameOfLife.Application.Tests;

[TestFixture]
public sealed class ResultTests
{
    private static readonly Error SampleError = new("sample.code", "Something failed.", ErrorType.Validation);

    [Test]
    public void Success_ExposesValue()
    {
        Result<int> result = 42;

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value, Is.EqualTo(42));
            Assert.That(result.Error, Is.Null);
        });
    }

    [Test]
    public void Failure_ExposesErrorAndHidesValue()
    {
        Result<int> result = SampleError;

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Error, Is.EqualTo(SampleError));
            Assert.That(() => result.Value, Throws.InvalidOperationException);
        });
    }

    [Test]
    public void Match_CallsTheMatchingBranch()
    {
        Result<int> success = 1;
        Result<int> failure = SampleError;

        Assert.Multiple(() =>
        {
            Assert.That(success.Match(v => $"value {v}", e => e.Code), Is.EqualTo("value 1"));
            Assert.That(failure.Match(v => $"value {v}", e => e.Code), Is.EqualTo("sample.code"));
        });
    }
}
