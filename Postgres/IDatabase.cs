using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace Flyingpie.Utils.Postgres
{
	public interface IDatabase : IDisposable
	{
		Task ConnectAsync();

		Task<IDbTransaction> BeginTransactionAsync();

		/// <summary>
		/// Executes query without a result set, returning the affected rows.
		/// </summary>
		Task<int> ExecuteAsync(string sql, object? param = null);

		Task<TResult> ExecuteScalarAsync<TResult>(string sql, object? param = null);

		Task<IEnumerable<TResult>> QueryAsync<TResult>(string sql, object? param = null);
	}
}