using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace Flyingpie.Blazor.PrerenderCache
{
	public interface IPrerenderCache
	{
		Task<TResult> GetOrAdd<TResult>(string key, Func<Task<TResult>> factory);

		string Serialize();
	}

	public class PrerenderCache : IPrerenderCache
	{
		private readonly IJSRuntime _js;

		public PrerenderCache(IJSRuntime js)
		{
			_js = js ?? throw new ArgumentNullException(nameof(js));
		}

		public Dictionary<string, object> Items { get; set; } = new Dictionary<string, object>();

		private bool _isLoaded;

		[Inject]
		public IJSRuntime JS { get; set; }

		public bool IsRunningOnServer => _js.GetType().Name == "UnsupportedJavaScriptRuntime";

		public async Task<TResult> GetOrAdd<TResult>(string key, Func<Task<TResult>> factory)
		{
			if (IsRunningOnServer)
			{
				var res = await factory();

				Items[key] = res;

				return res;
			}
			else
			{
				await LoadAsync();

				if (Items.Remove(key, out var item))
				{
					Console.WriteLine("From cache");

					var json = JsonSerializer.Serialize(item);

					return JsonSerializer.Deserialize<TResult>(json);
				}

				Console.WriteLine("From factory");
				return await factory();
			}
		}

		public async Task LoadAsync()
		{
			if (!_isLoaded)
			{
				Items = await _js.InvokeAsync<Dictionary<string, object>>("prerenderCache.load");
				_isLoaded = true;

				Console.WriteLine($"Loaded cache ({Items.Count} items).");
			}
		}

		public string Serialize() => JsonSerializer.Serialize(Items);
	}
}