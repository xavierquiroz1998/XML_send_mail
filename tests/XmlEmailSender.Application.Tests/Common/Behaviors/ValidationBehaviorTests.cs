using FluentAssertions;
using FluentValidation;
using MediatR;
using XmlEmailSender.Application.Common.Behaviors;
using XmlEmailSender.Application.Common.Messaging;
using XmlEmailSender.Domain.Common;

namespace XmlEmailSender.Application.Tests.Common.Behaviors;

public class ValidationBehaviorTests
{
    public sealed record FakeCommand(string Name) : ICommand<string>;

    public sealed class FakeValidator : AbstractValidator<FakeCommand>
    {
        public FakeValidator() => RuleFor(c => c.Name).NotEmpty().WithMessage("required");
    }

    [Fact]
    public async Task Handle_NoValidators_PassesThrough()
    {
        var sut = new ValidationBehavior<FakeCommand, Result<string>>(Array.Empty<IValidator<FakeCommand>>());

        Task<Result<string>> Next() => Task.FromResult(Result.Success("ok"));

        var result = await sut.Handle(new FakeCommand("x"), Next, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("ok");
    }

    [Fact]
    public async Task Handle_ValidatorFails_ReturnsTypedFailure_WithoutCallingNext()
    {
        var sut = new ValidationBehavior<FakeCommand, Result<string>>(new[] { new FakeValidator() });

        var nextCalled = false;
        Task<Result<string>> Next()
        {
            nextCalled = true;
            return Task.FromResult(Result.Success("ok"));
        }

        var result = await sut.Handle(new FakeCommand(""), Next, CancellationToken.None);

        nextCalled.Should().BeFalse("el handler no debe invocarse cuando la validación falla");
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
        result.Error.Message.Should().Be("required");
    }

    [Fact]
    public async Task Handle_ValidatorPasses_CallsNext()
    {
        var sut = new ValidationBehavior<FakeCommand, Result<string>>(new[] { new FakeValidator() });
        Task<Result<string>> Next() => Task.FromResult(Result.Success("from-handler"));

        var result = await sut.Handle(new FakeCommand("ok"), Next, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("from-handler");
    }
}
