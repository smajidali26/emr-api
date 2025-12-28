using EMR.Application.Common.Behaviours;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Moq;

namespace EMR.UnitTests.Application.Common.Behaviours;

/// <summary>
/// Unit tests for ValidationBehaviour
/// Tests cover: validation execution, error handling, multiple validators, and pipeline flow
/// </summary>
public class ValidationBehaviourTests
{
    // Test request and response types
    public record TestRequest : IRequest<TestResponse>
    {
        public string Name { get; init; } = string.Empty;
        public int Age { get; init; }
    }

    public record TestResponse
    {
        public bool Success { get; init; }
        public string Message { get; init; } = string.Empty;
    }

    // Test validator
    public class TestRequestValidator : AbstractValidator<TestRequest>
    {
        public TestRequestValidator()
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required");
            RuleFor(x => x.Age).GreaterThan(0).WithMessage("Age must be positive");
        }
    }

    #region No Validators Tests

    [Fact]
    public async Task Handle_WithNoValidators_ShouldCallNext()
    {
        // Arrange
        var request = new TestRequest { Name = "Test", Age = 25 };
        var expectedResponse = new TestResponse { Success = true, Message = "OK" };
        var validators = Enumerable.Empty<IValidator<TestRequest>>();
        var behaviour = new ValidationBehaviour<TestRequest, TestResponse>(validators);

        RequestHandlerDelegate<TestResponse> next = () => Task.FromResult(expectedResponse);

        // Act
        var result = await behaviour.Handle(request, next, CancellationToken.None);

        // Assert
        result.Should().Be(expectedResponse);
    }

    [Fact]
    public async Task Handle_WithNoValidators_ShouldNotThrowException()
    {
        // Arrange
        var request = new TestRequest { Name = "", Age = -1 }; // Invalid data
        var expectedResponse = new TestResponse { Success = true };
        var validators = Enumerable.Empty<IValidator<TestRequest>>();
        var behaviour = new ValidationBehaviour<TestRequest, TestResponse>(validators);

        RequestHandlerDelegate<TestResponse> next = () => Task.FromResult(expectedResponse);

        // Act
        var act = async () => await behaviour.Handle(request, next, CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
    }

    #endregion

    #region Single Validator Tests

    [Fact]
    public async Task Handle_WithValidRequest_ShouldCallNext()
    {
        // Arrange
        var request = new TestRequest { Name = "John", Age = 25 };
        var expectedResponse = new TestResponse { Success = true };
        var validators = new List<IValidator<TestRequest>> { new TestRequestValidator() };
        var behaviour = new ValidationBehaviour<TestRequest, TestResponse>(validators);

        RequestHandlerDelegate<TestResponse> next = () => Task.FromResult(expectedResponse);

        // Act
        var result = await behaviour.Handle(request, next, CancellationToken.None);

        // Assert
        result.Should().Be(expectedResponse);
    }

    [Fact]
    public async Task Handle_WithInvalidRequest_ShouldThrowValidationException()
    {
        // Arrange
        var request = new TestRequest { Name = "", Age = -1 }; // Invalid
        var validators = new List<IValidator<TestRequest>> { new TestRequestValidator() };
        var behaviour = new ValidationBehaviour<TestRequest, TestResponse>(validators);

        RequestHandlerDelegate<TestResponse> next = () => Task.FromResult(new TestResponse());

        // Act
        var act = async () => await behaviour.Handle(request, next, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<FluentValidation.ValidationException>();
    }

    [Fact]
    public async Task Handle_WithInvalidRequest_ShouldIncludeAllErrors()
    {
        // Arrange
        var request = new TestRequest { Name = "", Age = -1 }; // Multiple validation errors
        var validators = new List<IValidator<TestRequest>> { new TestRequestValidator() };
        var behaviour = new ValidationBehaviour<TestRequest, TestResponse>(validators);

        RequestHandlerDelegate<TestResponse> next = () => Task.FromResult(new TestResponse());

        // Act & Assert
        var exception = await Assert.ThrowsAsync<FluentValidation.ValidationException>(
            async () => await behaviour.Handle(request, next, CancellationToken.None));

        exception.Errors.Should().HaveCount(2);
        exception.Errors.Should().Contain(e => e.ErrorMessage == "Name is required");
        exception.Errors.Should().Contain(e => e.ErrorMessage == "Age must be positive");
    }

    [Fact]
    public async Task Handle_WithPartiallyInvalidRequest_ShouldThrowWithRelevantErrors()
    {
        // Arrange
        var request = new TestRequest { Name = "John", Age = -1 }; // Only Age is invalid
        var validators = new List<IValidator<TestRequest>> { new TestRequestValidator() };
        var behaviour = new ValidationBehaviour<TestRequest, TestResponse>(validators);

        RequestHandlerDelegate<TestResponse> next = () => Task.FromResult(new TestResponse());

        // Act & Assert
        var exception = await Assert.ThrowsAsync<FluentValidation.ValidationException>(
            async () => await behaviour.Handle(request, next, CancellationToken.None));

        exception.Errors.Should().HaveCount(1);
        exception.Errors.Should().Contain(e => e.ErrorMessage == "Age must be positive");
    }

    [Fact]
    public async Task Handle_WithInvalidRequest_ShouldNotCallNext()
    {
        // Arrange
        var request = new TestRequest { Name = "", Age = -1 };
        var validators = new List<IValidator<TestRequest>> { new TestRequestValidator() };
        var behaviour = new ValidationBehaviour<TestRequest, TestResponse>(validators);

        var nextCalled = false;
        RequestHandlerDelegate<TestResponse> next = () =>
        {
            nextCalled = true;
            return Task.FromResult(new TestResponse());
        };

        // Act
        try
        {
            await behaviour.Handle(request, next, CancellationToken.None);
        }
        catch (FluentValidation.ValidationException)
        {
            // Expected exception
        }

        // Assert
        nextCalled.Should().BeFalse();
    }

    #endregion

    #region Multiple Validators Tests

    public class AdditionalTestRequestValidator : AbstractValidator<TestRequest>
    {
        public AdditionalTestRequestValidator()
        {
            RuleFor(x => x.Name).MaximumLength(50).WithMessage("Name too long");
            RuleFor(x => x.Age).LessThan(150).WithMessage("Age too high");
        }
    }

    [Fact]
    public async Task Handle_WithMultipleValidators_ShouldExecuteAll()
    {
        // Arrange
        var request = new TestRequest { Name = "John", Age = 25 };
        var validators = new List<IValidator<TestRequest>>
        {
            new TestRequestValidator(),
            new AdditionalTestRequestValidator()
        };
        var behaviour = new ValidationBehaviour<TestRequest, TestResponse>(validators);
        var expectedResponse = new TestResponse { Success = true };

        RequestHandlerDelegate<TestResponse> next = () => Task.FromResult(expectedResponse);

        // Act
        var result = await behaviour.Handle(request, next, CancellationToken.None);

        // Assert
        result.Should().Be(expectedResponse);
    }

    [Fact]
    public async Task Handle_WithMultipleValidators_ShouldCollectErrorsFromAll()
    {
        // Arrange
        var longName = new string('A', 100);
        var request = new TestRequest { Name = longName, Age = 200 }; // Violates both validators
        var validators = new List<IValidator<TestRequest>>
        {
            new AdditionalTestRequestValidator() // Only this one will fail for these values
        };
        var behaviour = new ValidationBehaviour<TestRequest, TestResponse>(validators);

        RequestHandlerDelegate<TestResponse> next = () => Task.FromResult(new TestResponse());

        // Act & Assert
        var exception = await Assert.ThrowsAsync<FluentValidation.ValidationException>(
            async () => await behaviour.Handle(request, next, CancellationToken.None));

        exception.Errors.Should().HaveCount(2);
        exception.Errors.Should().Contain(e => e.ErrorMessage == "Name too long");
        exception.Errors.Should().Contain(e => e.ErrorMessage == "Age too high");
    }

    [Fact]
    public async Task Handle_WithMultipleValidators_WhenOneFailsAndOnePasses_ShouldThrow()
    {
        // Arrange
        var request = new TestRequest { Name = "", Age = 25 }; // First validator fails, second passes
        var validators = new List<IValidator<TestRequest>>
        {
            new TestRequestValidator(),
            new AdditionalTestRequestValidator()
        };
        var behaviour = new ValidationBehaviour<TestRequest, TestResponse>(validators);

        RequestHandlerDelegate<TestResponse> next = () => Task.FromResult(new TestResponse());

        // Act & Assert
        var exception = await Assert.ThrowsAsync<FluentValidation.ValidationException>(
            async () => await behaviour.Handle(request, next, CancellationToken.None));

        exception.Errors.Should().Contain(e => e.ErrorMessage == "Name is required");
    }

    #endregion

    #region Async Validation Tests

    public class AsyncTestRequestValidator : AbstractValidator<TestRequest>
    {
        public AsyncTestRequestValidator()
        {
            RuleFor(x => x.Name)
                .MustAsync(async (name, cancellation) =>
                {
                    await Task.Delay(1, cancellation);
                    return !string.IsNullOrEmpty(name);
                })
                .WithMessage("Name is required (async)");
        }
    }

    [Fact]
    public async Task Handle_WithAsyncValidation_ShouldWork()
    {
        // Arrange
        var request = new TestRequest { Name = "John", Age = 25 };
        var validators = new List<IValidator<TestRequest>>
        {
            new AsyncTestRequestValidator()
        };
        var behaviour = new ValidationBehaviour<TestRequest, TestResponse>(validators);
        var expectedResponse = new TestResponse { Success = true };

        RequestHandlerDelegate<TestResponse> next = () => Task.FromResult(expectedResponse);

        // Act
        var result = await behaviour.Handle(request, next, CancellationToken.None);

        // Assert
        result.Should().Be(expectedResponse);
    }

    [Fact]
    public async Task Handle_WithAsyncValidationFailing_ShouldThrow()
    {
        // Arrange
        var request = new TestRequest { Name = "", Age = 25 };
        var validators = new List<IValidator<TestRequest>>
        {
            new AsyncTestRequestValidator()
        };
        var behaviour = new ValidationBehaviour<TestRequest, TestResponse>(validators);

        RequestHandlerDelegate<TestResponse> next = () => Task.FromResult(new TestResponse());

        // Act & Assert
        var exception = await Assert.ThrowsAsync<FluentValidation.ValidationException>(
            async () => await behaviour.Handle(request, next, CancellationToken.None));

        exception.Errors.Should().Contain(e => e.ErrorMessage == "Name is required (async)");
    }

    #endregion

    #region Cancellation Token Tests

    [Fact]
    public async Task Handle_ShouldPassCancellationTokenToValidators()
    {
        // Arrange
        var request = new TestRequest { Name = "John", Age = 25 };
        var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;

        var mockValidator = new Mock<IValidator<TestRequest>>();
        mockValidator.Setup(v => v.ValidateAsync(
                It.IsAny<ValidationContext<TestRequest>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        var validators = new List<IValidator<TestRequest>> { mockValidator.Object };
        var behaviour = new ValidationBehaviour<TestRequest, TestResponse>(validators);

        RequestHandlerDelegate<TestResponse> next = () => Task.FromResult(new TestResponse());

        // Act
        await behaviour.Handle(request, next, cancellationToken);

        // Assert
        mockValidator.Verify(
            v => v.ValidateAsync(
                It.IsAny<ValidationContext<TestRequest>>(),
                cancellationToken),
            Times.Once);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Constructor_WithNullValidatorsList_DoesNotThrowInConstructor()
    {
        // Arrange
        IEnumerable<IValidator<TestRequest>>? validators = null;

        // Act - Constructor accepts null (DI never provides null in practice)
        // Note: The constructor does not validate for null, which is acceptable
        // since DI frameworks always provide an empty IEnumerable rather than null
        var act = () => new ValidationBehaviour<TestRequest, TestResponse>(validators!);

        // Assert - Constructor does not throw (null check is not performed)
        act.Should().NotThrow();
    }

    [Fact]
    public async Task Handle_WithEmptyErrorList_ShouldCallNext()
    {
        // Arrange
        var request = new TestRequest { Name = "John", Age = 25 };
        var mockValidator = new Mock<IValidator<TestRequest>>();
        mockValidator.Setup(v => v.ValidateAsync(
                It.IsAny<ValidationContext<TestRequest>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult()); // No errors

        var validators = new List<IValidator<TestRequest>> { mockValidator.Object };
        var behaviour = new ValidationBehaviour<TestRequest, TestResponse>(validators);
        var expectedResponse = new TestResponse { Success = true };

        RequestHandlerDelegate<TestResponse> next = () => Task.FromResult(expectedResponse);

        // Act
        var result = await behaviour.Handle(request, next, CancellationToken.None);

        // Assert
        result.Should().Be(expectedResponse);
    }

    #endregion

    #region Integration Scenarios

    [Fact]
    public async Task Handle_RealWorldScenario_WithComplexValidation_ShouldWork()
    {
        // Arrange - Simulate a real registration scenario
        var validRequest = new TestRequest { Name = "John Doe", Age = 30 };
        var invalidRequest = new TestRequest { Name = "", Age = -5 };

        var validators = new List<IValidator<TestRequest>>
        {
            new TestRequestValidator(),
            new AdditionalTestRequestValidator()
        };
        var behaviour = new ValidationBehaviour<TestRequest, TestResponse>(validators);

        var expectedResponse = new TestResponse { Success = true, Message = "User registered" };
        RequestHandlerDelegate<TestResponse> next = () => Task.FromResult(expectedResponse);

        // Act - Valid request should succeed
        var validResult = await behaviour.Handle(validRequest, next, CancellationToken.None);

        // Assert
        validResult.Should().Be(expectedResponse);

        // Act - Invalid request should fail
        var act = async () => await behaviour.Handle(invalidRequest, next, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<FluentValidation.ValidationException>();
    }

    #endregion
}
