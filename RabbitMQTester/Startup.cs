using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Threading;

namespace RabbitMQTester
{
    public class Startup
    {

        bool isProducer = true;
        bool isConsumer = true;
        string receiveUser = "";
        string receivePassword = "";
        string sendUser = "";
        string sendPassword = "";

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            RunRabbit();

            app.Run(async (context) =>
            {
                await context.Response.WriteAsync("Welcome to RabbitMQTester!");
            });
        }

        void RunRabbit(){
            if (isProducer) {
                var producerThread = new Thread(
                    () =>
                    {
                        Send.Start(Connection.Connect(sendUser,sendPassword));
                    });
                producerThread.IsBackground = true;
                producerThread.Start();
            }

            if (isConsumer) {
                var consumerThread = new Thread(
                    () =>
                    {
                        Receive.Start(Connection.Connect(receiveUser,receivePassword));
                    });
                consumerThread.IsBackground = true;
                consumerThread.Start();
                
            }

        }
    }

    class Connection {
        
        public static string MyQueue = "hello-world";

        public static IModel Connect(string username, string password){
            var factory = new ConnectionFactory();
            
            factory.HostName = "localhost";
            factory.Port = AmqpTcpEndpoint.UseDefaultPort;
            factory.VirtualHost = "";
            factory.UserName = username;
            factory.Password = password;

            return factory.CreateConnection().CreateModel();
        }
    }

    class Send {
        public static void Start(IModel model){
            while(true){
                model.BasicPublish(exchange: "",
                    routingKey: Connection.MyQueue,
                    basicProperties: null,
                    body: Encoding.UTF8.GetBytes(string.Format("Test message: '{0}'",DateTime.Now.ToString("u"))));

                Thread.Sleep(1000);
            }
        }
    }

    class Receive {
        public static void Start(IModel model){
            var consumer = new EventingBasicConsumer(model);
            consumer.Received += (m, ea) =>
            {
                Task.Run(() => {
                    var body = ea.Body;
                    var message = Encoding.UTF8.GetString(body);
                    Console.WriteLine("   Received message: '{0}'", message);
                    model.BasicAck(ea.DeliveryTag, false);
                });
            };
            model.BasicConsume(queue: Connection.MyQueue,
                                 noAck: true,
                                 consumer: consumer);
        }
    }

}
