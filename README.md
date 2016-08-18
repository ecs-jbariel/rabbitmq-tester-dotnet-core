# RabbitMQ tester for .NET Core
.NET Core Producer/Consumer for RabbitMQ.

# Usage
Clone the repo and ensure that you have the `dotnet` command available.  You can find it [inside the .NET Core SDK](https://www.microsoft.com/net/core)

Copy the [props.json.example](props.json.example) file to `props.json` and update the values as needed.

Run restore

`dotnet restore`

Run build (optional, as this is part of the `run` command)

`dotnet build`

Start the app

`dotnet run`

You should see the Rabbit Consumer/Producer output on the Console, and you can visit [http://localhost:5000](http://localhost:5000) to see the web server running.

# Issues
Please use the [Issues tab](../../issues) to report any problems or feature requests.
