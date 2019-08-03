/*
 * The C# SNTP client used by Microsoft in .NET Micro Framework
 * 
 * Copyright (C)2001-2019 Valer BOCAN, PhD <valer@bocan.ro>
 * Last modified: August 3rd, 2019
 * Historically, this has been the very first piece of C# code I've written.
 * 
 * Comments, bugs and suggestions are welcome.
 *
 * Update history:
 * 
 * August 3rd, 2019
 * - Removed SNTP_WindowsMobile compilation directive
 * - Fixed a few "obsolete" warnings by using TimeZoneInfo
 * - Removed Windows-specific ability to set the time of the local computer
 * - Various code enhancements, mostly cosmetic
 * 
 * November 20, 2011
 * - Added the SNTP_WindowsMobile compilation directive for discrimination between Windows Desktop and Windows Mobile
 * 
 * - Altered Connect() method to provide a socket timeout (Jason Garrett - jason.garrett@hotmail.com)
 *    - Credit goes to Kyle Jones who posted this improved Connect() method on
 *      http://objectmix.com/dotnet/98919-socket-receive-timeout-compact-framework.html
 *      on 10 Mar 2009.
 * - Added <summary> tags to class methods and attributes
 * 
 * May 2, 2011
 * - RoundTripDelay and LocalClockOffset now return a double instead of an integer to avoid overflows
 *   when the computer clock is way off.
 * - Added the DllImport directive for Windows Mobile 6.0
 *   Thanks to Andre Rippstein <andre@rippstein.net>
 * 
 * September 20, 2003
 * - Renamed the class from NTPClient to SNTPClient.
 * - Fixed the RoundTripDelay and LocalClockOffset properties.
 *   Thanks go to DNH <dnharris@csrlink.net>.
 * - Fixed the PollInterval property.
 *   Thanks go to Jim Hollenhorst <hollenho@attbi.com>.
 * - Changed the ReceptionTimestamp variable to DestinationTimestamp to follow the standard
 *   more closely.
 * - Precision property is now shown is seconds rather than milliseconds in the
 *   ToString method.
 * 
 * May 28, 2002
 * - Fixed a bug in the Precision property and the SetTime function.
 *   Thanks go to Jim Hollenhorst <hollenho@attbi.com>.
 * 
 * March 14, 2001
 * - First public release.
 */

using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace ro.bocan.sntpclient
{    
    /// <summary>
    /// SNTPClient is a C# class designed to connect to time servers on the Internet and
    /// fetch the current date and time. The implementation of the protocol is based on the RFC 2030.
    /// 
    /// Public class members:
    ///
    /// LeapIndicator - Warns of an impending leap second to be inserted/deleted in the last
    /// minute of the current day. (See the _LeapIndicator enum)
    /// 
    /// VersionNumber - Version number of the protocol (3 or 4).
    /// 
    /// Mode - Returns mode. (See the _Mode enum)
    /// 
    /// Stratum - Stratum of the clock. (See the _Stratum enum)
    /// 
    /// PollInterval - Maximum interval between successive messages
    /// 
    /// Precision - Precision of the clock
    /// 
    /// RootDelay - Round trip time to the primary reference source.
    /// 
    /// RootDispersion - Nominal error relative to the primary reference source.
    /// 
    /// ReferenceID - Reference identifier (either a 4 character string or an IP address).
    /// 
    /// ReferenceTimestamp - The time at which the clock was last set or corrected.
    /// 
    /// OriginateTimestamp - The time at which the request departed the client for the server.
    /// 
    /// ReceiveTimestamp - The time at which the request arrived at the server.
    /// 
    /// Transmit Timestamp - The time at which the reply departed the server for client.
    /// 
    /// RoundTripDelay - The time between the departure of request and arrival of reply.
    /// 
    /// LocalClockOffset - The offset of the local clock relative to the primary reference
    /// source.
    /// 
    /// Initialize - Sets up data structure and prepares for connection.
    /// 
    /// Connect - Connects to the time server and populates the data structure.
    ///	It can also update the system time.
    /// 
    /// ToString - Returns a string representation of the object.
    /// 
    /// -----------------------------------------------------------------------------
    /// Structure of the standard NTP header (as described in RFC 2030)
    ///                       1                   2                   3
    ///   0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
    ///  +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    ///  |LI | VN  |Mode |    Stratum    |     Poll      |   Precision   |
    ///  +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    ///  |                          Root Delay                           |
    ///  +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    ///  |                       Root Dispersion                         |
    ///  +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    ///  |                     Reference Identifier                      |
    ///  +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    ///  |                                                               |
    ///  |                   Reference Timestamp (64)                    |
    ///  |                                                               |
    ///  +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    ///  |                                                               |
    ///  |                   Originate Timestamp (64)                    |
    ///  |                                                               |
    ///  +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    ///  |                                                               |
    ///  |                    Receive Timestamp (64)                     |
    ///  |                                                               |
    ///  +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    ///  |                                                               |
    ///  |                    Transmit Timestamp (64)                    |
    ///  |                                                               |
    ///  +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    ///  |                 Key Identifier (optional) (32)                |
    ///  +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    ///  |                                                               |
    ///  |                                                               |
    ///  |                 Message Digest (optional) (128)               |
    ///  |                                                               |
    ///  |                                                               |
    ///  +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    /// 
    /// -----------------------------------------------------------------------------
    /// 
    /// SNTP Timestamp Format (as described in RFC 2030)
    ///                         1                   2                   3
    ///     0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
    /// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    /// |                           Seconds                             |
    /// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    /// |                  Seconds Fraction (0-padded)                  |
    /// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    /// 
    /// </summary>

    public class SNTPClient
    {
        #region Private stuff
        // SNTP Data Structure Length
        private const byte SNTPDataLength = 48;

        // SNTP Data Structure (as described in RFC 2030)
        private readonly byte[] SNTPData = new byte[SNTPDataLength];

        // Offset constants for timestamps in the data structure
        private const byte offReferenceID = 12;
        private const byte offReferenceTimestamp = 16;
        private const byte offOriginateTimestamp = 24;
        private const byte offReceiveTimestamp = 32;
        private const byte offTransmitTimestamp = 40;
        #endregion

        #region Public accessors
        /// <summary>
        /// Warns of an impending leap second to be inserted/deleted in the last
        /// minute of the current day. (See the _LeapIndicator enum)
        /// </summary>
        public LeapIndicator LeapIndicator
        {
            get
            {
                // Isolate the two most significant bits
                byte val = (byte)(SNTPData[0] >> 6);
                switch (val)
                {
                    case 0: return LeapIndicator.NoWarning;
                    case 1: return LeapIndicator.LastMinute61;
                    case 2: return LeapIndicator.LastMinute59;
                    case 3: goto default;
                    default:
                        return LeapIndicator.Alarm;
                }
            }
        }

        /// <summary>
        /// Version number of the protocol (3 or 4).
        /// </summary>
        public byte VersionNumber
        {
            get
            {
                // Isolate bits 3 - 5
                byte val = (byte)((SNTPData[0] & 0x38) >> 3);
                return val;
            }
        }

        /// <summary>
        /// Returns mode. (See the _Mode enum)
        /// </summary>
        public Mode Mode
        {
            get
            {
                // Isolate bits 0 - 3
                byte val = (byte)(SNTPData[0] & 0x7);
                switch (val)
                {
                    case 0: goto default;
                    case 6: goto default;
                    case 7: goto default;
                    default:
                        return Mode.Unknown;
                    case 1:
                        return Mode.SymmetricActive;
                    case 2:
                        return Mode.SymmetricPassive;
                    case 3:
                        return Mode.Client;
                    case 4:
                        return Mode.Server;
                    case 5:
                        return Mode.Broadcast;
                }
            }
        }

        /// <summary>
        /// Stratum of the clock. (See the _Stratum enum)
        /// </summary>
        public Stratum Stratum
        {
            get
            {
                byte val = (byte)SNTPData[1];
                if (val == 0) return Stratum.Unspecified;
                else
                    if (val == 1) return Stratum.PrimaryReference;
                    else
                        if (val <= 15) return Stratum.SecondaryReference;
                        else
                            return Stratum.Reserved;
            }
        }

        /// <summary>
        /// Maximum interval (seconds) between successive messages
        /// </summary>
        public uint PollInterval
        {
            get
            {
                // Thanks to Jim Hollenhorst <hollenho@attbi.com>
                return (uint)(Math.Pow(2, (sbyte)SNTPData[2]));
            }
        }

        /// <summary>
        /// Precision (in seconds) of the clock
        /// </summary>
        public double Precision
        {
            get
            {
                // Thanks to Jim Hollenhorst <hollenho@attbi.com>
                return (Math.Pow(2, (sbyte)SNTPData[3]));
            }
        }

        /// <summary>
        /// Round trip time (in milliseconds) to the primary reference source.
        /// </summary>
        public double RootDelay
        {
            get
            {
                int temp = 0;
                temp = 256 * (256 * (256 * SNTPData[4] + SNTPData[5]) + SNTPData[6]) + SNTPData[7];
                return 1000 * (((double)temp) / 0x10000);
            }
        }

        /// <summary>
        /// Nominal error (in milliseconds) relative to the primary reference source.
        /// </summary>
        public double RootDispersion
        {
            get
            {
                int temp = 0;
                temp = 256 * (256 * (256 * SNTPData[8] + SNTPData[9]) + SNTPData[10]) + SNTPData[11];
                return 1000 * (((double)temp) / 0x10000);
            }
        }

        /// <summary>
        /// Reference identifier (either a 4 character string or an IP address)
        /// </summary>
        public string ReferenceID
        {
            get
            {
                string val = "";
                switch (Stratum)
                {
                    case Stratum.Unspecified:
                        goto case Stratum.PrimaryReference;
                    case Stratum.PrimaryReference:
                        val += (char)SNTPData[offReferenceID + 0];
                        val += (char)SNTPData[offReferenceID + 1];
                        val += (char)SNTPData[offReferenceID + 2];
                        val += (char)SNTPData[offReferenceID + 3];
                        break;
                    case Stratum.SecondaryReference:
                        switch (VersionNumber)
                        {
                            case 3:	// Version 3, Reference ID is an IPv4 address
                                string Address = SNTPData[offReferenceID + 0].ToString() + "." +
                                                 SNTPData[offReferenceID + 1].ToString() + "." +
                                                 SNTPData[offReferenceID + 2].ToString() + "." +
                                                 SNTPData[offReferenceID + 3].ToString();
                                try
                                {
                                    IPHostEntry Host = Dns.GetHostEntry(Address);
                                    val = Host.HostName + " (" + Address + ")";
                                }
                                catch (Exception)
                                {
                                    val = "N/A";
                                }
                                break;
                            case 4: // Version 4, Reference ID is the timestamp of last update
                                DateTime time = ComputeDate(GetMilliSeconds(offReferenceID));
                                // Take care of the time zone                                
                                TimeSpan offspan = TimeZoneInfo.Local.GetUtcOffset(DateTime.UtcNow);
                                val = (time + offspan).ToString();
                                break;
                            default:
                                val = "N/A";
                                break;
                        }
                        break;
                }

                return val;
            }
        }

        /// <summary>
        /// The time at which the clock was last set or corrected
        /// </summary>
        public DateTime ReferenceTimestamp
        {
            get
            {
                DateTime time = ComputeDate(GetMilliSeconds(offReferenceTimestamp));
                // Take care of the time zone
                TimeSpan offspan = TimeZoneInfo.Local.GetUtcOffset(DateTime.UtcNow);
                return time + offspan;
            }
        }

        /// <summary>
        /// The time (T1) at which the request departed the client for the server
        /// </summary>
        public DateTime OriginateTimestamp
        {
            get
            {
                return ComputeDate(GetMilliSeconds(offOriginateTimestamp));
            }
        }

        /// <summary>
        /// The time (T2) at which the request arrived at the server
        /// </summary>
        public DateTime ReceiveTimestamp
        {
            get
            {
                DateTime time = ComputeDate(GetMilliSeconds(offReceiveTimestamp));
                // Take care of the time zone
                TimeSpan offspan = TimeZoneInfo.Local.GetUtcOffset(DateTime.UtcNow);
                return time + offspan;
            }
        }

        /// <summary>
        /// The time (T3) at which the reply departed the server for client
        /// </summary>
        public DateTime TransmitTimestamp
        {
            get
            {
                DateTime time = ComputeDate(GetMilliSeconds(offTransmitTimestamp));
                // Take care of the time zone
                TimeSpan offspan = TimeZoneInfo.Local.GetUtcOffset(DateTime.UtcNow);
                return time + offspan;
            }
            set
            {
                SetDate(offTransmitTimestamp, value);
            }
        }

        /// <summary>
        /// Destination Timestamp (T4)
        /// </summary>
        public DateTime DestinationTimestamp;

        /// <summary>
        /// The time (in milliseconds) between the departure of request and arrival of reply 
        /// </summary>
        public double RoundTripDelay
        {
            get
            {
                // Thanks to DNH <dnharris@csrlink.net>
                TimeSpan span = (DestinationTimestamp - OriginateTimestamp) - (ReceiveTimestamp - TransmitTimestamp);
                return span.TotalMilliseconds;
            }
        }

        /// <summary>
        /// The offset (in milliseconds) of the local clock relative to the primary reference source
        /// </summary>
        public double LocalClockOffset
        {
            get
            {
                // Thanks to DNH <dnharris@csrlink.net>
                TimeSpan span = (ReceiveTimestamp - OriginateTimestamp) + (TransmitTimestamp - DestinationTimestamp);
                return (span.TotalMilliseconds / 2);
            }
        }
        #endregion

        #region Helpers
        /// <summary>
        /// Compute date, given the number of milliseconds since January 1, 1900
        /// </summary>
        private DateTime ComputeDate(ulong milliseconds)
        {
            TimeSpan span = TimeSpan.FromMilliseconds((double)milliseconds);
            DateTime time = new DateTime(1900, 1, 1);
            time += span;
            return time;
        }

        /// <summary>
        /// Compute the number of milliseconds, given the offset of a 8-byte array
        /// </summary>
        private ulong GetMilliSeconds(byte offset)
        {
            ulong intpart = 0, fractpart = 0;

            for (int i = 0; i <= 3; i++)
            {
                intpart = 256 * intpart + SNTPData[offset + i];
            }
            for (int i = 4; i <= 7; i++)
            {
                fractpart = 256 * fractpart + SNTPData[offset + i];
            }
            ulong milliseconds = intpart * 1000 + (fractpart * 1000) / 0x100000000L;
            return milliseconds;
        }

        /// <summary>
        /// Set the date part of the SNTP data
        /// </summary>
        /// <param name="offset">Offset at which the date part of the SNTP data is</param>
        /// <param name="date">The date</param>
        private void SetDate(byte offset, DateTime date)
        {
            ulong intpart = 0, fractpart = 0;
            DateTime StartOfCentury = new DateTime(1900, 1, 1, 0, 0, 0);	// January 1, 1900 12:00 AM

            ulong milliseconds = (ulong)(date - StartOfCentury).TotalMilliseconds;
            intpart = milliseconds / 1000;
            fractpart = ((milliseconds % 1000) * 0x100000000L) / 1000;

            ulong temp = intpart;
            for (int i = 3; i >= 0; i--)
            {
                SNTPData[offset + i] = (byte)(temp % 256);
                temp = temp / 256;
            }

            temp = fractpart;
            for (int i = 7; i >= 4; i--)
            {
                SNTPData[offset + i] = (byte)(temp % 256);
                temp = temp / 256;
            }
        }

        /// <summary>
        /// Returns true if received data is valid and if comes from a NTP-compliant time server.
        /// </summary>
        private bool IsResponseValid()
        {
            if (SNTPData.Length < SNTPDataLength || Mode != Mode.Server)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        /// <summary>
        /// Initialize the SNTP client data. Sets up data structure and prepares for connection.
        /// </summary>
        private void Initialize()
        {
            // Set version number to 4 and Mode to 3 (client)
            SNTPData[0] = 0x1B;
            // Initialize all other fields with 0
            for (int i = 1; i < 48; i++)
            {
                SNTPData[i] = 0;
            }
            // Initialize the transmit timestamp
            TransmitTimestamp = GetCurrentTime();
        }

        private DateTime GetCurrentTime() => DateTime.Now;

        #endregion

        #region Public methods
        /// <summary>
        /// Connects to the time server and populates the data structure.
        ///	It can also update the system time.
        /// </summary>
        /// <param name="Host">Address of the NTP server.</param>
        /// <param name="TimeOut">Time in milliseconds after which the method returns.</param>        
        public void Connect(string Host, int TimeOut)
        {
            try
            {
                IPEndPoint listenEP = new IPEndPoint(IPAddress.Any, 123);
                Socket sendSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                IPHostEntry hostEntry = Dns.GetHostEntry(Host);
                IPEndPoint sendEP = new IPEndPoint(hostEntry.AddressList[0], 123);
                EndPoint epSendEP = (EndPoint)sendEP;

                int messageLength = 0;
                try
                {
                    sendSocket.Bind(listenEP);
                    Initialize();

                    bool messageReceived = false;
                    int elapsedTime = 0;

                    // Timeout code
                    while (!messageReceived && (elapsedTime < TimeOut))
                    {
                        sendSocket.SendTo(SNTPData, SNTPData.Length, SocketFlags.None, sendEP);
                        // Check if data has been received by the listening socket and is available to be read
                        if (sendSocket.Available > 0)
                        {
                            messageLength = sendSocket.ReceiveFrom(SNTPData, ref epSendEP);
                            if (!IsResponseValid())
                            {
                                throw new Exception($"Host sent an invalid response.");
                            }
                            messageReceived = true;
                            break;
                        }
                        // Wait a bit
                        Thread.Sleep(500);
                        elapsedTime += 500;
                    }
                    if (!messageReceived)
                    {
                        throw new TimeoutException($"Host did not respond.");
                    }
                }
                catch (SocketException e)
                {
                    throw new Exception(e.Message);
                }
                finally
                {
                    sendSocket.Close();
                }

                DestinationTimestamp = GetCurrentTime();
            }
            catch (SocketException e)
            {
                throw new Exception(e.Message);
            }            
        }        

        /// <summary>
        /// Returns a string representation of the object
        /// </summary>
        public override string ToString()
        {
            var str = new StringBuilder();

            str.Append("Leap indicator     : ");
            switch (LeapIndicator)
            {
                case LeapIndicator.NoWarning:
                    str.AppendLine("No warning");
                    break;
                case LeapIndicator.LastMinute61:
                    str.AppendLine("Last minute has 61 seconds");
                    break;
                case LeapIndicator.LastMinute59:
                    str.AppendLine("Last minute has 59 seconds");
                    break;
                case LeapIndicator.Alarm:
                    str.AppendLine("Alarm Condition (clock not synchronized)");
                    break;
            }
            str.AppendLine($"Version number     : {VersionNumber}");
            str.Append("Mode               : ");
            switch (Mode)
            {
                case Mode.Unknown:
                    str.AppendLine("Unknown");
                    break;
                case Mode.SymmetricActive:
                    str.AppendLine("Symmetric Active");
                    break;
                case Mode.SymmetricPassive:
                    str.AppendLine("Symmetric Pasive");
                    break;
                case Mode.Client:
                    str.AppendLine("Client");
                    break;
                case Mode.Server:
                    str.AppendLine("Server");
                    break;
                case Mode.Broadcast:
                    str.AppendLine("Broadcast");
                    break;
            }
            str.Append("Stratum            : ");
            switch (Stratum)
            {
                case Stratum.Unspecified:
                case Stratum.Reserved:
                    str.AppendLine("Unspecified");
                    break;
                case Stratum.PrimaryReference:
                    str.AppendLine("Primary reference");
                    break;
                case Stratum.SecondaryReference:
                    str.AppendLine("Secondary reference");
                    break;
            }

            str.AppendLine($"Precision          : {Precision} s.");
            str.AppendLine($"Poll interval      : {PollInterval} s.");
            str.AppendLine($"Reference ID       : {ReferenceID}");
            str.AppendLine($"Root delay         : {RootDelay} ms.");
            str.AppendLine($"Root dispersion    : {RootDispersion} ms.");
            str.AppendLine($"Round trip delay   : {RoundTripDelay} ms.");
            str.AppendLine($"Local clock offset : {LocalClockOffset} ms.");
            str.AppendLine($"Local time         : {GetCurrentTime().AddMilliseconds(LocalClockOffset)}");
            str.AppendLine();

            return str.ToString();
        }
        #endregion
    }
}
