using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Npgsql;
using System;
using System.Threading.Tasks;

namespace Flyingpie.Utils.Postgres.UnitTest
{
	[TestClass]
	public class ResilientPgDatabaseTest
	{
		private Mock<IDatabase> _dbMock;
		private Mock<ILogger> _logMock;

		private ResilientPgDatabase _db;

		[TestInitialize]
		public void Setup()
		{
			_dbMock = new Mock<IDatabase>();
			_logMock = new Mock<ILogger>();

			_db = new ResilientPgDatabase(_dbMock.Object, new[]
			{
				TimeSpan.FromMilliseconds(5),
				TimeSpan.FromMilliseconds(5)
			}, _logMock.Object);
		}

		[TestMethod]
		public async Task Retry_Attempt1_Ok()
		{
			/// No exceptions

			// Act
			await _db.ExecuteAsync("sql");

			// Assert
			_dbMock.Verify(m => m.ExecuteAsync("sql", null), Times.Once());
		}

		[TestMethod]
		public async Task Retry_Attempt2_Ok()
		{
			/// 1 NpgsqlException

			// Arrange
			var count = 0;

			_dbMock
				.Setup(m => m.ExecuteAsync("sql", null))
				.ReturnsAsync(() =>
				{
					if (count++ < 1) throw new NpgsqlException();

					return 1;
				})
			;

			// Act
			await _db.ExecuteAsync("sql");

			// Assert
			_dbMock.Verify(m => m.ExecuteAsync("sql", null), Times.Exactly(2));
		}

		[TestMethod]
		public async Task Retry_Attempt3_Ok()
		{
			/// 2 NpgsqlExceptions
			var count = 0;

			// Arrange
			_dbMock
				.Setup(m => m.ExecuteAsync("sql", null))
				.ReturnsAsync(() =>
				{
					if (count++ < 2) throw new NpgsqlException();

					return 1;
				})
			;

			// Act
			await _db.ExecuteAsync("sql");

			// Assert
			_dbMock.Verify(m => m.ExecuteAsync("sql", null), Times.Exactly(3));
		}

		[TestMethod]
		public void Retry_Attempt3_Rethrow()
		{
			/// 3 NpgsqlExceptions

			// Arrange
			var count = 0;
			var exc = new NpgsqlException();

			_dbMock.Setup(m => m.ExecuteAsync("sql", null)).Throws(exc);

			// Act + Assert
			//AssertException.Thrown
			//(
			//	() => _db.ExecuteAsync("sql"),
			//	ex => Assert.AreEqual(exc, ex)
			//);

			//_dbMock.Verify(m => m.ExecuteAsync("sql", null), Times.Exactly(3));

			Assert.Inconclusive();
		}

		[TestMethod]
		public void Retry_Attempt1_Rethrow_PostgresException()
		{
			/// PostgresException

			// Arrange
			var exc = new PostgresException("", "", "", "");

			_dbMock.Setup(m => m.ExecuteAsync("sql", null)).Throws(exc);

			// Act + Assert
			//AssertException.Thrown
			//(
			//	() => _db.ExecuteAsync("sql", null),
			//	ex => Assert.AreEqual(exc, ex)
			//);

			//_dbMock.Verify(m => m.ExecuteAsync("sql", null), Times.Once());

			Assert.Inconclusive();
		}

		[TestMethod]
		public void Retry_Attempt1_Rethrow_Exception()
		{
			/// Exception

			// Arrange
			var exc = new Exception();

			//_dbMock.Setup(m => m.ExecuteAsync("sql", null)).Throws(exc);

			//// Act + Assert
			//AssertException.Thrown
			//(
			//	() => _db.ExecuteAsync("sql", null),
			//	ex => Assert.AreEqual(exc, ex)
			//);

			Assert.Inconclusive();
		}
	}
}