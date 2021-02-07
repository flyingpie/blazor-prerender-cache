using Microsoft.AspNetCore.Components;

namespace Blazor.PrerenderCache
{
	public partial class PrerenderCacheStore
	{
		[Inject]
		public IPrerenderCache Cache { get; set; }
	}
}