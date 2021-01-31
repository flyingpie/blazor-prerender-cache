using Dapper;
using System.Linq;

namespace Flyingpie.Utils.Postgres.Extensions
{
	public static class DapperExtensions
	{
		/// <summary>
		/// Adds a parameter to the specified <see cref="DynamicParameters"/> and returns the generated name.
		/// </summary>
		public static string AddNext(this DynamicParameters param, object value)
		{
			// Generate parameter name
			var pName = $"p{param.ParameterNames.Count() + 1}";

			// Add parameter and value
			param.Add(pName, value);

			// Return generated name
			return pName;
		}
	}
}