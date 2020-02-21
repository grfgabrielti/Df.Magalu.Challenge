using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Df.Magalu.Challenge.Domain.Entity;
using Df.Magalu.Challenge.Domain.Dto;
using AutoMapper;
using Df.Magalu.Challenge.Service;
using Df.Magalu.Challenge.Service.Interfaces;
using Df.Magalu.Challenge.Domain.Interfaces.Factories;
using Df.Magalu.Challenge.Domain.Factories;
using Df.Magalu.Challenge.Domain.Interfaces.Acl;
using Df.Magalu.Challenge.Acl;
using Df.Magalu.Challenge.Domain.Interfaces.Repositories;
using Df.Magalu.Challenge.Data;
using Df.Magalu.Challenge.Data.Context;
using Df.Magalu.Challenge.Data.Repositories;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using HealthChecks.UI.Client;
using System;
using System.Threading.Tasks;
using Df.Message.Broker.ServiceBus.Standard.Contracts;
using Df.Message.Broker.ServiceBus.Standard;
using Df.Magalu.Challenge.Domain.Interfaces.Entity;

namespace Df.Magalu.Challenge.Api
{
    public class Startup
    {
        public IConfiguration Configuration { get; }
        private string _serviceBusConnectionString = "Endpoint=sb://dfmessage.servicebus.windows.net/;SharedAccessKeyName=meu_token;SharedAccessKey=+JWuf875RyazD1Cj8/ezM49LiPk08c+B0lm/I4nqx98=";
        private string _topicName = "df.magalu.challenge";

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services
            .AddHealthChecks()
            .AddMongoDb("mongodb://localhost:27017", "Magalu", "Magalu - DB 27017")
            .Services
            .AddOptions()
            .AddControllers();

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Magalu Challege", Version = "v1" });
            });

            var config = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<ProductLabsDto,Product>();
            });
            IMapper mapper = config.CreateMapper();
            services.AddSingleton(mapper);



            services.AddTransient<IClientFactory, ClientFactory>();

            services.AddTransient<IProductLabsAcl, ProductLabsAcl>();

            services.AddScoped<IMongoContext, MongoContext>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddTransient<IClientRepository, ClientRepository>();

            services.AddTransient<IClientService, ClientService>();

            services.AddSingleton<IPublisher>(new Publisher(_serviceBusConnectionString, _topicName));


            Task t3 = new Task( () => 
            {
                Console.WriteLine("Chupa fui executado");
            });

            new Consumer(_serviceBusConnectionString, _topicName).RegisterOnMessageHandlerAndReceiveMessages<IClient>(t3);
        }
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app
               .UseRouting()
               .UseEndpoints(config =>
               {
                   config.MapHealthChecks("healthz", new HealthCheckOptions()
                   {
                       Predicate = _ => true,
                       ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
                   });
                   config.MapDefaultControllerRoute();
                   config.MapControllers();
               })
            .UseSwagger()
            .UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Magalu Challege");
            })
            .UseAuthorization();

        }
    }
}
