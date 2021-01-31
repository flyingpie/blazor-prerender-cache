using Dapper;
using Microsoft.Extensions.Logging;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace Flyingpie.Utils.Postgres
{
	public class PgDatabase : IDatabase
	{
		private readonly NpgsqlConnection _connection;
		private readonly ILogger _log;

		private bool _isDisposed;

		public PgDatabase(string connectionString, ILogger logger)
			: this(new NpgsqlConnection(connectionString), logger)
		{
			if (string.IsNullOrWhiteSpace(connectionString)) throw new ArgumentNullException(nameof(connectionString));
		}

		public PgDatabase(NpgsqlConnection connection, ILogger logger)
		{
			_connection = connection ?? throw new ArgumentNullException(nameof(connection));
			_log = logger ?? throw new ArgumentNullException(nameof(logger));
		}

		public async Task ConnectAsync()
		{
			if (_connection.State != ConnectionState.Broken && _connection.State != ConnectionState.Closed) return;

			try
			{
				_log.LogInformation($"Connecting to database at '{_connection.Host}' using username '{_connection.UserName}'");
				await _connection.OpenAsync();

				_log.LogInformation($"Successfully connected to database at '{_connection.Host}' using username '{_connection.UserName}'");
			}
			catch (Exception ex)
			{
				_log.LogError(ex, $"Something went wrong while connecting: {ex.Message}");
				await _connection.CloseAsync();

				throw;
			}
		}

		public Task<IDbTransaction> BeginTransactionAsync()
			=> Task.FromResult<IDbTransaction>(_connection.BeginTransaction());

		public Task<int> ExecuteAsync(string sql, object? param = null)
			=> _connection.ExecuteAsync(sql, param);

		public Task<TResult> ExecuteScalarAsync<TResult>(string sql, object? param = null)
			=> _connection.ExecuteScalarAsync<TResult>(sql, param);

		public Task<IEnumerable<TResult>> QueryAsync<TResult>(string sql, object? param = null)
			=> _connection.QueryAsync<TResult>(sql, param);

		public void Dispose()
		{
			if (_isDisposed) return;

			_connection.Dispose();
			_isDisposed = true;
		}

		public Task<TResult> WithConnectionAsync<TResult>(Func<IDbConnection, Task<TResult>> action)
			=> action(_connection);
	}
}