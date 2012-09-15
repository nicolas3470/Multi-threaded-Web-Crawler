using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using Isis;

namespace AzureConsole
{
    public class UdpState
    {
        public IPEndPoint e;
        public UdpClient u;
    }

    class Program
    {
        static IPEndPoint local = new IPEndPoint(IPAddress.Any, 10080);
        static IPEndPoint local_ = new IPEndPoint(IPAddress.Any, 10081);
        static IPEndPoint web = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 10090);
        static IPEndPoint web_;
        static UdpClient sender = null;
        static UdpClient receiver = null;
        static UdpState ss = null;
        static UdpState rs = null;
        static bool sending = false;
        static bool monitoring = false;
        static Thread monitor = null;
        static Group smallGroup = null;
        static List<int> rankList = new List<int>();
        static string bootstrap = "";

        static void Main(string[] args)
        {
            try
            {
                if (args.Length == 3)
                {
                    int setlocal = int.Parse(args[0]);
                    int setlocal_ = int.Parse(args[1]);
                    local.Port = setlocal;
                    local_.Port = setlocal_;
                    Console.WriteLine("Local port has been set to " + setlocal + " and " + setlocal_);
                    bootstrap = args[2];
                }
                else if (args.Length == 1)
                {
                    bootstrap = args[0];
                }

                // Set up local UDP communication with web front
                string ip = NetSetup();

                // TODO: add logic to retrieve oracle information from bootstrap server

                // Set up runtime environments
                Environment.SetEnvironmentVariable("ISIS_TCP_ONLY", "true");
                

                IsisSystem.Start();
                Console.WriteLine("IsisSystem started");
                smallGroup = new SmallGroup("Azure Group");
                smallGroup.Join();
                Console.WriteLine("Azure group joined");
                Thread.Sleep(15 * 1000);                

                IsisSystem.WaitForever();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        static string NetSetup()
        {
            receiver = new UdpClient(local);
            sender = new UdpClient(local_);

            ss = new UdpState();
            ss.e = local;
            ss.u = sender;

            rs = new UdpState();
            rs.e = local;
            rs.u = receiver;

            Console.WriteLine("Network for web started");

            try
            {
                receiver.BeginReceive(new AsyncCallback(OnReceive), rs);
            }
            catch (SocketException e)
            {
                Console.WriteLine(e.Message);
            }

            IPHostEntry host;
            string localIP = "?";
            host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (IPAddress ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    localIP = ip.ToString();
                }
            }
            return localIP;
        }

        static void MonitorStart()
        {
            if (monitor == null)
            {
                monitor = new Thread(delegate()
                    {
                        while (true)
                        {
                            if (!sending)
                            {
                                string state = IsisSystem.GetMyAddress().ToString() + " " + IsisSystem.NOW();
                                Console.WriteLine("Get time " + state);
                                Byte[] tosend = Encoding.ASCII.GetBytes(state);
                                try
                                {
                                    sender.BeginSend(tosend, tosend.Length, web, new AsyncCallback(OnSend), ss);
                                    sending = true;
                                }
                                catch (Exception e)
                                {
                                    Console.WriteLine(e.Message);
                                }
                            }
                            Thread.Sleep(5 * 1000);
                        }
                    });
                monitor.Name = "Monitor Thread";
                monitor.Start();
            }
            else
            {
                monitor.Resume();
            }

            Console.WriteLine("Monitor Thread running");
        }

        static void OnReceive(IAsyncResult ar)
        {
            Byte[] receiveBytes = receiver.EndReceive(ar, ref web_);
            string receiveString = Encoding.ASCII.GetString(receiveBytes);

            Console.WriteLine("Received: {0}", receiveString);

            switch(receiveString.ToLower())
            {
                case "monitor":
                    if (!monitoring)
                    {
                        MonitorStart();
                        monitoring = true;
                    }
                    break;
                case "stop":
                    if (monitoring)
                    {
                        monitor.Suspend();
                        monitoring = false;
                    }
                    break;
                case "address":
                    {
                        string address = IsisSystem.GetMyAddress().ToStringVerboseFormat();
                        Byte[] tosend = Encoding.ASCII.GetBytes(address);
                        sender.BeginSend(tosend, tosend.Length, web, new AsyncCallback(OnSend), ss);
                        sending = true;
                    }
                    break;
                case "members":
                    {
                        Address[] mems = smallGroup.GetView().members;
                        string members = "Members: ";
                        foreach (Address mem in mems)
                        {
                            members += mem.ToStringVerboseFormat() + "|";
                        }
                        Byte[] tosend = Encoding.ASCII.GetBytes(members);
                        sender.BeginSend(tosend, tosend.Length, web, new AsyncCallback(OnSend), ss);
                        sending = true;
                    }
                    break;
                case "query":
                    {
                        rankList.Clear();
                        int nr = smallGroup.Query(Group.ALL, SmallGroup.myTO, SmallGroup.COUNT, 1, "Query", SmallGroup.myEOL, rankList);
                        string result = nr.ToString() + " returns: ";
                        int total = 0;
                        foreach (int count in rankList)
                        {
                            result += "| " + count;
                            total += count;
                        }
                        result += "| Total processors: " + total;
                        Console.WriteLine(result);
                        Byte[] tosend = Encoding.ASCII.GetBytes(result);
                        sender.BeginSend(tosend, tosend.Length, web, new AsyncCallback(OnSend), ss);
                        sending = true;
                    }
                    break;
                default:
                    {
                        Byte[] tosend = Encoding.ASCII.GetBytes("Wrong command, Isis cannot handle");
                        sender.BeginSend(tosend, tosend.Length, web, new AsyncCallback(OnSend), ss);
                        sending = true;
                    }
                    break;
            }

            receiver.BeginReceive(new AsyncCallback(OnReceive), rs);
        }

        static void OnSend(IAsyncResult ar)
        {
            try
            {
                sender.EndSend(ar);
                sending = false;
            }
            catch (SocketException e)
            {
                Console.WriteLine(e.Message);
            }

            Console.WriteLine("Message sent to " + web.ToString());
        }
    }
}
