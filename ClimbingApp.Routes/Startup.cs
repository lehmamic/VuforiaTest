﻿using AutoMapper;
using ClimbingApp.Routes.Services.ImageRecognition;
using ClimbingApp.Routes.Services.Media;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Raven.Client.Documents;

namespace ClimbingApp.Routes
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton((serviceProvider) =>
            {
                return new DocumentStore { Urls = new[] { this.Configuration.GetValue<string>("RavenDb:Url") } }.Initialize();
            });

            services.AddScoped((serviceProvider) =>
            {
                var store = serviceProvider.GetService<IDocumentStore>();
                return store.OpenAsyncSession(new Raven.Client.Documents.Session.SessionOptions
                {
                    Database = this.Configuration.GetValue<string>("RavenDb:Database"),
                });
            });

            services.AddSingleton((serviceProvider) =>
            {
                var config = new MapperConfiguration(cfg => {
                    cfg.AddMaps(typeof(Startup).Assembly);
                });

                config.AssertConfigurationIsValid();

                return config;
            });

            services.AddSingleton<IMapper>((serviceProvider) =>
            {
                var config = serviceProvider.GetService<MapperConfiguration>();
                return new Mapper(config);
            });

            services.Configure<ImageRecognitionApiSettings>(Configuration.GetSection("ImageRecognitionApi"));
            services.AddHttpClient<IImageRecognitionApiClient, ImageRecognitionApiClient>();

            services.Configure<MediaApiSettings>(Configuration.GetSection("MediaApi"));
            services.AddHttpClient<IMediaApiClient, MediaApiClient>();

            services.AddCors(options =>
            {
                options.AddPolicy("default", builder =>
                {
                    builder.AllowAnyOrigin()
                           .AllowAnyHeader()
                           .AllowAnyMethod();
                });
            });

            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                // app.UseHsts();
            }

            app.UseCors("default");
            // app.UseHttpsRedirection();
            app.UseMvc();
        }
    }
}
