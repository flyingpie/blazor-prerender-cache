using Microsoft.Extensions.Logging;
using Npgsql;
using Polly;
using Polly.Retry;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace Flyingpie.Utils.Postgres
{
	/// <summary>
	/// Adds thread-safety and auto-retry to <see cref="IDatabase"/>.
	/// </summary>
	public class ResilientPgDatabase : IDatabase
	{
		private readonly static TimeSpan[] DefaultRetryIntervals = new[]
		{
			TimeSpan.FromSeconds(1),

			TimeSpan.FromSeconds(5),
			TimeSpan.FromSeconds(5),

			TimeSpan.FromSeconds(10),
			TimeSpan.FromSeconds(10),

			TimeSpan.FromSeconds(15),
			TimeSpan.FromSeconds(15)
		};

		private readonly AsyncRetryPolicy _retryPolicy;
		private readonly IDatabase _db;
		private readonly SemaphoreSlim _sema = new SemaphoreSlim(1);
		private readonly ILogger _log;
		private bool _isDisposed;

		public ResilientPgDatabase(string connectionString, ILogger logger)
			: this(new PgDatabase(connectionString, logger), DefaultRetryIntervals, logger)
		{ }

		public ResilientPgDatabase(IDatabase db, ILogger logger)
			: this(db, DefaultRetryIntervals, logger)
		{ }

		public ResilientPgDatabase(IDatabase db, TimeSpan[] retryIntervals, ILogger logger)
		{
			_db = db ?? throw new ArgumentNullException(nameof(db));
			_log = logger ?? throw new ArgumentNullException(nameof(logger));

			_retryPolicy = Policy
				// Handle NpgsqlExceptions, but not the subclass PostgresException,
				// since that concerns SQL errors such as duplicate keys and syntax errors.
				.Handle<NpgsqlException>(ex => ex.GetType() == typeof(NpgsqlException))
				.WaitAndRetryAsync(retryIntervals, (ex, delay) => _log.LogError("Error while executing database operation, retrying.", ex))
			;
		}

		public Task ConnectAsync() => _db.ConnectAsync();

		public async Task<IDbTransaction> BeginTransactionAsync()
		{
			await ConnectAsync();

			return await _db.BeginTransactionAsync();
		}

		public Task<int> ExecuteAsync(string sql, object? param = null)
		{
			if (string.IsNullOrWhiteSpace(sql)) throw new ArgumentNullException(nameof(sql));

			return ExecuteInternalAsync(db => db.ExecuteAsync(sql, param));
		}

		public Task<TResult> ExecuteScalarAsync<TResult>(string sql, object? param = null)
		{
			if (string.IsNullOrWhiteSpace(sql)) throw new ArgumentNullException(nameof(sql));

			return ExecuteInternalAsync(db => db.ExecuteScalarAsync<TResult>(sql, param));
		}

		public Task<IEnumerable<TResult>> QueryAsync<TResult>(string sql, object? param = null)
		{
			if (string.IsNullOrWhiteSpace(sql)) throw new ArgumentNullException(nameof(sql));

			return ExecuteInternalAsync(db => db.QueryAsync<TResult>(sql, param));
		}

		public void Dispose()
		{
			if (_isDisposed) return;

			_db.Dispose();
			_isDisposed = true;
		}

		private Task<TResult> ExecuteInternalAsync<TResult>(Func<IDatabase, Task<TResult>> action)
		{
			return _retryPolicy.ExecuteAsync(async () =>
			{
				try
				{
					await _sema.WaitAsync();

					await _db.ConnectAsync();

					return await action(_db);
				}
				finally
				{
					_sema.Release();
				}
			});
		}

		public Task<TResult> WithConnectionAsync<TResult>(Func<IDbConnection, Task<TResult>> action)
		{
			return _retryPolicy.ExecuteAsync(async () =>
			{
				try
				{
					await _sema.WaitAsync();

					await _db.ConnectAsync();

					return await _db.WithConnectionAsync<TResult>(action);
				}
				finally
				{
					_sema.Release();
				}
			});
		}
	}
}