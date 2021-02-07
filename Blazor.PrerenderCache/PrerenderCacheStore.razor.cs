using Microsoft.AspNetCore.Components;

namespace Flyingpie.Blazor.PrerenderCache
{
	public partial class PrerenderCacheStore
	{
		[Inject]
		public IPrerenderCache Cache { get; set; }
	}
}