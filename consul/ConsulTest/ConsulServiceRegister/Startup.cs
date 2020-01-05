using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Consul;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ConsulServiceRegister
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
            services.AddControllers();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IConfiguration config, IHostApplicationLifetime appLifetime)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            //�ѱ�����ע�ᵽConsul
            this.RegisterToConsul(config, appLifetime);
        }

        /// <summary>
        /// �ѱ�����ע�ᵽConsul
        /// </summary>
        /// <param name="config">��������</param>
        /// <param name="appLifetime">������������</param>
        private void RegisterToConsul(IConfiguration config, IHostApplicationLifetime appLifetime)
        {
            //Consul��ַ
            var consulClient = new ConsulClient(p => { p.Address = new Uri($"http://127.0.0.1:8500"); });

            //����IP
            var localIP = this.GetLocalIP();
            //���ط���˿�
            var localPort = Convert.ToInt32(config["port"]); //�˿ںŴ������в�����ȡ��ע��Ŀǰû�ҵ�ֱ�ӻ�ȡ����������Ķ˿ڵķ�����

            //�����������
            var httpCheck = new AgentServiceCheck()
            {
                DeregisterCriticalServiceAfter = TimeSpan.FromSeconds(15), //����ֹͣ��ú�ע��
                Interval = TimeSpan.FromSeconds(10), //�������������һ��
                HTTP = $"http://{localIP}:{localPort}/api/Health/Check", //��������ַ���������ṩ�ĵ�ַ
                Timeout = TimeSpan.FromSeconds(5)  //������鳬ʱʱ��
            };

            //������������ͨ�������в������벻ͬ�ķ�������ģ�������в�ͬ�ķ���[��ʵֻ��ͬһ���ӿ���Ŀ�Ĳ�ͬ����ʵ��]��
            var serviceName = config["service"];

            //ע����Ϣ
            var registration = new AgentServiceRegistration()
            {
                ID = $"{localIP}:{localPort}", //����ID��Ψһ
                Name = serviceName, //���������������Ⱥ�����ǵķ�����Ӧ����һ���ģ�����ID��һ����
                Address = $"{localIP}", //�����ַ
                Port = localPort, //����˿�
                Tags = new string[] { }, //�����ǩ��һ�������������Ȩ�صȱ��ط���������Ϣ
                Checks = new[] { httpCheck }, //�����������
            };

            //��Consulע�����
            consulClient.Agent.ServiceRegister(registration).Wait();

            //�رճ����ע����Consul
            appLifetime.ApplicationStopped.Register(() =>
            {
                consulClient.Agent.ServiceDeregister(registration.ID).Wait();
            });
        }

        /// <summary>
        /// ��ȡ����IP
        /// </summary>
        /// <returns></returns>
        private string GetLocalIP()
        {
            var ip = System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces()
                    .Select(p => p.GetIPProperties())
                    .SelectMany(p => p.UnicastAddresses)
                    .Where(p => p.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork && !System.Net.IPAddress.IsLoopback(p.Address))
                    .FirstOrDefault()?.Address.ToString();
            return ip;
        }
    }
}
