using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Blazored.LocalStorage;
using Fluxor;
using Lyra.Core.API;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Nebula.Data;
using Nebula.Store.WeatherUseCase;
using Nethereum.Metamask.Blazor;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using System.IO;
using Lyra.Data.API;

namespace Nebula
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddBlazoredLocalStorage();

            services.Configure<reCAPTCHAVerificationOptions>(Configuration.GetSection("reCAPTCHA"));
            services.Configure<SwapOptions>(Configuration.GetSection("Swap"));
            services.AddTransient<SampleAPI>();

            services.AddRazorPages();
            services.AddServerSideBlazor();
            services.AddSingleton<WeatherForecastService>();

            services.AddMvc();
            services.AddControllers();

            // for Metamask wallet
            services.AddScoped<IMetamaskInterop, MetamaskBlazorInterop>();
            services.AddScoped<MetamaskService>();
            services.AddScoped<MetamaskInterceptor>();

            services.AddHttpClient<FetchDataActionEffect>();

            var networkid = Configuration["network"];
            // use dedicate host to avoid "random" result from api.lyra.live which is dns round-robbined. <-- not fail safe
            //services.AddTransient<LyraRestClient>(a => LyraRestClient.Create(networkid, Environment.OSVersion.ToString(), "Nebula", "1.0"/*, $"http://nebula.{networkid}.lyra.live:{Neo.Settings.Default.P2P.WebAPI}/api/Node/"*/));

            services.AddScoped<ILyraAPI>(provider =>
            {
                var client = new LyraAggregatedClient(networkid);
                var t = Task.Run(async () => { await client.InitAsync(); });
                Task.WaitAll(t);
                return client;
            });

            // for database
            services.Configure<LiteDbOptions>(Configuration.GetSection("LiteDbOptions"));
            services.AddSingleton<ILiteDbContext, LiteDbContext>();
            services.AddTransient<INodeHistory, NodeHistory>();
            services.AddHostedService<IncentiveProgram>();

            var currentAssembly = typeof(Startup).Assembly;
            services.AddFluxor(options => options.ScanAssemblies(currentAssembly));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            //app.UseHttpsRedirection();
            app.UseDefaultFiles();

            // Set up custom content types - associating file extension to MIME type
            var provider = new FileExtensionContentTypeProvider();
            // Add new mappings
            provider.Mappings[".appx"] = "application/appx";
            provider.Mappings[".msix"] = "application/msix";
            provider.Mappings[".appxbundle"] = "application/appxbundle";
            provider.Mappings[".msixbundle"] = "application/msixbundle";
            provider.Mappings[".appinstaller"] = "application/appinstaller";
            provider.Mappings[".cer"] = "application/pkix-cert";

            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(
                    Path.Combine(env.WebRootPath, "apps")),
                RequestPath = "/apps",
                ContentTypeProvider = provider
            });
            app.UseStaticFiles();

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapBlazorHub();
                endpoints.MapControllers();
                endpoints.MapFallbackToPage("/_Host");
            });
        }
    }
}
