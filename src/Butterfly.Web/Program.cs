using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

// The browser host: no components, only the bridge (Bridge.cs) the page calls.
var builder = WebAssemblyHostBuilder.CreateDefault(args);
await builder.Build().RunAsync();
