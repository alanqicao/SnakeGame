using System.Text.RegularExpressions;
using System.Xml;
using Model;
using SnakeGame;
using NetworkUtil;
using System.Text.Json;
using System.Diagnostics;

namespace Server
{
    /// <summary>
    /// This class represents the server side of a snake game project. It is in charge of starting up the server, accepting and disconnecting
    /// clients, receiving and sending information to and from clients. Runs and updates the world by calling the model classes to update the world.
    /// </summary>
    internal class Server
    {
        /// <summary>
        /// The world everything in the game exists in.
        /// </summary>
        private World world;
        /// <summary>
        /// Dictionary of currently connected clients
        /// </summary>
        private Dictionary<long, SocketState> clients;
        /// <summary>
        /// Hashset of disconnected clients
        /// </summary>
        private HashSet<long> disconnectedClients;
        /// <summary>
        /// Powerup template/placeholder
        /// </summary>
        private Powerup startPowerup;
        /// <summary>
        /// Watch to keep track of update timing in the world
        /// </summary>
        private Stopwatch watch;

        // fields for snakes
        /// <summary>
        /// snake speed field set by settings
        /// </summary>
        private int snakeSpeed;
        /// <summary>
        /// snake starting length set by settings
        /// </summary>
        private int snakeStartingLength;
        /// <summary>
        /// snake growth factor set by settings
        /// </summary>
        private int snakeGrowth;

        /// <summary>
        /// Default constructor for creating a server.
        /// </summary>
        public Server()
        {
            world = new World();
            clients = new Dictionary<long, SocketState>();
            world = new World();

            startPowerup = new Powerup();
            watch = new Stopwatch();
            disconnectedClients = new HashSet<long>();
            //  snake.snakeRegenerate += UpdateCameFromServer;
        }

        /// <summary>
        /// Starts the server by creating a server object, running the server on a seperate thread
        /// reading all the settings from the settigns.xml, starting the server, and keep the console open so that the server keeps running
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            Server server = new Server();
            Thread t = new Thread(server.Run);
            server.ReadSettings();
            server.StartServer();

            t.Start();
            Console.Read();
        }

        /// <summary>
        /// XML reader for reading default settings of the game
        /// </summary>
        public void ReadSettings()
        {
            XmlDocument xmlDoc = new XmlDocument();
            try
            {
                string Directory = ".." + Path.DirectorySeparatorChar + ".." + Path.DirectorySeparatorChar + ".." + Path.DirectorySeparatorChar;
                string fileName = "settings.xml";
                string fullPath = Path.Combine(Directory, fileName);
                xmlDoc.Load(fullPath);

                XmlNode databaseNode = xmlDoc.SelectSingleNode("GameSettings/database")!;
                if (databaseNode != null)
                {
                    world.MSPerFrame = int.Parse(databaseNode["MSPerFrame"]!.InnerText);
                    world.RespawnRate = int.Parse(databaseNode["RespawnRate"]!.InnerText);
                    world.Size = int.Parse(databaseNode["UniverseSize"]!.InnerText);
                    snakeSpeed = int.Parse(databaseNode["SnakeSpeed"]!.InnerText);
                    snakeStartingLength = int.Parse(databaseNode["SnakeStartingLength"]!.InnerText);
                    snakeGrowth = int.Parse(databaseNode["SnakeGrowth"]!.InnerText);
                    startPowerup.MaxPowerups = int.Parse(databaseNode["MaxPowerups"]!.InnerText);
                    startPowerup.MaxPowerupDelay = int.Parse(databaseNode["MaxPowerupDelay"]!.InnerText);

                    Console.WriteLine($"Database MSPerFrame: {world.MSPerFrame}");
                    Console.WriteLine($"Database RespawnRate: {world.RespawnRate}");
                    Console.WriteLine($"Database UniverseSize: {world.Size}");
                }

                XmlNode wallsNode = xmlDoc.SelectSingleNode("GameSettings/database/Walls")!;
                if (wallsNode != null)
                {
                    foreach (XmlNode wallNode in wallsNode.ChildNodes)
                    {
                        if (wallNode.Name == "Wall")
                        {
                            int id = int.Parse(wallNode["ID"]!.InnerText);
                            int x1 = int.Parse(wallNode["p1"]!["x"]!.InnerText);
                            int y1 = int.Parse(wallNode["p1"]!["y"]!.InnerText);
                            int x2 = int.Parse(wallNode["p2"]!["x"]!.InnerText);
                            int y2 = int.Parse(wallNode["p2"]!["y"]!.InnerText);

                            // Use these values as needed
                            Wall newWall = new Wall(id, x1, y1, x2, y2);
                            if (id >= 0)
                            {
                                world.walls.AddOrUpdate(id, newWall, (wallId, existingVal) =>
                                {
                                    existingVal = newWall;
                                    return existingVal;
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error reading XML: " + e.Message);
            }
        }

        /// <summary>
        /// Start accepting Tcp sockets connections from clients
        /// </summary>
        public void StartServer()
        {
            // This begins an "event loop"
            Networking.StartServer(NewClientConnected, 11000);

            Console.WriteLine("Server is running");
        }

        /// <summary>
        /// Method to be invoked by the networking library
        /// when a new client connects (see line 41)
        /// </summary>
        /// <param name="state">The SocketState representing the new client</param>
        private void NewClientConnected(SocketState state)
        {
            if (state.ErrorOccurred)
                return;

            // Save the client state
            // Need to lock here because clients can disconnect at any time
            lock (clients)
            {
                clients[state.ID] = state;
            }

            // change the state's network action to the 
            // receive handler so we can process data when something
            // happens on the network

            state.OnNetworkAction = ReceiveMessage;
            Networking.GetData(state);
            Console.WriteLine("new client connect: " + state.ID);
        }

        /// <summary>
        /// Method to be invoked by the networking library
        /// when a network action occurs (see lines 64-66)
        /// </summary>
        /// <param name="state"></param>
        private void ReceiveMessage(SocketState state)
        {
            // Remove the client if they aren't still connected
            if (state.ErrorOccurred)
            {
                RemoveClient(state.ID);
                return;
            }

            ProcessMessage(state);
            // Continue the event loop that receives messages from this client
            Networking.GetData(state);
        }


        /// <summary>
        /// Given the data that has arrived so far, 
        /// potentially from multiple receive operations, 
        /// determine if we have enough to make a complete message,
        /// and process it (print it and broadcast it to other clients).
        /// </summary>
        /// <param name="sender">The SocketState that represents the client</param>
        private void ProcessMessage(SocketState state)
        {
            //TODO- CHANGE IT 
            string totalData = state.GetData();

            string[] parts = Regex.Split(totalData, @"(?<=[\n])");

            // TODO - IF it name, or it is changing movement
            //IF name we send the world then keep looping sending world
            //if it is movement we change our logic and then send the world

            // Loop until we have processed all messages.
            // We may have received more than one.
            foreach (string p in parts)
            {
                // Ignore empty strings added by the regex splitter
                if (p.Length == 0)
                    continue;
                // The regex splitter will include the last string even if it doesn't end with a '\n',
                // So we need to ignore it if this happens. 
                if (p[p.Length - 1] != '\n')
                    break;

                // Console.WriteLine("received message from client " + state.ID + ": \"" + p.Substring(0, p.Length - 1) + "\"");

                // Remove it from the SocketState's growable buffer
                state.RemoveData(0, p.Length);

                // Broadcast the message to all clients
                // Lock here beccause we can't have new connections 
                // adding while looping through the clients list.
                // We also need to remove any disconnected clients.

                lock (clients)
                {

                    if (IsMatch(p))
                    {
                        //TODO - Movment
                        //{"moving":"left"}
                        JsonDocument document = JsonDocument.Parse(p);
                        JsonElement root = document.RootElement;
                        string movement = "";
                        if (root.TryGetProperty("moving", out JsonElement move))
                        {
                            movement = move.ToString();
                        }

                        if (world.snakes.TryGetValue((int)state.ID, out Snake? moveValueSnake))
                        {
                            moveValueSnake.changeDirection(movement);
                        }


                    }
                    // we find the sn
                    if (!world.snakes.TryGetValue((int)state.ID, out Snake? value))
                    {

                        // TODO- send the ID and world size to the clients
                        Networking.Send(state.TheSocket, state.ID + "\n" + world.Size + "\n");
                        /// make
                        Snake newSnake = new Snake();
                        world.snakes.TryAdd((int)state.ID, newSnake.GenerateSnake((int)state.ID, p.Substring(0, p.Length - 1), world, snakeSpeed, snakeStartingLength, snakeGrowth));

                        Console.WriteLine("World size send");
                    }
                }

            }
        }
        public void Run()
        {
            Console.WriteLine("Frame: " + world.MSPerFrame);
            initPowerUp();
            watch.Start();
            while (true)
            {

                while (watch.ElapsedMilliseconds < world.MSPerFrame)
                {
                    Thread.Sleep(1);
                    // Console.WriteLine("watch.ElapsedMilliseconds:" + watch.ElapsedMilliseconds);

                }

                foreach (long id in disconnectedClients)
                {
                    RemoveClient(id);
                }

                watch.Restart();

                    update();
            }
        }


        private void update()
        {
            // we do not need to send wall ever time only the first time!
            string json = GetAllSnakesInJson() + GetAllPowerupsInJson() + GetAllWallsInJson();
            string withOutWalls = GetAllSnakesInJson() + GetAllPowerupsInJson();
            // try catch  keep the sever runing

            if (clients.Count != 0)
            {
                foreach (long id in disconnectedClients)
                {
                    RemoveClient(id);
                }

                List<int> snakesToRemove = new List<int>();

                foreach (var snakeEntry in world.snakes)
                {
                    if (snakeEntry.Value.dc) // If the snake is marked as disconnected
                    {
                        snakesToRemove.Add(snakeEntry.Key); // Add to the list of snakes to be removed
                    }
                }

                foreach (int snakeId in snakesToRemove)
                {
                    world.snakes.TryRemove(snakeId, out _);
                }

                foreach (SocketState state in clients.Values)
                {

                    //TODO- LImit data sending 
                    if (!disconnectedClients.Contains(state.ID))
                    {
                        if (world.snakes.TryGetValue((int)state.ID, out Snake? value))
                        {
                            if (value.ReceivedWalls)
                            {
                                if (!Networking.Send(state.TheSocket, withOutWalls))
                                {
                                    disconnectedClients.Add(state.ID);
                                    //RemoveClient(state.ID);
                                    Console.WriteLine("clients: " + clients);
                                    Console.WriteLine("disconnectedClients: " + disconnectedClients);
                                }
                                else
                                {

                                }
                            }
                            else
                            {
                                if (!Networking.Send(state.TheSocket, json))
                                {
                                    disconnectedClients.Add(state.ID);
                                    //RemoveClient(state.ID);
                                    Console.WriteLine("clients: " + clients);
                                    Console.WriteLine("disconnectedClients: " + disconnectedClients);
                                }
                                else
                                {
                                    // Console.WriteLine("Send walls" + json);
                                    value.ReceivedWalls = true;
                                }
                            }
                        }
                    }
                }

                foreach (Snake snake in world.snakes.Values)
                {
                    if (snake.died || !snake.alive)
                    {
                        snake.UpdateRespawn(world);
                    }
                    else
                    {
                        snake.Moving(world);
                    }
                }

            }

        }

        private void initPowerUp()
        {
            for (int i = 0; i < startPowerup.MaxPowerups; i++)
            {
                world.powerups.TryAdd(i, Powerup.GeneratePowerUp(i, world));
            }

        }

        private string GetAllSnakesInJson()
        {
            //loop snakes
            // make it to json
            //return json
            string snakeJson = "";
            foreach (Snake snake in world.snakes.Values)
            {
                snakeJson += JsonSerializer.Serialize(snake) + "\n";
            }

            return snakeJson;
        }

        private string GetAllPowerupsInJson()

        {
            string powerupsJson = "";
            foreach (Powerup powerup in world.powerups.Values)
            {
                powerupsJson += JsonSerializer.Serialize(powerup) + "\n";
            }

            return powerupsJson;
        }

        private string GetAllWallsInJson()

        {
            string wallssJson = "";
            foreach (Wall wall in world.walls.Values)
            {
                wallssJson += JsonSerializer.Serialize(wall) + "\n";
            }
            return wallssJson;
        }


        /// <summary>
        /// Removes a client from the clients dictionary
        /// </summary>
        /// <param name="id">The ID of the client</param>
        private void RemoveClient(long id)
        {
            lock (clients)
            {
                if (clients.TryGetValue(id, out SocketState? client))
                {
                    // Close the client socket and remove it from the dictionary
                    client.TheSocket.Close();
                    clients.Remove(id);
                    Console.WriteLine("Client " + id + " disconnected");

                    // Mark the corresponding snake as disconnected
                    if (world.snakes.TryGetValue((int)id, out Snake? snake))
                    {
                        snake.dc = true; // Mark as disconnected
                    }
                }
            }
        }

        public static bool IsMatch(string input)
        {
            string pattern = @"\{\s*""moving""\s*:\s*""[^""]*""\s*\}";
            return Regex.IsMatch(input, pattern);
        }
    }
}