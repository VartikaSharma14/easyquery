using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SpaServices.ReactDevelopmentServer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Korzh.EasyQuery.DbGates;
using Korzh.EasyQuery.Services;
using EasyData.Export;
using Npgsql;
using System.Data.Common;
using System;

namespace EqDemo
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
            services.AddCors(options =>
            {
                options.AddPolicy(name: "AllowAllPolicy",
                    builder =>
                    {
                        builder.AllowAnyOrigin();
                        builder.AllowAnyHeader();
                        builder.AllowAnyMethod();
                        builder.WithExposedHeaders("Content-Disposition");
                    });
            });

            services.AddControllersWithViews();

            // In production, the React files will be served from this directory
            services.AddSpaStaticFiles(configuration =>
            {
                configuration.RootPath = "ClientApp/build";
            });

            services.AddEasyQuery()
                    .UseSqlManager()
                    .RegisterDbGate<NpgSqlGate>()
                    .AddDefaultExporters()
                    .AddDataExporter<PdfDataExporter>("pdf")
                    .AddDataExporter<ExcelDataExporter>("excel");

            //to support non-Unicode code pages in PDF Exporter
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
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

            app.UseCors("AllowAllPolicy");

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            if (!env.IsDevelopment())
            {
                app.UseSpaStaticFiles();
            }

            app.UseRouting();

            app.UseEasyQuery((options) =>
            {
                string connectionstring = Configuration.GetConnectionString("EqDemoPostgres");
                options.Endpoint = "/api/easyquery";
                options.ConnectionString = connectionstring;
                options.UseManager<EasyQueryManagerSqlWithFilter>();
                options.UseDbConnection<NpgsqlConnection>();
                options.UseDbConnectionModelLoader();
                
            });
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller}/{action=Index}/{id?}");
            });

            app.UseSpa(spa =>
            {
                spa.Options.SourcePath = "ClientApp";

                if (env.IsDevelopment())
                {
                    spa.UseReactDevelopmentServer(npmScript: "start");
                }
            });
        }
    }


    public class EasyQueryManagerSqlWithFilter : EasyQueryManagerSql
    {
        public EasyQueryManagerSqlWithFilter(EasyQueryOptions options, IServiceProvider services) : base(options, services)
        {
        }

        protected override DbConnection GetConnectionCore()
        {
            var dbConnection = base.GetConnectionCore();

            dbConnection.ConnectionString = Model.Id switch
            {
                "test" => "Host=localhost;Database=postgres;Username=postgres;Password=Secure@123;Include Error Detail=true;",
                _ => "Host=localhost;Database=xsiadapter;Username=postgres;Password=Secure@123;Include Error Detail=true;"
            };


            return dbConnection;
        }
    }
}
