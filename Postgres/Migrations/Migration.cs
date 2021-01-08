using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Flyingpie.Utils.Postgres.Migrations
{
	public abstract class Migration
	{
		public static IReadOnlyList<Migration> FromAssembly(Assembly assembly)
		{
			if (assembly == null) throw new ArgumentNullException(nameof(assembly));

			return assembly
				.GetTypes()
				.Where(t => typeof(Migration).IsAssignableFrom(t))
				.Where(t => !t.IsAbstract)
				.Select(t => (Migration)Activator.CreateInstance(t))
				.OrderBy(t => t.Version)
				.ToList();
		}

		public abstract int Version { get; }

		public abstract string Description { get; }

		public abstract Task MigrateAsync(IDatabase db);

		public override string ToString() => $"[{Version:0000}] {Description}";
	}
}