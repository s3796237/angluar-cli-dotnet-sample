﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace myApp
{
	public class Startup
	{
		public Startup(IHostingEnvironment env)
		{
			var builder = new ConfigurationBuilder()
				.SetBasePath(env.ContentRootPath)
				.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
				.AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
				.AddEnvironmentVariables();
			Configuration = builder.Build();
		}

		public IConfigurationRoot Configuration { get; }

		// This method gets called by the runtime. Use this method to add services to the container.
		public void ConfigureServices(IServiceCollection services)
		{
			// Add service and create Policy with options
			services.AddCors(options =>
			{
				options.AddPolicy("CorsPolicy",
					builder => builder.AllowAnyOrigin()
					.AllowAnyMethod()
					.AllowAnyHeader()
					.AllowCredentials());
			});

			// Add framework services.
			services.AddMvc();
		}

		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
		public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
		{
			loggerFactory.AddConsole(Configuration.GetSection("Logging"));
			loggerFactory.AddDebug();

			// global policy - assign here or on each controller
			app.UseCors("CorsPolicy");

			app.Use(async (context, next) =>
			{
				await next();

				// If there's no available file and the request doesn't contain an extension, we're probably trying to access a page.
				// Rewrite request to use app root
				if (context.Response.StatusCode == 404 && !Path.HasExtension(context.Request.Path.Value) && !context.Request.Path.Value.StartsWith("/api"))
				{
					context.Request.Path = "/index.html";
					context.Response.StatusCode = 200; // Make sure we update the status code, otherwise it returns 404
					await next();
				}
			});

			app.UseDefaultFiles();
			app.UseStaticFiles();

			app.UseMvc();
		}
	}
}