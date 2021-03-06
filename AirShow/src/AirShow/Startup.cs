﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using AirShow.Models.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using AirShow.Models.EF;
using AirShow.Models.Seeders;
using AirShow.Models;
using AirShow.Models.Interfaces;
using AirShow.Models.FileRepositories;
using AirShow.WebSockets;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Collections.Concurrent;
using AirShow.Models.AppRepositories;
using AirShow.Models.Services;
using AirShow.Models.Cleaners;

namespace AirShow
{
    public class Startup
    {

        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Add framework services.
            services.AddMvc();
            services.AddEntityFrameworkSqlite();
            services.AddDbContext<AirShowContext>(options => options.UseSqlite("Filename=./AirShowDB.db"));
            services.AddIdentity<User, IdentityRole>().AddEntityFrameworkStores<AirShowContext>();
            services.AddScoped<BasicDBSeeder>();
            services.AddScoped<IPresentationsRepository, EFPresentationsRepository>();
            services.AddScoped<IPresentationFilesRepository, BasicFileRepository>();
            services.AddScoped<IPresentationThumbnailRepository, LocalThumbnailRepository>();
            services.AddScoped<ICategoriesRepository, EFCategoriesRepository>();
            services.AddScoped<ITagsRepository, EFTagsRepository>();
            services.AddScoped<IUsersRepository, EFUsersRepository>();
            services.AddSingleton<GlobalSessionManager>();
            services.AddSingleton<IPresentationFilesRepository, BasicFileRepository>();
            services.AddSingleton<IMailService, SendGridMailService>();
            services.AddSingleton<IConfigurationRoot>(Configuration);
            services.AddSingleton<UnconfirmedAccountsCleaner>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
           

            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

            var seeder = app.ApplicationServices.GetService<BasicDBSeeder>();
            seeder.Run();

            var cleaner = app.ApplicationServices.GetService<UnconfirmedAccountsCleaner>();
            cleaner.Run();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseBrowserLink();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }


            app.UseStaticFiles(new StaticFileOptions()
            {
                OnPrepareResponse = context =>
                {
                    context.Context.Response.Headers.Add("Cache-Control", "no-cache, no-store");
                    context.Context.Response.Headers.Add("Expires", "-1");
                }
            });

            app.UseWebSockets();
            app.Use(async (http, next) =>
            {
                if (http.WebSockets.IsWebSocketRequest)
                {
                    var webSocket = await http.WebSockets.AcceptWebSocketAsync();
                    var gwss = app.ApplicationServices.GetService<GlobalSessionManager>();
                    gwss.HandleWebSocketV(webSocket);
                }
                else
                {
                    await next();
                }
            });

            app.UseIdentity();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Explore}/{action=Index}/{id?}");
            });
        }


    }
}
