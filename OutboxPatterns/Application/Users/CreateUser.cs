using FluentValidation;
using OutboxPatterns.Domain;
using OutboxPatterns.Infrastructure;
using System.Text.Json;

namespace OutboxPatterns.Application.Users;

public record CreateUserRequest(Guid Id, string Name, string Email, string Password);
public record CreateUserResponse(Guid Id, string Name, string Email, string Password);

public class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(100).WithMessage("Name must not exceed 100 characters.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Email is not valid.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters.")
            .Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches(@"[0-9]").WithMessage("Password must contain at least one digit.");
    }
}

public interface IUserService
{
    Task<CreateUserResponse> CreateUserRegistration(CreateUserRequest request, CancellationToken cancellationToken);
}

public class CreateUser(OutboxDbContext outboxDbContext, ILogger<CreateUser> logger) : IUserService
{
    public async Task<CreateUserResponse> CreateUserRegistration(
        CreateUserRequest createUserRequest, CancellationToken cancellationToken)
    {
        await using var transaction = await outboxDbContext.Database.BeginTransactionAsync(cancellationToken);
        logger.LogInformation("Transaction is run");
        try
        {
            var createUser = new Userss
            {
                Id = Guid.NewGuid(),
                Name = createUserRequest.Name,
                Email = createUserRequest.Email,
                Password = createUserRequest.Password
            };
            await outboxDbContext.AddAsync(createUser, cancellationToken);

            var outboxMessage = new OutboxTable
            {
                Id = Guid.NewGuid(),
                EventType = "UserCreated",
                Payload = JsonSerializer.Serialize(new
                {
                    createUser.Id,
                    createUser.Name,
                    createUser.Email
                }),
                OccurredOn = DateTime.UtcNow
            };
            await outboxDbContext.OutboxTables.AddAsync(outboxMessage, cancellationToken);

            await outboxDbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            logger.LogInformation("Create new users is Success");

            return new CreateUserResponse(
                createUser.Id,
                createUser.Name,
                createUser.Email,
                createUser.Password
            );
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            logger.LogError("Create new process is failed");
            throw;
        }
    }
}

public static class Endpoint
{
    public static void CreateNewUsers(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("Users");

        group.MapPost("/", async (
            CreateUserRequest request,
            IUserService service,
            IValidator<CreateUserRequest> validator,
            CancellationToken cancellationToken) =>
        {
            var validationResult = await validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
                return Results.ValidationProblem(validationResult.ToDictionary());

            var createUser = await service.CreateUserRegistration(request, cancellationToken);
            if (createUser is null) return Results.NotFound();
            return Results.Ok(createUser);
        })
        .WithName("CreateUsers")
        .Produces<Userss>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .ProducesValidationProblem();

        
    }
    
}

