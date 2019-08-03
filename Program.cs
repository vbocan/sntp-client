using System;

namespace ro.bocan.sntpclient
{
    class Program
    {
        const string Host = "0.pool.ntp.org";
        const int TimeOut = 5000;
        static void Main()
        {
            Console.WriteLine("SNTP Client v1.0");            
            Console.WriteLine("(C)2001-2019 Valer BOCAN, PhD <valer@bocan.ro>");
            Console.WriteLine();
            Console.WriteLine($"Connecting to {Host}...");
            Console.WriteLine();
            try
            {
                var client = new SNTPClient();
                client.Connect(Host, TimeOut);
                Console.WriteLine(client.ToString());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }
}
