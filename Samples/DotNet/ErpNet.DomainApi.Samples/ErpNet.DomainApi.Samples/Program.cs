using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;

namespace ErpNet.DomainApi.Samples
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                // Ignore SSL certificate errors
                ServicePointManager.ServerCertificateValidationCallback +=
                    (sender, cert, chain, sslPolicyErrors) => true;
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

                Console.WriteLine("Hello");
                Console.WriteLine("Enter odata service root uri):");
                Console.WriteLine("For example: https://mycompany.com/api/domain/odata/");
                var serviceRoot = new Uri(Console.ReadLine());
                Console.WriteLine("Enter user name:");
                var userName = Console.ReadLine();
                Console.WriteLine("Enter password:");
                var password = ReadPassword();

                while (true)
                {
                    Console.WriteLine();
                    Console.WriteLine("Type a sample number and hit Enter to execute it. Type 'exit' to exit the program.");

                    int i = 1;
                    List<string> methods = new List<string>();
                    foreach (var mi in typeof(Samples).GetTypeInfo().DeclaredMethods)
                    {
                        methods.Add(mi.Name);
                        Console.WriteLine("{0}: {1}", i++, mi.Name);
                    }

                    var line = Console.ReadLine();
                    if (line == "exit")
                        return;
                    int k = int.Parse(line);


                    Task.Run(async () => await ExecuteSamples(methods[k - 1], serviceRoot, userName, password)).Wait();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR");
                Console.WriteLine(ex.Message);
            }
            Console.WriteLine("Type any key to exit");
            Console.ReadKey();

        }

        static async Task ExecuteSamples(string sampleName, Uri root, string user, string pass)
        {
            var credentials = new ErpCredentials("Samples", user, pass, "en");

            Console.WriteLine("Logging in...");
            using (var session = new ErpSession(root))
            {
                await session.LoginAsync(credentials);

                // Omit null values from JSON result.
                session.RequestOptions.SkipNulls = true;

                await ExecuteSample(sampleName, session);
            }


        }

        static async Task ExecuteSample(string sampleName, ErpSession session)
        {
            try
            {
                Console.WriteLine($"Executing {sampleName}...");

                var m = typeof(Samples).GetMethod(sampleName,
                    BindingFlags.Public
                    | BindingFlags.InvokeMethod
                    | BindingFlags.Static
                    | BindingFlags.IgnoreCase);
                if (m == null)
                    throw new Exception($"Sample '{sampleName}' not found.");

                var task = ((Task)m.Invoke(null, new object[] { session }));
                await task;

                Console.WriteLine($"{sampleName} done.");
            }
            catch (Exception ex)
            {
                DisplayError(ex);
            }
        }

        static void DisplayError(Exception ex)
        {
            if (ex is AggregateException ex1)
            {
                foreach (var iex in ex1.InnerExceptions)
                    DisplayError(iex);
            }
            else if (ex is Simple.OData.Client.WebRequestException ex2)
            {
                Console.WriteLine(ex2.Message);
                try
                {
                    var json = JObject.Parse(ex2.Response);
                    Console.WriteLine(json["error"]["message"]);
                    //Console.Write(json.ToString(Newtonsoft.Json.Formatting.Indented));
                }
                catch
                {
                    Console.WriteLine(ex2.Response);
                }
            }
            else
            {
                Console.WriteLine(ex.ToString());
            }
        }

        public static string ReadPassword()
        {
            string password = "";
            while (true)
            {
                ConsoleKeyInfo key = Console.ReadKey(true);
                switch (key.Key)
                {
                    case ConsoleKey.Escape:
                        return null;
                    case ConsoleKey.Enter:
                        return password;
                    case ConsoleKey.Backspace:
                        password = password.Substring(0, (password.Length - 1));
                        Console.Write("\b \b");
                        break;
                    default:
                        password += key.KeyChar;
                        Console.Write("*");
                        break;
                }
            }
        }
    }
}
