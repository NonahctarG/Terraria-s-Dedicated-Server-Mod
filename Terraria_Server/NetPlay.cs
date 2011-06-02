﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Terraria_Server
{
    using System;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading;

    public class NetPlay
    {
        public const int bufferSize = 1024;
        public ClientSock clientSock = null;
        public bool disconnect = false;
        public int maxConnections = 9;
        public string password = "";
        public IPAddress serverIP;
        public IPAddress serverListenIP;
        public int serverPort = 7777;
        public ServerSock[] serverSock = new ServerSock[9];
        public bool stopListen = false;
        public TcpListener tcpListener;
        private bool cake = false;

        public World world = null;

        public NetPlay() {
            clientSock = new ClientSock(world);
        }
        
        public NetPlay(World World) {
            clientSock = new ClientSock(world);
            world = World;
        }

        public void ClientLoop(object threadContext)
        {
            if (Statics.rand == null)
            {
                DateTime now = DateTime.Now;
                Statics.rand = new Random((int)now.Ticks);
            }
            if (WorldGen.genRand == null)
            {
                DateTime now = DateTime.Now;
                WorldGen.genRand = new Random((int)now.Ticks);
            }
            world.getPlayerList()[Statics.myPlayer].hostile = false;
            //Main.clientPlayer = (Player)world.getPlayerList()[Statics.myPlayer].clientClone();
            //Main.menuMode = 10;
            //Main.menuMode = 14;
            Console.WriteLine("Connecting to " + world.getServer().getNetPlay().serverIP);
            //Statics.netMode = 1;
            world.getServer().getNetPlay().disconnect = false;
            world.getServer().getNetPlay().clientSock = new ClientSock(world);
            world.getServer().getNetPlay().clientSock.tcpClient.NoDelay = true;
            world.getServer().getNetPlay().clientSock.readBuffer = new byte[1024];
            world.getServer().getNetPlay().clientSock.writeBuffer = new byte[1024];
            try
            {
                world.getServer().getNetPlay().clientSock.tcpClient.Connect(world.getServer().getNetPlay().serverIP, world.getServer().getNetPlay().serverPort);
                world.getServer().getNetPlay().clientSock.networkStream = world.getServer().getNetPlay().clientSock.tcpClient.GetStream();
            }
            catch (Exception arg_127_0)
            {
                Exception exception = arg_127_0;
                if (!world.getServer().getNetPlay().disconnect)
                {
                    Console.WriteLine(exception.ToString());
                    world.getServer().getNetPlay().disconnect = true;
                }
            }
            NetMessage.buffer[9].Reset();
            int num = -1;
            while (!world.getServer().getNetPlay().disconnect)
            {
                if (world.getServer().getNetPlay().clientSock.tcpClient.Connected)
                {
                    if (NetMessage.buffer[9].checkBytes)
                    {
                        NetMessage.CheckBytes(world, 9);
                    }
                    world.getServer().getNetPlay().clientSock.active = true;
                    if (world.getServer().getNetPlay().clientSock.state == 0)
                    {
                        Console.WriteLine("Found server");
                        world.getServer().getNetPlay().clientSock.state = 1;
                        NetMessage.SendData(1, world, -1, -1, "", 0, 0f, 0f, 0f);
                    }
                    if (world.getServer().getNetPlay().clientSock.state == 2 && num != world.getServer().getNetPlay().clientSock.state)
                    {
                        Console.WriteLine("Sending player data...");
                    }
                    if (world.getServer().getNetPlay().clientSock.state == 3 && num != world.getServer().getNetPlay().clientSock.state)
                    {
                        Console.WriteLine("Requesting world information");
                    }
                    if (world.getServer().getNetPlay().clientSock.state == 4)
                    {
                        //WorldGen.worldCleared = false;
                        world.getServer().getNetPlay().clientSock.state = 5;
                        WorldGen.clearWorld(world);
                    }
                    if (world.getServer().getNetPlay().clientSock.state == 5) // && WorldGen.worldCleared)
                    {
                        world.getServer().getNetPlay().clientSock.state = 6;
                        world.getPlayerList()[Statics.myPlayer].FindSpawn(world);
                        NetMessage.SendData(8, world, -1, -1, "", world.getPlayerList()[Statics.myPlayer].SpawnX, (float)world.getPlayerList()[Statics.myPlayer].SpawnY, 0f, 0f);
                    }
                    if (world.getServer().getNetPlay().clientSock.state == 6 && num != world.getServer().getNetPlay().clientSock.state)
                    {
                        Console.WriteLine("Requesting tile data");
                    }
                    if (!world.getServer().getNetPlay().clientSock.locked && !world.getServer().getNetPlay().disconnect && world.getServer().getNetPlay().clientSock.networkStream.DataAvailable)
                    {
                        world.getServer().getNetPlay().clientSock.locked = true;
                        world.getServer().getNetPlay().clientSock.networkStream.BeginRead(world.getServer().getNetPlay().clientSock.readBuffer, 0, world.getServer().getNetPlay().clientSock.readBuffer.Length, new AsyncCallback(world.getServer().getNetPlay().clientSock.ClientReadCallBack), world.getServer().getNetPlay().clientSock.networkStream);
                    }
                    if (world.getServer().getNetPlay().clientSock.statusMax > 0 && world.getServer().getNetPlay().clientSock.statusText != "")
                    {
                        if (world.getServer().getNetPlay().clientSock.statusCount >= world.getServer().getNetPlay().clientSock.statusMax)
                        {
                            Console.WriteLine(world.getServer().getNetPlay().clientSock.statusText + ": Complete!");
                            world.getServer().getNetPlay().clientSock.statusText = "";
                            world.getServer().getNetPlay().clientSock.statusMax = 0;
                            world.getServer().getNetPlay().clientSock.statusCount = 0;
                        }
                        else
                        {
                            Console.WriteLine(string.Concat(new object[]
							{
								world.getServer().getNetPlay().clientSock.statusText, 
								": ", 
								(int)((float)world.getServer().getNetPlay().clientSock.statusCount / (float)world.getServer().getNetPlay().clientSock.statusMax * 100f), 
								"%"
							}));
                        }
                    }
                    Thread.Sleep(1);
                }
                else
                {
                    if (world.getServer().getNetPlay().clientSock.active)
                    {
                        Console.WriteLine("Lost connection");
                        world.getServer().getNetPlay().disconnect = true;
                    }
                }
                num = world.getServer().getNetPlay().clientSock.state;
            }
            try
            {
                world.getServer().getNetPlay().clientSock.networkStream.Close();
                world.getServer().getNetPlay().clientSock.networkStream = world.getServer().getNetPlay().clientSock.tcpClient.GetStream();
            }
            catch
            {
            }
            //if (!Main.gameMenu)
            //{
                //Main.netMode = 0;
                Player.SavePlayer(world.getPlayerList()[Statics.myPlayer]);
                //Main.gameMenu = true;
                //Main.menuMode = 14;
            //}
            NetMessage.buffer[9].Reset();
            //if (Main.menuMode == 15 && Main.statusText == "Lost connection")
            //{
                //Main.menuMode = 14;
            //}
            if (world.getServer().getNetPlay().clientSock.statusText != "" && world.getServer().getNetPlay().clientSock.statusText != null)
            {
                //Console.WriteLine("Lost connection");
            }
            world.getServer().getNetPlay().clientSock.statusCount = 0;
            world.getServer().getNetPlay().clientSock.statusMax = 0;
            world.getServer().getNetPlay().clientSock.statusText = "";
           // Main.netMode = 0;
        }

        public static int GetSectionX(int x)
        {
            return x / 200;
        }
        
        public static int GetSectionY(int y)
        {
            return y / 150;
        }

        public void Init()
        {
            for (int i = 0; i < 10; i++)
            {
                if (i < 9)
                {
                    world.getServer().getNetPlay().serverSock[i] = new ServerSock(world);
                    world.getServer().getNetPlay().serverSock[i].tcpClient.NoDelay = true;
                }
                NetMessage.buffer[i] = new messageBuffer();
                NetMessage.buffer[i].whoAmI = i;
            }
            world.getServer().getNetPlay().clientSock.tcpClient.NoDelay = true;
        }

        public void ListenForClients(object threadContext)
        {
            while (!world.getServer().getNetPlay().disconnect && !world.getServer().getNetPlay().stopListen)
            {
                int num = -1;
                for (int i = 0; i < 8; i++)
                {
                    if (!world.getServer().getNetPlay().serverSock[i].tcpClient.Connected)
                    {
                        num = i;
                        break;
                    }
                }
                if (num >= 0)
                {
                    try
                    {
                        world.getServer().getNetPlay().serverSock[num].tcpClient = world.getServer().getNetPlay().tcpListener.AcceptTcpClient();
                        world.getServer().getNetPlay().serverSock[num].tcpClient.NoDelay = true;
                    }
                    catch (Exception arg_6B_0)
                    {
                        Exception exception = arg_6B_0;
                        if (!world.getServer().getNetPlay().disconnect)
                        {
                            //Main.menuMode = 15;
                            Console.WriteLine(exception.ToString());
                            world.getServer().getNetPlay().disconnect = true;
                        }
                    }
                }
                else
                {
                    world.getServer().getNetPlay().stopListen = true;
                    world.getServer().getNetPlay().tcpListener.Stop();
                }
            }
        }

        public void ServerLoop(object threadContext)
        {
            if (Statics.rand == null)
            {
                DateTime now = DateTime.Now;
                Statics.rand = new Random((int)now.Ticks);
            }
            if (WorldGen.genRand == null)
            {
                DateTime now = DateTime.Now;
                WorldGen.genRand = new Random((int)now.Ticks);
            }
            Statics.myPlayer = 8;
            world.getServer().getNetPlay().serverIP = IPAddress.Any;
            world.getServer().getNetPlay().serverListenIP = world.getServer().getNetPlay().serverIP;
            //Main.menuMode = 14;
            Console.WriteLine("Starting server...");
            Statics.netMode = 2;
            world.getServer().getNetPlay().disconnect = false;
            for (int i = 0; i < 9; i++)
            {
                world.getServer().getNetPlay().serverSock[i] = new ServerSock(world);
                world.getServer().getNetPlay().serverSock[i].Reset();
                world.getServer().getNetPlay().serverSock[i].whoAmI = i;
                world.getServer().getNetPlay().serverSock[i].tcpClient = new TcpClient();
                world.getServer().getNetPlay().serverSock[i].tcpClient.NoDelay = true;
                world.getServer().getNetPlay().serverSock[i].readBuffer = new byte[1024];
                world.getServer().getNetPlay().serverSock[i].writeBuffer = new byte[1024];
            }
            world.getServer().getNetPlay().tcpListener = new TcpListener(world.getServer().getNetPlay().serverListenIP, world.getServer().getNetPlay().serverPort);
            try
            {
                world.getServer().getNetPlay().tcpListener.Start();
            }
            catch (Exception arg_142_0)
            {
                Exception exception = arg_142_0;
                //Main.menuMode = 15;
                Console.WriteLine(exception.ToString());
                world.getServer().getNetPlay().disconnect = true;
            }
            if (!world.getServer().getNetPlay().disconnect)
            {
                ThreadPool.QueueUserWorkItem(new WaitCallback(world.getServer().getNetPlay().ListenForClients), 1);
                Console.WriteLine("Server started");
            }
            while (!world.getServer().getNetPlay().disconnect)
            {
                if (world.getServer().getNetPlay().stopListen)
                {
                    int num = -1;
                    for (int j = 0; j < 8; j++)
                    {
                        if (!world.getServer().getNetPlay().serverSock[j].tcpClient.Connected)
                        {
                            num = j;
                            break;
                        }
                    }
                    if (num >= 0)
                    {
                        world.getServer().getNetPlay().tcpListener.Start();
                        world.getServer().getNetPlay().stopListen = false;
                        ThreadPool.QueueUserWorkItem(new WaitCallback(world.getServer().getNetPlay().ListenForClients), 1);
                    }
                }
                int num2 = 0;
                for (int k = 0; k < 9; k++)
                {
                    if (NetMessage.buffer[k].checkBytes)
                    {
                        NetMessage.CheckBytes(world, k);
                    }
                    if (world.getServer().getNetPlay().serverSock[k].kill)
                    {
                        world.getServer().getNetPlay().serverSock[k].Reset();
                        NetMessage.syncPlayers(world);
                    }
                    else
                    {
                        if (world.getServer().getNetPlay().serverSock[k].tcpClient.Connected)
                        {
                            if (!world.getServer().getNetPlay().serverSock[k].active)
                            {
                                world.getServer().getNetPlay().serverSock[k].state = 0;
                            }
                            world.getServer().getNetPlay().serverSock[k].active = true;
                            num2++;
                            if (!world.getServer().getNetPlay().serverSock[k].locked)
                            {
                                try
                                {
                                    world.getServer().getNetPlay().serverSock[k].networkStream = world.getServer().getNetPlay().serverSock[k].tcpClient.GetStream();
                                    if (world.getServer().getNetPlay().serverSock[k].networkStream.DataAvailable)
                                    {
                                        world.getServer().getNetPlay().serverSock[k].locked = true;
                                        world.getServer().getNetPlay().serverSock[k].networkStream.BeginRead(world.getServer().getNetPlay().serverSock[k].readBuffer, 0, world.getServer().getNetPlay().serverSock[k].readBuffer.Length, new AsyncCallback(world.getServer().getNetPlay().serverSock[k].ServerReadCallBack), world.getServer().getNetPlay().serverSock[k].networkStream);
                                    }
                                }
                                catch
                                {
                                    world.getServer().getNetPlay().serverSock[k].kill = true;
                                }
                            }
                            if (world.getServer().getNetPlay().serverSock[k].statusMax > 0 && world.getServer().getNetPlay().serverSock[k].statusText2 != "")
                            {
                                if (world.getServer().getNetPlay().serverSock[k].statusCount >= world.getServer().getNetPlay().serverSock[k].statusMax)
                                {
                                    world.getServer().getNetPlay().serverSock[k].statusText = string.Concat(new object[]
									{
										"(", 
										world.getServer().getNetPlay().serverSock[k].tcpClient.Client.RemoteEndPoint, 
										") ", 
										world.getServer().getNetPlay().serverSock[k].name, 
										" ", 
										world.getServer().getNetPlay().serverSock[k].statusText2, 
										": Complete!"
									});
                                    world.getServer().getNetPlay().serverSock[k].statusText2 = "";
                                    world.getServer().getNetPlay().serverSock[k].statusMax = 0;
                                    world.getServer().getNetPlay().serverSock[k].statusCount = 0;
                                }
                                else
                                {
                                    world.getServer().getNetPlay().serverSock[k].statusText = string.Concat(new object[]
									{
										"(", 
										world.getServer().getNetPlay().serverSock[k].tcpClient.Client.RemoteEndPoint, 
										") ", 
										world.getServer().getNetPlay().serverSock[k].name, 
										" ", 
										world.getServer().getNetPlay().serverSock[k].statusText2, 
										": ", 
										(int)((float)world.getServer().getNetPlay().serverSock[k].statusCount / (float)world.getServer().getNetPlay().serverSock[k].statusMax * 100f), 
										"%"
									});
                                }
                            }
                            else
                            {
                                if (world.getServer().getNetPlay().serverSock[k].state == 0)
                                {
                                    world.getServer().getNetPlay().serverSock[k].statusText = string.Concat(new object[]
									{
										"(", 
										world.getServer().getNetPlay().serverSock[k].tcpClient.Client.RemoteEndPoint, 
										") ", 
										world.getServer().getNetPlay().serverSock[k].name, 
										" is connecting..."
									});
                                }
                                else
                                {
                                    if (world.getServer().getNetPlay().serverSock[k].state == 1)
                                    {
                                        world.getServer().getNetPlay().serverSock[k].statusText = string.Concat(new object[]
										{
											"(", 
											world.getServer().getNetPlay().serverSock[k].tcpClient.Client.RemoteEndPoint, 
											") ", 
											world.getServer().getNetPlay().serverSock[k].name, 
											" is sending player data..."
										});
                                    }
                                    else
                                    {
                                        if (world.getServer().getNetPlay().serverSock[k].state == 2)
                                        {
                                            world.getServer().getNetPlay().serverSock[k].statusText = string.Concat(new object[]
											{
												"(", 
												world.getServer().getNetPlay().serverSock[k].tcpClient.Client.RemoteEndPoint, 
												") ", 
												world.getServer().getNetPlay().serverSock[k].name, 
												" requested world information"
											});
                                        }
                                        else
                                        {
                                            if (world.getServer().getNetPlay().serverSock[k].state != 3 && world.getServer().getNetPlay().serverSock[k].state == 10)
                                            {
                                                world.getServer().getNetPlay().serverSock[k].statusText = string.Concat(new object[]
												{
													"(", 
													world.getServer().getNetPlay().serverSock[k].tcpClient.Client.RemoteEndPoint, 
													") ", 
													world.getServer().getNetPlay().serverSock[k].name, 
													" is playing"
												});
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (world.getServer().getNetPlay().serverSock[k].active)
                            {
                                world.getServer().getNetPlay().serverSock[k].kill = true;
                            }
                            else
                            {
                                world.getServer().getNetPlay().serverSock[k].statusText2 = "";
                                if (k < 8)
                                {
                                    world.getPlayerList()[k].active = false;
                                }
                            }
                        }
                    }
                    Thread.Sleep(1);
                }
                if (!Statics.saveLock)
                {
                    if (num2 == 0)
                    {
                        if (!world.getServer().getNetPlay().cake)
                        {
                            world.getServer().getNetPlay().cake = true;
                            Program.updateThread.Start(world.getServer());
                            Console.WriteLine("Started update thread!");
                        }
                    }
                }
            }
            world.getServer().getNetPlay().tcpListener.Stop();
            for (int l = 0; l < 9; l++)
            {
                world.getServer().getNetPlay().serverSock[l].Reset();
            }
            //if (Main.menuMode != 15)
            //{
            //    Main.netMode = 0;
            //    Main.menuMode = 10;
            //    WorldGen.saveWorld(false);
            //    while (WorldGen.saveLock)
            //    {
            //    }
            //    Main.menuMode = 0;
            //}
            //else
            //{
            //    Main.netMode = 0;
            //}
            //Main.myPlayer = 0;
        }

        public bool SetIP(string newIP)
        {
            bool result;
            try
            {
                world.getServer().getNetPlay().serverIP = IPAddress.Parse(newIP);
            }
            catch
            {
                result = false;
                return result;
            }
            result = true;
            return result;
        }

        public bool SetIP2(string newIP)
        {
            bool result;
            try
            {
                IPAddress[] addressList = Dns.GetHostEntry(newIP).AddressList;
                for (int i = 0; i < addressList.Length; i++)
                {
                    if (addressList[i].AddressFamily == AddressFamily.InterNetwork)
                    {
                        world.getServer().getNetPlay().serverIP = addressList[i];
                        result = true;
                        return result;
                    }
                }
                result = false;
            }
            catch
            {
                result = false;
            }
            return result;
        }

        public void StartClient()
        {
            ThreadPool.QueueUserWorkItem(new WaitCallback(world.getServer().getNetPlay().ClientLoop), 1);
        }

        public void StartServer()
        {
            ThreadPool.QueueUserWorkItem(new WaitCallback(world.getServer().getNetPlay().ServerLoop), 1);
        }

        public Player GetPlayerByName(string name)
        {
            Player player = null;
            int num = 0;
            Player[] player2 = world.getPlayerList();
            Player result;
            for (int i = 0; i < player2.Length; i++)
            {
                Player player3 = player2[i];
                if (player3.name.ToLower() == name.ToLower())
                {
                    result = player3;
                    return result;
                }
                if (player3.name.CompareTo(name) > num && player3.name.IndexOf(name) > -1)
                {
                    num = player3.name.CompareTo(name);
                    player = player3;
                }
            }
            result = player;
            return result;
        }
    }

}