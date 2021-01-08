using System;

namespace Flyingpie.Utils.Postgres.Migrations
{
	public class MigrationDto
	{
		public MigrationDto()
		{
		}

		public MigrationDto(Migration migration)
		{
			if (migration == null) throw new ArgumentNullException(nameof(migration));

			Version = migration.Version;
			Description = migration.Description;
			Timestamp = DateTime.UtcNow;
		}

		public int Version { get; set; }

		public string Description { get; set; }

		public DateTime Timestamp { get; set; }
	}
}