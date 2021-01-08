using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Flyingpie.Utils.Postgres.Extensions
{
	public static class DatabaseExtensions
	{
		public static async Task<IEnumerable<string>> GetTableNamesAsync(this IDatabase db)
		{
			if (db == null) throw new ArgumentNullException(nameof(db));

			const string sql = "SELECT tablename FROM pg_tables WHERE schemaname = 'public'";

			return await db.QueryAsync<string>(sql);
		}
	}
}