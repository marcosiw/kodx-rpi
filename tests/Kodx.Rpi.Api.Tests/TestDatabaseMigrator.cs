using Kodx.Rpi.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Kodx.Rpi.Api.Tests;

/// <summary>
/// Este projeto roda como processo separado do Kodx.Rpi.Infrastructure.Tests (dotnet test por
/// projeto) e ambos chamam Database.MigrateAsync() na própria inicialização, contra o mesmo
/// Postgres — sem coordenação, isso é uma corrida real: mais de uma chamada pode achar a mesma
/// migration pendente ao mesmo tempo e uma delas falha com "already exists" (visto de verdade
/// no CI, com Postgres novo/sem histórico de migrations). Um lock consultivo do Postgres
/// reduz a janela da corrida, mas não é uma garantia hermética — por isso a segunda camada de
/// defesa: se, mesmo com o lock, duas chamadas ainda colidirem, o erro específico de objeto
/// duplicado é tratado como "outra chamada concorrente já aplicou esta migration", não como
/// falha real. Cópia do mesmo helper usado em Kodx.Rpi.Infrastructure.Tests — duplicado de
/// propósito, os dois projetos não compartilham um projeto de utilitários de teste.
/// </summary>
internal static class TestDatabaseMigrator
{
    private const long LockKey = 785412001;

    // 42P07 = duplicate_table, 42710 = duplicate_object (índices/constraints), 23505 = unique_violation.
    private static readonly HashSet<string> DuplicateObjectSqlStates = ["42P07", "42710", "23505"];

    public static async Task MigrateAsync(KodxRpiDbContext context, CancellationToken cancellationToken = default)
    {
        var lockConnectionString = new NpgsqlConnectionStringBuilder(context.Database.GetConnectionString())
        {
            Pooling = false
        }.ConnectionString;

        await using var lockConnection = new NpgsqlConnection(lockConnectionString);
        await lockConnection.OpenAsync(cancellationToken);

        await using (var lockCommand = lockConnection.CreateCommand())
        {
            lockCommand.CommandText = "SELECT pg_advisory_lock($1)";
            lockCommand.Parameters.Add(new NpgsqlParameter { Value = LockKey });
            await lockCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        try
        {
            await context.Database.MigrateAsync(cancellationToken);
        }
        catch (PostgresException ex) when (DuplicateObjectSqlStates.Contains(ex.SqlState ?? string.Empty))
        {
            // Outra chamada concorrente ganhou a corrida e já aplicou esta migration —
            // resultado equivalente a não ter tido nada pendente, não é uma falha real.
        }
        finally
        {
            await using var unlockCommand = lockConnection.CreateCommand();
            unlockCommand.CommandText = "SELECT pg_advisory_unlock($1)";
            unlockCommand.Parameters.Add(new NpgsqlParameter { Value = LockKey });
            await unlockCommand.ExecuteNonQueryAsync(cancellationToken);
        }
    }
}
