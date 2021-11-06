using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SwaggerAPIVer
{
    /// <summary>
    /// 
    /// </summary>
    public class Startup
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="configuration"></param>
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        /// <summary>
        /// 
        /// </summary>
        public IConfiguration Configuration { get; }

        /// <summary>
        /// This method gets called by the runtime. Use this method to add services to the container.
        /// </summary>
        /// <param name="services"></param>
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddApiVersioning(o =>
            {
                //Ϊtrueʱ��API������Ӧ��header�з���֧�ֵİ汾��Ϣ
                o.ReportApiVersions = true;
                ////������δָ���汾ʱĬ��Ϊ1.0
                //o.DefaultApiVersion = new ApiVersion(1, 0);
                ////�汾����ʲô��ʽ��ʲô�ֶδ���
                //o.ApiVersionReader = ApiVersionReader.Combine(new HeaderApiVersionReader("api-version"));
                ////�ڲ��ṩ�汾��ʱ��Ĭ��Ϊ1.0  �������Ӵ����ã����ṩ�汾��ʱ�ᱨ��"message": "An API version is required, but was not specified."
                //o.AssumeDefaultVersionWhenUnspecified = true;
                ////Ĭ���Ե�ǰ��߰汾���з���
                //o.ApiVersionSelector = new CurrentImplementationApiVersionSelector(o);
            }).AddVersionedApiExplorer(o =>
            {
                //��֪ͨswagger�滻������·���еİ汾������api�汾
                o.SubstituteApiVersionInUrl = true;
                // �汾���ĸ�ʽ��v+�汾��
                o.GroupNameFormat = "'v'VVV";
                ////δָ��ʱ����Ĭ�ϰ汾
                //o.AssumeDefaultVersionWhenUnspecified = true;
            });

            //swagger
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
                {
                    Version = "v1",
                    Title = "API V1�ӿ��ĵ�Title",
                    Description = "API V1�ӿ��ĵ�Description"
                });

                c.SwaggerDoc("v2", new Microsoft.OpenApi.Models.OpenApiInfo
                {
                    Version = "v2",
                    Title = "API V2�ӿ��ĵ�Title",
                    Description = "API V2�ӿ��ĵ�Description"
                });

                //�ӿ�ע��
                var xmlAPI = Path.Combine(AppContext.BaseDirectory, "SwaggerAPIVer.xml");
                c.IncludeXmlComments(xmlAPI, true);
            });

            services.AddControllers();
        }

        /// <summary>
        /// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        /// </summary>
        /// <param name="app"></param>
        /// <param name="env"></param>
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseAuthorization();

            //swagger
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "V1 Docs");
                c.SwaggerEndpoint("/swagger/v2/swagger.json", "V2 Docs");
            });

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
