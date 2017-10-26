using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using Microsoft.TemplateEngine.Core.Contracts;

namespace Microsoft.TemplateEngine.Orchestrator.RunnableProjects.Macros.Config
{
    public class GeneratePortNumberConfig : IMacroConfig
    {
        public string DataType { get; }

        public string VariableName { get; }

        public string Type => "port";

        public Socket Socket { get; }

        public int Port { get; }

        public int Low { get; }

        public int High { get; }

        private static readonly HashSet<int> UnsafePorts = new HashSet<int>() {
                    2049, // nfs
                    3659, // apple-sasl / PasswordServer
                    4045, // lockd
                    6000, // X11
                    6665, // Alternate IRC [Apple addition]
                    6666, // Alternate IRC [Apple addition]
                    6667, // Standard IRC [Apple addition]
                    6668, // Alternate IRC [Apple addition]
                    6669, // Alternate IRC [Apple addition]
        };

        public GeneratePortNumberConfig(string variableName, string dataType, int fallback, int low, int high)
        {
            DataType = dataType;
            VariableName = variableName;
            Random rand = new Random();
            int startPort = rand.Next(low, high);

            for (int testPort = startPort; testPort <= high; testPort++)
            {
                if (TryAllocatePort(testPort, out Socket testSocket))
                {
                    Socket = testSocket;
                    Port = ((IPEndPoint)Socket.LocalEndPoint).Port;
                    return;
                }
            }

            for (int testPort = low; testPort < startPort; testPort++)
            {
                if (TryAllocatePort(testPort, out Socket testSocket))
                {
                    Socket = testSocket;
                    Port = ((IPEndPoint)Socket.LocalEndPoint).Port;
                    return;
                }
            }

            Port = fallback;
        }

        private bool TryAllocatePort(int testPort, out Socket testSocket)
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
