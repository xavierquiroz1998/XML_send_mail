using FluentAssertions;
using XmlEmailSender.Domain.Common;

namespace XmlEmailSender.Domain.Tests;

public class ResultTests
{
    [Fact]
    public void Success_HasNoError()
    {
        var r = Result.Success();
        r.IsSuccess.Should().BeTrue();
        r.IsFailure.Should().BeFalse();
        r.Error.Should().Be(Error.None);
    }

    [Fact]
    public void Failure_CarriesError()
    {
        var err = Error.Validation("Some.Code", "msg");
        var r = Result.Failure(err);
        r.IsSuccess.Should().BeFalse();
        r.Error.Should().Be(err);
    }

    [Fact]
    public void GenericSuccess_ExposesValue()
    {
        var r = Result.Success(42);
        r.Value.Should().Be(42);
    }

    [Fact]
    public void GenericFailure_AccessingValue_Throws()
    {
        var r = Result.Failure<int>(Error.Failure("X", "y"));
        var act = () => _ = r.Value;
        act.Should().Throw<InvalidOperationException>();
    }
}
