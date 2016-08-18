using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.IO;
using System.Text;
using System.Threading;

namespace RabbitMQTester
{
    public class Program
    {
        static bool isProducer = true;
        static bool isConsumer = true;

        static Thread ProducerThread;
        static Thread ConsumerThread;
        static Thread AppThread;
        static IWebHost WebHost;

        public static void Main(string[] args)
        {
            var config = new ConfigurationBuilder().AddCommandLine(args).AddEnvironmentVariables(prefix: "ASPNETCORE_").Build();

            WebHost = new WebHostBuilder().UseConfiguration(config).UseKestrel().UseContentRoot(Directory.GetCurrentDirectory())
                .UseIISIntegration().UseStartup<Startup>().Build();

            AppThread = new Thread(() => { WebHost.Run(); });
            AppThread.IsBackground = true;
            AppThread.Start();

            if (isProducer)
            {
                ProducerThread = new Thread(() => { Send.Start(); });
                ProducerThread.IsBackground = true;
                ProducerThread.Start();
            }

            if (isConsumer)
            {
                ConsumerThread = new Thread(() => { Receive.Start(); });
                ConsumerThread.IsBackground = true;
                ConsumerThread.Start();
            }

            while (Console.ReadKey().Key != ConsoleKey.Enter) { }

            // should probably clean up threads here...
            Send.Stop();
            Receive.Stop();
            WebHost.Dispose();
            AppThread.Join();

            Environment.Exit(0);
        }
    }

    class Connection
    {

        static string HostName = "localhost";
        static int Port = AmqpTcpEndpoint.UseDefaultPort;
        static string VirtualHost = "";
        public static string MyQueue = "";
        public static string ReceiveUser = "";
        public static string ReceivePassword = "";
        public static string SendUser = "";
        public static string SendPassword = "";
        
        public static IModel Connect(string username, string password)
        {
            var factory = new ConnectionFactory();

            factory.HostName = HostName;
            factory.Port = Port;
            factory.VirtualHost = VirtualHost;
            factory.UserName = username;
            factory.Password = password;

            return factory.CreateConnection().CreateModel();
        }
    }

    class Send
    {
        static Timer SendTimer;
        static IModel Channel;

        public static void Start()
        {
            Console.WriteLine("Starting Producer...");
            Channel = Connection.Connect(Connection.SendUser, Connection.SendPassword);
            SendTimer = new Timer((args) =>
            {
                Console.WriteLine("Publishing...");
                Channel.BasicPublish(exchange: "",
                    routingKey: Connection.MyQueue,
                    basicProperties: null,
                    body: Encoding.UTF8.GetBytes(string.Format("Test message: '{0}'", DateTime.Now.ToString("u"))));
            }, null, 0, 1000);

        }

        public static void Stop()
        {
            if (null != SendTimer) SendTimer.Dispose();
            if (null != Channel) Channel.Dispose();
        }
    }

    class Receive
    {
        static Timer ReceiveTimer;
        static IModel Channel;

        public static void Start()
        {
            Console.WriteLine("Starting Consumer...");
            Channel = Connection.Connect(Connection.ReceiveUser, Connection.ReceivePassword);
            //ReceiveTimer = new Timer((args) => { Console.WriteLine("Consuming..."); }, null, 0, 1000);
            var consumer = new EventingBasicConsumer(Channel);
            consumer.Received += (m, ea) =>
            {
                Console.WriteLine("   Received message: '{0}'", Encoding.UTF8.GetString(ea.Body));
            };
            Channel.BasicConsume(queue: Connection.MyQueue, noAck: true, consumer: consumer);
        }

        public static void Stop()
        {
            if (null != ReceiveTimer) ReceiveTimer.Dispose();
            if (null != Channel) Channel.Dispose();
        }
    }
}
