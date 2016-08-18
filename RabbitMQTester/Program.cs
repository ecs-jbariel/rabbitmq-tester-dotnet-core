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
        public static string HostName;
        public static int Port;
        public static string VirtualHost;
        public static string MyQueue;
        public static string ReceiveUser;
        public static string ReceivePassword;
        public static string SendUser;
        public static string SendPassword;

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

            // chain calls together as a fluent API
            IConfigurationSection rabbitConfig = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("props.json").Build().GetSection("RabbitMQ");
            Program.HostName = rabbitConfig["host"];
            Program.Port = Int32.Parse(rabbitConfig["port"]);
            Program.VirtualHost = rabbitConfig["vHost"];
            Program.MyQueue = rabbitConfig["queue"];
            Program.SendUser = rabbitConfig["producerUsername"];
            Program.SendPassword = rabbitConfig["producerPassword"];
            Program.ReceiveUser = rabbitConfig["consumerUsername"];
            Program.ReceivePassword = rabbitConfig["consumerPassword"];

            if (Boolean.Parse(rabbitConfig["producer"]))
            {
                ProducerThread = new Thread(() => { Send.Start(); });
                ProducerThread.IsBackground = true;
                ProducerThread.Start();
            }

            if (Boolean.Parse(rabbitConfig["consumer"]))
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

        public static IModel Connect(string username, string password)
        {
            var factory = new ConnectionFactory();

            factory.HostName = Program.HostName;
            factory.Port = Program.Port;
            factory.VirtualHost = Program.VirtualHost;
            factory.UserName = username;
            factory.Password = password;

            //Console.WriteLine("Logging into: '{0}:{1}/{2}' with username: '{3}' and password '{4}'", Program.HostName, Program.Port, Program.VirtualHost, username, password);

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
            Channel = Connection.Connect(Program.SendUser, Program.SendPassword);
            SendTimer = new Timer((args) =>
            {
                Console.WriteLine("Publishing...");
                Channel.BasicPublish(exchange: "",
                    routingKey: Program.MyQueue,
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
            Channel = Connection.Connect(Program.ReceiveUser, Program.ReceivePassword);
            //ReceiveTimer = new Timer((args) => { Console.WriteLine("Consuming..."); }, null, 0, 1000);
            var consumer = new EventingBasicConsumer(Channel);
            consumer.Received += (m, ea) =>
            {
                Console.WriteLine("   Received message: '{0}'", Encoding.UTF8.GetString(ea.Body));
            };
            Channel.BasicConsume(queue: Program.MyQueue, noAck: true, consumer: consumer);
        }

        public static void Stop()
        {
            if (null != ReceiveTimer) ReceiveTimer.Dispose();
            if (null != Channel) Channel.Dispose();
        }
    }
}
