using EMR.Application.Common.Exceptions;
using EMR.Domain.Interfaces;
using EMR.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace EMR.Infrastructure.Repositories;

/// <summary>
/// Unit of Work implementation for managing database transactions
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;
    private IDbContextTransaction? _currentTransaction;

    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            // Handle unique constraint violations and convert to domain exception
            if (IsUniqueConstraintViolation(ex))
            {
                var propertyName = GetViolatedPropertyName(ex);
                var message = GetUserFriendlyErrorMessage(ex, propertyName);
                throw new DuplicateEntityException(message, propertyName, ex);
            }
            throw;
        }
    }

    /// <summary>
    /// Check if the exception is a unique constraint violation
    /// </summary>
    private bool IsUniqueConstraintViolation(DbUpdateException ex)
    {
        var innerException = ex.InnerException?.Message ?? ex.Message;

        // PostgreSQL unique constraint violation codes
        return innerException.Contains("23505") || // PostgreSQL unique violation
               innerException.Contains("duplicate key") ||
               innerException.Contains("unique constraint", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Get the property name that violated the unique constraint
    /// </summary>
    private string GetViolatedPropertyName(DbUpdateException ex)
    {
        var innerException = ex.InnerException?.Message ?? ex.Message;

        if (innerException.Contains("IX_Users_Email", StringComparison.OrdinalIgnoreCase) ||
            innerException.Contains("email", StringComparison.OrdinalIgnoreCase))
        {
            return "Email";
        }

        if (innerException.Contains("IX_Users_AzureAdB2CId", StringComparison.OrdinalIgnoreCase) ||
            innerException.Contains("azureadb2cid", StringComparison.OrdinalIgnoreCase))
        {
            return "AzureAdB2CId";
        }

        return "Unknown";
    }

    /// <summary>
    /// Get user-friendly error message from unique constraint violation
    /// </summary>
    private string GetUserFriendlyErrorMessage(DbUpdateException ex, string propertyName)
    {
        return propertyName switch
        {
            "Email" => "A user with this email address already exists.",
            "AzureAdB2CId" => "A user with this Azure AD B2C ID already exists.",
            _ => "A user with these credentials already exists."
        };
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction != null)
        {
            throw new InvalidOperationException("A transaction is already in progress.");
        }

        _currentTransaction = await _context.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction == null)
        {
            throw new InvalidOperationException("No transaction is in progress.");
        }

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            await _currentTransaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await RollbackTransactionAsync(cancellationToken);
            throw;
        }
        finally
        {
            if (_currentTransaction != null)
            {
                await _currentTransaction.DisposeAsync();
                _currentTransaction = null;
            }
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction == null)
        {
            throw new InvalidOperationException("No transaction is in progress.");
        }

        try
        {
            await _currentTransaction.RollbackAsync(cancellationToken);
        }
        finally
        {
            if (_currentTransaction != null)
            {
                await _currentTransaction.DisposeAsync();
                _currentTransaction = null;
            }
        }
    }

    public void Dispose()
    {
        _currentTransaction?.Dispose();
        _context.Dispose();
    }
}
