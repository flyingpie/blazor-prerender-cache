using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Flyingpie.Utils.Postgres.Migrations
{
	public interface IMigrationsRunner
	{
		bool IsMigrated { get; }

		IReadOnlyList<Migration> Migrations { get; }

		Task<IEnumerable<MigrationDto>> GetExecutedMigrationsAsync();

		Task MigrateAsync();
	}

	public class MigrationsRunner : IMigrationsRunner
	{
		private readonly static TimeSpan[] RetryIntervals = new[]
		{
			TimeSpan.FromSeconds(5),
			TimeSpan.FromSeconds(5),
			TimeSpan.FromSeconds(5)
		};

		private readonly IDatabase _db;
		private readonly ILogger _log;
		private readonly AsyncRetryPolicy _retryPolicy;

		public bool IsMigrated { get; private set; }

		public IReadOnlyList<Migration> Migrations { get; private set; }

		public MigrationsRunner(IDatabase db, Assembly migrationsAssembly, ILogger logger)
			: this(db, Migration.FromAssembly(migrationsAssembly), logger)
		{
		}

		public MigrationsRunner(IDatabase db, IEnumerable<Migration> migrations, ILogger logger)
		{
			_db = db ?? throw new ArgumentNullException(nameof(db));
			_log = logger ?? throw new ArgumentNullException(nameof(logger));

			Migrations = migrations?.ToList() ?? throw new ArgumentNullException(nameof(migrations));

			_retryPolicy = Policy
				.Handle<Exception>()
				.WaitAndRetryAsync(RetryIntervals, (ex, delay) => _log.LogError($"Error while executing migration, retrying in {delay}.", ex))
			;
		}

		public async Task CreateMigrationsTableAsync()
		{
			var sql = $@"
			CREATE TABLE IF NOT EXISTS public._migrations
			(
				version			int			NOT NULL	PRIMARY KEY,
				description		text		NOT NULL,
				timestamp		timestamp	NULL
			);
			";

			await _db.ExecuteAsync(sql);
		}

		public Task<IEnumerable<MigrationDto>> GetExecutedMigrationsAsync()
			=> _db.QueryAsync<MigrationDto>("SELECT version, description, timestamp FROM _migrations ORDER BY version");

		public async Task MigrateAsync()
		{
			if (IsMigrated) return;

			await _retryPolicy.ExecuteAsync(() => MigrateInternalAsync());

			IsMigrated = true;
		}

		private async Task MigrateInternalAsync()
		{
			// Make sure the migrations table is created
			await CreateMigrationsTableAsync();

			// Get migrations that are already executed
			var executedMigs = (await GetExecutedMigrationsAsync()).Select(m => m.Version).ToHashSet();

			// Loop through all migrations, ordered by version from low to high, and execute the ones that aren't executed yet
			foreach (var mig in Migrations)
			{
				// Skip already executed migrations
				if (executedMigs.Contains(mig.Version))
				{
					_log.LogInformation($"Skipping migration '{mig}' (already executed)");
					continue;
				}

				// Run migration in a transaction, so we can roll back the whole shebang if something goes wrong
				var transaction = await _db.BeginTransactionAsync();

				try
				{
					_log.LogInformation($"Executing migration '{mig}'...");

					// Execute migration
					await mig.MigrateAsync(_db);

					// Add migration to the history table
					await _db.ExecuteAsync("INSERT INTO _migrations (version, description, timestamp) VALUES (@version, @description, @timestamp)", new MigrationDto(mig));

					transaction.Commit();
				}
				catch
				{
					transaction.Rollback();
					throw;
				}
			}
		}
	}
}