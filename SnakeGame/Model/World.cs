using System.Collections.Concurrent;
namespace Model
{
    /// <summary>
    /// This class represents the state of the world that every object will live in.
    /// It will hold all powerups, snakes, walls, and the size of the world. Every client
    /// has their own world, and will contain a reference to their
    /// </summary>
    public class World
    {
        /// <summary>
        /// Dictionary that holds all currently playing and connected snakes in the world
        /// </summary>
        public ConcurrentDictionary<int, Snake> snakes;
        /// <summary>
        /// Dictionary that holds all currently alive powerups in the world
        /// </summary>
        public ConcurrentDictionary<int, Powerup> powerups;
        /// <summary>
        /// Dictionary that holds all walls located in the world.
        /// </summary>
        public ConcurrentDictionary<int, Wall> walls;
        /// <summary>
        /// Contains the size of the world, specified by the server
        /// </summary>
        public int Size { get; set; }
        /// <summary>
        /// Snake ID of the current player
        /// </summary>
        public int OwnSnakeID { get; set; }
        /// <summary>
        /// MPS for the game update speed
        /// </summary>
        public int MSPerFrame { get; set; }
        /// <summary>
        /// Milliseconds for how long to wait till something respawns.
        /// </summary>
        public int RespawnRate { get; set; }

        /// <summary>
        /// Constructor to initialize all fields
        /// </summary>
        public World()
        {
            snakes = new ConcurrentDictionary<int, Snake>();
            powerups = new ConcurrentDictionary<int, Powerup>();
            walls = new ConcurrentDictionary<int, Wall>();
            Size = 0;
            OwnSnakeID = 0;
        }
        /// <summary>
        /// Constructor to initialize fields given a player's snake ID, and a world size.
        /// </summary>
        /// <param name="id"> Current player snake ID</param>
        /// <param name="s"> World size</param>
        public World(int id, int s)
        {
            snakes = new ConcurrentDictionary<int, Snake>();
            powerups = new ConcurrentDictionary<int, Powerup>();
            walls = new ConcurrentDictionary<int, Wall>();
            Size = s;
            OwnSnakeID = id;
        }

        /// <summary>
        /// Constructor that allows you to construct a world with a given amount of walls, snakes, powerups, Player snake ID, and world size
        /// </summary>
        /// <param name="walls">All walls</param>
        /// <param name="snakes">All snakes</param>
        /// <param name="powerups">All powerups</param>
        /// <param name="ownSnakeID">Current snake player ID</param>
        /// <param name="worldSize">World size</param>
        public World(ConcurrentDictionary<int, Wall> walls, ConcurrentDictionary<int, Snake> snakes, ConcurrentDictionary<int, Powerup> powerups, int ownSnakeID, int worldSize)
        {
            this.snakes = snakes;
            this.powerups = powerups;
            this.walls = walls;
            this.OwnSnakeID = ownSnakeID;
            this.Size = worldSize;
        }
    }
}

