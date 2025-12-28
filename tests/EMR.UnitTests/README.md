# EMR Backend API - Unit Tests

## Quick Start

```bash
# Navigate to test directory
cd tests/EMR.UnitTests

# Restore dependencies
dotnet restore

# Build tests
dotnet build

# Run all tests
dotnet test

# Run tests with detailed output
dotnet test --logger "console;verbosity=detailed"
```

## Test Organization

### Domain Tests
Tests for domain entities and value objects (business logic, validation, invariants)

- **Value Objects:** PatientIdentifier, PatientId, PatientAddress, EmergencyContact
- **Entities:** Patient, User, BaseEntity
- **Focus:** Business rules, validation, immutability, equality

### Application Tests
Tests for application layer (commands, queries, validators, behaviors)

- **Validators:** RegisterUserCommandValidator
- **Handlers:** RegisterUserCommandHandler
- **Behaviors:** ValidationBehaviour
- **Focus:** Command execution, validation rules, error handling

### Infrastructure Tests
Tests for data access and external integrations

- **Repositories:** UserRepository
- **Focus:** Data access, query correctness, database operations

## Running Specific Tests

```bash
# Run tests from a specific namespace
dotnet test --filter "FullyQualifiedName~Domain.ValueObjects"

# Run a specific test class
dotnet test --filter "FullyQualifiedName~PatientIdentifierTests"

# Run a specific test method
dotnet test --filter "FullyQualifiedName~PatientIdentifierTests.Generate_ShouldCreateValidMRN"

# Run tests by category (if using [Trait])
dotnet test --filter "Category=Security"
```

## Code Coverage

```bash
# Install coverage tool (one-time)
dotnet tool install --global dotnet-coverage

# Run tests with coverage
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover

# Generate coverage report
dotnet coverage collect "dotnet test" -f xml -o coverage.xml
```

## Test Patterns

### Arrange-Act-Assert
```csharp
[Fact]
public void Method_Scenario_ExpectedBehavior()
{
    // Arrange - Set up test data and dependencies
    var input = CreateTestData();

    // Act - Execute the method under test
    var result = MethodUnderTest(input);

    // Assert - Verify the expected outcome
    result.Should().Be(expectedValue);
}
```

### Theory Tests (Data-Driven)
```csharp
[Theory]
[InlineData("")]
[InlineData("   ")]
[InlineData(null)]
public void Method_WithInvalidInput_ShouldThrowException(string? invalidInput)
{
    // Arrange & Act
    Action act = () => MethodUnderTest(invalidInput);

    // Assert
    act.Should().Throw<ArgumentException>();
}
```

### Mock Verification
```csharp
[Fact]
public async Task Handler_ShouldCallRepository()
{
    // Arrange
    var mockRepo = new Mock<IRepository>();
    var handler = new Handler(mockRepo.Object);

    // Act
    await handler.Handle(command);

    // Assert
    mockRepo.Verify(x => x.AddAsync(It.IsAny<Entity>()), Times.Once);
}
```

## Writing New Tests

### 1. Follow the Existing Structure
Place tests in the appropriate directory matching the source code structure:
- `Domain/` → Domain layer tests
- `Application/` → Application layer tests
- `Infrastructure/` → Infrastructure layer tests

### 2. Naming Conventions
- **Test Class:** `{ClassName}Tests` (e.g., `PatientTests`)
- **Test Method:** `{MethodName}_{Scenario}_{ExpectedBehavior}` (e.g., `Create_WithValidData_ShouldSucceed`)

### 3. Use FluentAssertions
```csharp
// Instead of
Assert.Equal(expected, actual);

// Use
actual.Should().Be(expected);
```

### 4. Test Categories

Create comprehensive tests covering:

- **Happy Path:** Valid inputs, expected behavior
- **Validation:** Invalid inputs, edge cases
- **Error Handling:** Exceptions, error messages
- **Security:** Authorization, data protection
- **Edge Cases:** Boundary conditions, special characters

### 5. Test Data Builders
Create helper methods for complex test data:

```csharp
private static User CreateTestUser(string email = "test@example.com")
{
    return new User(
        email: email,
        firstName: "John",
        lastName: "Doe",
        azureAdB2CId: Guid.NewGuid().ToString(),
        roles: new List<UserRole> { UserRole.Doctor },
        createdBy: "system");
}
```

## Debugging Tests

### Visual Studio
1. Set breakpoints in test methods
2. Right-click test → Debug Test(s)
3. Use Test Explorer to view results

### VS Code
1. Install C# extension
2. Use "Debug Test" code lens above test methods
3. View results in Test Explorer panel

### Command Line
```bash
# Run with detailed logging
dotnet test --logger "console;verbosity=detailed"

# Run specific test with debugging
dotnet test --filter "FullyQualifiedName~TestName" --logger "console;verbosity=detailed"
```

## Common Issues

### Issue: Tests fail with database errors
**Solution:** Ensure each test uses a unique in-memory database:
```csharp
var options = new DbContextOptionsBuilder<ApplicationDbContext>()
    .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
    .Options;
```

### Issue: Async tests hanging
**Solution:** Always await async operations and pass cancellation tokens:
```csharp
var result = await repository.GetByIdAsync(id, CancellationToken.None);
```

### Issue: Mock not being called
**Solution:** Verify the mock setup matches the actual call:
```csharp
// Setup
mockRepo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
    .ReturnsAsync(user);

// Verify
mockRepo.Verify(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Once);
```

## Continuous Integration

### GitHub Actions Example
```yaml
- name: Test
  run: dotnet test --no-build --verbosity normal --collect:"XPlat Code Coverage"

- name: Code Coverage
  uses: codecov/codecov-action@v3
  with:
    files: ./coverage.cobertura.xml
```

## Best Practices

1. **Keep Tests Fast:** Use in-memory databases, avoid external dependencies
2. **Keep Tests Isolated:** Each test should be independent
3. **Keep Tests Simple:** One concept per test
4. **Keep Tests Maintainable:** Use helper methods, avoid duplication
5. **Test Behavior, Not Implementation:** Focus on outcomes, not internal details

## Resources

- [xUnit Documentation](https://xunit.net/)
- [Moq Documentation](https://github.com/moq/moq4)
- [FluentAssertions Documentation](https://fluentassertions.com/)
- [EF Core Testing](https://learn.microsoft.com/en-us/ef/core/testing/)

## Support

For questions or issues with tests:
1. Review existing tests for patterns
2. Check TEST_SUMMARY.md for coverage details
3. Consult team documentation
4. Reach out to Christopher Martin (test suite author)
