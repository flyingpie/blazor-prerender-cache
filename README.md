# Blazor Prerender Cache
Cache for use with Blazor WebAssembly and server prerendering. Prevents/eases the double initialize UI flash.

## The Problem
This is an attempt to solve/ease the problem, where Blazor WebAssembly + Server Prerendering on pages can cause a "UI Flash".

1. Page is rendered on the server (including some async step such as a database call), and the resulting static version of the page is sent to the browser.
2. The browser renders the static page, leading to a fast, SEO-friendly initial load.
3. Blazor is being set up in the background, pulling in .Net and the framework binaries.
4. The app starts and replaces (part of) the initial static page with the dynamic, client-side rendered one.
5. Some async data gets loaded once again, this time by the client.

The time it takes to fetch the data for the second time is what causes the UI to flash.

![The Problem](https://github.com/flyingpie/blazor-prerender-cache/raw/dev/img/DoubleLoad.gif)

## The "Fix"

This piece of code adds a cache, that handles a lot like the IMemoryCache (which is one of the suggested ways of solving this problem in the first place).

Though, instead of storing the cache on the server (thereby removing eg. a second trip to the database), the cache is stored in the static prerendered page.

Then, when the page gets a second initialization, the cache is loaded from the static page and data gets pulled from there.
This prevents the trip to the server entirely, removing the async step and drastically reducing/removing the second loading phase.

## Installation

The library is available as a NuGet package for convienence, but it's so small that it might preferable to just copy the 2 files into your project.

1. PrerenderCache.cs
2. PrerenderCacheStore.razor

```
Install-Package Flyingpie.Blazor.PrerenderCache
```

## Usage

1. Register IPrerenderCache.

```csharp
builder.Services
  .AddScoped<IPrerenderCache, PrerenderCache>()
;
```

2. Include the PrerenderCacheStore component in _Host.cshtml.

```cshtml
<component type="typeof(Flyingpie.Blazor.PrerenderCache.PrerenderCacheStore)" render-mode="Static" />
```

Note that this component must be added to the very bottom of the page, so it gets loaded after all the other components.

This is to prevent the component from rendering before all to-be-cached-data is available from IPrerenderCache.

3. Fetch data subject to double-initialization through IPrerenderCache.

```csharp
[Inject]
public IPrerenderCache { get; set; }

protected override async Task OnInitializedAsync()
{
  // Original
  // VM = await StashApiClient.GetPostsAsync();

  // Cacheable
  VM = await Cache.GetOrAdd(nameof(Articles), () => StashApiClient.GetPostsAsync());
}
```

## Under The Hood

You can see the result of the cache by looking at the page source:

```html
<script type="text/javascript">
	window.prerenderCache = {
		cache: {"Articles":{"Limit":25,"Offset":0,"Count":4,"Posts":[{"Id":"37af5802-7ea4-4def-b090-5bd4ebf4dabc", ... }]}},
		load: () => window.prerenderCache.cache
	};
</script>
```
