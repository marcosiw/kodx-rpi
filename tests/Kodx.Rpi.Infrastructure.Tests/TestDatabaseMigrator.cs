using Kodx.Rpi.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Kodx.Rpi.Infrastructure.Tests;

/// <summary>
/// Várias classes de teste (e, no CI, o projeto Kodx.Rpi.Api.Tests rodando como processo
/// separado) chamam <c>Database.MigrateAsync()</c> na própria <see cref="IAsyncLifetime.InitializeAsync"/>
/// contra o mesmo Postgres — sem coordenação, isso é uma corrida real: mais de uma chamada pode
/// achar a mesma migration pendente ao mesmo tempo e uma delas falha com "already exists"
/// (visto de verdade no CI, com Postgres novo/sem histórico de migrations). Um lock consultivo
/// do Postgres reduz a janela da corrida, mas não é uma garantia hermética (a checagem de
/// migrations pendentes e a aplicação em si não são atômicas de ponta a ponta dentro do
/// próprio EF Core) — por isso a segunda camada de defesa: se, mesmo com o lock, duas chamadas
/// ainda colidirem, o erro específico de objeto duplicado é tratado como "outra chamada
/// concorrente já aplicou esta migration", não como falha real.
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
