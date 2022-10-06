// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using Microsoft.TemplateEngine.Orchestrator.RunnableProjects.Abstractions;

namespace Microsoft.TemplateEngine.Orchestrator.RunnableProjects.Macros.Config
{
    internal class GeneratePortNumberConfig : IMacroConfig
    {
        private static readonly HashSet<int> UnsafePorts = new HashSet<int>()
        {
            1, // tcpmux
            7, // echo
            9, // discard
            11, // systat
            13, // daytime
            15, // netstat
            17, // qotd
            19, // chargen
            20, // FTP-data
            21, // FTP-control
            22, // SSH
            23, // telnet
            25, // SMTP
            37, // time
            42, // name
            43, // nicname
            53, // domain
            69, // TFTP
            77, // priv-rjs
            79, // finger
            87, // ttylink
            95, // supdup
            101, // hostriame
            102, // iso-tsap
            103, // gppitnp
            104, // acr-nema
            109, // POP2
            110, // POP3
            111, // sunrpc
            113, // auth
            115, // SFTP
            117, // uucp-path
            119, // nntp
            123, // NTP
            135, // loc-srv / epmap
            137, // NetBIOS
            139, // netbios
            143, // IMAP2
            161, // SNMP
            179, // BGP
            389, // LDAP
            427, // SLP (Also used by Apple Filing Protocol)
            465, // SMTP+SSL
            512, // print / exec
            513, // login
            514, // shell
            515, // printer
            526, // tempo
            530, // courier
            531, // Chat
            532, // netnews
            540, // UUCP
            548, // afpovertcp [Apple addition]
            554, // rtsp
            556, // remotefs
            563, // NNTP+SSL
            587, // ESMTP
            601, // syslog-conn
            636, // LDAP+SSL
            989, // ftps-data
            990, // ftps
            993, // IMAP+SSL
            995, // POP3+SSL
            1719, // H323 (RAS)
            1720, // H323 (Q931)
            1723, // H323 (H245)
            2049, // NFS
            3659, // apple-sasl / PasswordServer [Apple addition]
            4045, // lockd
            5060, // SIP
            5061, // SIPS
            6000, // X11
            6566, // SANE
            6665, // Alternate IRC [Apple addition]
            6666, // Alternate IRC [Apple addition]
            6667, // Standard IRC [Apple addition]
            6668, // Alternate IRC [Apple addition]
            6669, // Alternate IRC [Apple addition]
            6697, // IRC+SSL [Apple addition]
            10080, // amanda
            4190, // ManageSieve [Apple addition]
            6679, // Alternate IRC SSL [Apple addition]
        };

        internal GeneratePortNumberConfig(string variableName, string? dataType, int fallback, int low, int high)
        {
            DataType = dataType;
            VariableName = variableName;
            int startPort = CryptoRandom.NextInt(low, high);

            for (int testPort = startPort; testPort <= high; testPort++)
            {
                if (TryAllocatePort(testPort, out Socket? testSocket))
                {
                    Socket = testSocket;
                    Port = ((IPEndPoint)Socket!.LocalEndPoint).Port;
                    return;
                }
            }

            for (int testPort = low; testPort < startPort; testPort++)
            {
                if (TryAllocatePort(testPort, out Socket? testSocket))
                {
                    Socket = testSocket;
                    Port = ((IPEndPoint)Socket!.LocalEndPoint).Port;
                    return;
                }
            }

            Port = fallback;
        }

        public string VariableName { get; }

        public string Type => "port";

        internal string? DataType { get; }

        internal Socket? Socket { get; }

        internal int Port { get; }

        internal int Low { get; }

        internal int High { get; }

        private bool TryAllocatePort(int testPort, out Socket? testSocket)
        {
            testSocket = null;

            if (UnsafePorts.Contains(testPort))
            {
                return false;
            }

            try
            {
                if (Socket.OSSupportsIPv4)
                {
                    testSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                }
                else if (Socket.OSSupportsIPv6)
                {
                    testSocket = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp);
                }
            }
            catch
            {
                testSocket?.Dispose();
                return false;
            }

            if (testSocket != null)
            {
                IPEndPoint endPoint = new IPEndPoint(testSocket.AddressFamily == AddressFamily.InterNetworkV6 ? IPAddress.IPv6Any : IPAddress.Any, testPort);

                try
                {
                    testSocket.Bind(endPoint);
                    return true;
                }
                catch
                {
                    testSocket?.Dispose();
                    return false;
                }
            }

            testSocket = null;
            return false;
        }
    }
}
