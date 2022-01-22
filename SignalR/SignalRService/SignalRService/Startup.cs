using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using SignalRService.Hubs;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tdb.framework.webapi.APIVersion;
using tdb.framework.webapi.Auth;
using tdb.framework.webapi.Cors;
using tdb.framework.webapi.standard.Exceptions;
using tdb.framework.webapi.standard.Log;
using tdb.framework.webapi.Swagger;

namespace SignalRService
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
            //��־
            services.AddTdbNLogger();
            //��������������Դ����
            services.AddTdbAllAllowCors();
            //��������֤����Ȩ����
            //services.AddTdbAuthJwtBearer("tangdabin20220108");
            services.AddAuthentication(x =>
            {
                x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(o =>
            {
                o.TokenValidationParameters = new TokenValidationParameters
                {
                    NameClaimType = tdb.framework.webapi.standard.Auth.TdbClaimTypes.Name,
                    RoleClaimType = tdb.framework.webapi.standard.Auth.TdbClaimTypes.Role,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes("tangdabin20220108")),
                    //����Audience
                    ValidateAudience = false,
                    //����Issuer
                    ValidateIssuer = false,
                    //����ķ�����ʱ��ƫ����
                    ClockSkew = TimeSpan.FromSeconds(10),
                };
                o.Events = new JwtBearerEvents()
                {
                    OnAuthenticationFailed = context =>
                    {
                        Logger.Ins.Fatal(context.Exception, "��֤��Ȩ�쳣");
                        return Task.CompletedTask;
                    },
                    OnForbidden = context =>
                    {
                        context.Response.Clear();
                        context.Response.ContentType = "application/json";
                        context.Response.StatusCode = 403;
                        context.Response.WriteAsync("Ȩ�޲���");
                        return Task.CompletedTask;
                    },
                    OnChallenge = context =>
                    {
                        context.HandleResponse();
                        context.Response.Clear();
                        context.Response.ContentType = "application/json";
                        context.Response.StatusCode = 401;
                        context.Response.WriteAsync("��֤δͨ��");
                        return Task.CompletedTask;
                    },
                    OnMessageReceived = (context) =>
                    {
                        if (!context.HttpContext.Request.Path.HasValue)
                        {
                            return Task.CompletedTask;
                        }

                        //�ص���������ж���Signalr��·��
                        var accessToken = context.HttpContext.Request.Query["access_token"];
                        var path = context.HttpContext.Request.Path;
                        if (!(string.IsNullOrWhiteSpace(accessToken)) && path.StartsWithSegments("/AuthHub"))
                        {
                            context.Token = accessToken;
                            return Task.CompletedTask;
                        }
                        return Task.CompletedTask;
                    }
                };
            });

            //��Ȩ
            services.AddAuthorization();
            //���api�汾���Ƽ��������
            services.AddTdbApiVersionExplorer();
            //swagger
            services.AddTdbSwaggerGenApiVer(o =>
            {
                o.ServiceCode = "tdb.signalR.demo";
                o.ServiceName = "SignalR Demo";
                o.LstXmlCommentsFileName.Add("SignalRService.xml");
            });

            //SignalR
            services.AddSignalR();

            //services.AddControllers();
            services.AddControllers(option =>
            {
                //�쳣����
                option.AddTdbGlobalException();
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IApiVersionDescriptionProvider provider)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            //��������������Դ����
            app.UseTdbAllAllowCors();
            //��֤
            app.UseAuthentication();
            //��Ȩ
            app.UseAuthorization();
            //swagger
            app.UseTdbSwaggerAndUIApiVer(provider);

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapHub<ServerTimeHub>("/ServerTimeHub");
                endpoints.MapHub<SendMsgHub>("/SendMsgHub");
                endpoints.MapHub<AuthHub>("/AuthHub");
            });

            //�㲥������ʱ��
            BroadcastTime(app);
        }

        //�㲥������ʱ��
        private void BroadcastTime(IApplicationBuilder app)
        {
            Task.Factory.StartNew(async () =>
            {
                while (true)
                {
                    var hubContext = app.ApplicationServices.GetService<IHubContext<ServerTimeHub, IServerTimeHub>>();
                    await hubContext.Clients.All.BroadcastTime(DateTime.Now);
                    await Task.Delay(1000);
                }
            });
        }
    }
}
