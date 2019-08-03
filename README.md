# A C# SNTP Client
This is the very C# SNTP client used by Microsoft in .NET Micro Framework.

SNTPClient is a C# class designed to connect to time servers on the Internet and fetch the current date and time using the Network Time Protocol (NTP). The implementation of the protocol is based on the RFC 2030.

Historically, this has been the very first piece of C# code I've written back in 2000, apart from the traditional "Hello, world!".

Usage:
```
try
{
	// Build the SNTP client
	var client = new SNTPClient();
	// Connect to server and specify a timeout
	client.Connect("0.pool.ntp.org", 5000);
	// See connection results
	Console.WriteLine(client.ToString());
}
catch (Exception ex)
{
	Console.WriteLine($"Error: {ex.Message}");
}
```
A typical response is:

```
Connecting to 0.pool.ntp.org...
Leap indicator : No warning
Version number : 3
Mode : Server
Stratum : Secondary reference
Precision : 2.98023223876953E-08 s.
Poll interval : 1 s.
Reference ID : ftp.upcnet.ro (78.96.7.8)
Root delay : 31.0821533203125 ms.
Root dispersion : 18.6767578125 ms.
Round trip delay : 504.7738 ms.
Local clock offset : 2461.6131 ms.
Local time : 8/3/2019 11:23:18 AM
```
