using SnakeGame;
using System.Text.Json.Serialization;
namespace Model

{
    /// <summary>
    /// This class represents powerups that will live in the world. All powerups
    /// have an ID, a location, and a bool stating if it is dead or not. If it is dead
    /// then it will disappear Contains logic for wall colision, where to respawn, and when to respawn.
    /// </summary>
    public class Powerup
    {
        /// <summary>
        /// an int representing the powerup's unique ID.
        /// </summary>
        public int power { get; set; }
        /// <summary>
        ///  a Vector2D representing the location of the powerup.
        /// </summary>
        public Vector2D loc { get; set; }
        /// <summary>
        /// a bool indicating if the powerup "died" (was collected by a player) on this frame.
        /// </summary>
        public bool died { get; set; }
        /// <summary>
        /// Maximum number of powerups in the world
        /// </summary>
        public int MaxPowerups { get; set; }
        /// <summary>
        /// Time delay for a powerup before it respawns after a powerup is eaten.
        /// </summary>
        public int MaxPowerupDelay { get; set; }
        /// <summary>
        /// number to countdown till the powerup respawns 
        /// </summary>
        private int powerupTimer;

        /// <summary>
        /// Default Constructor
        /// </summary>
        public Powerup()
        {
            power = 0;
            loc = new Vector2D();
            died = false;
            powerupTimer = 0;
        }

        /// <summary>
        /// Constructor for initializing this powerup that will be in the world.
        /// </summary>
        /// <param name="id">Identity ID</param>
        /// <param name="x">x-coord of the power</param>
        /// <param name="y">y-coord of the power</param>
        /// <param name="d">death bool</param>
        public Powerup(int id, int x, int y, bool d)
        {
            power = id;
            loc = new Vector2D(x, y);
            died = d;
            powerupTimer = 0;
        }

        /// <summary>
        /// JsonConstructor for initializing this powerup that will be in the world
        /// </summary>
        /// <param name="power">Identity ID</param>
        /// <param name="loc">Location</param>
        /// <param name="died">death bool</param>
        [JsonConstructor]
        public Powerup(int power, Vector2D loc, bool died)
        {
            this.power = power;
            this.loc = loc;
            this.died = died;
            powerupTimer = 0;
        }

        /// <summary>
        /// Method for Generating a powerup with a given ID in the world
        /// </summary>
        /// <param name="id"></param>
        /// <param name="world"></param>
        /// <returns></returns>
        public static Powerup GeneratePowerUp(int id, World world)
        {
            Random random = new Random();
            double maxValue = (double)world.Size;

            Powerup newPowerup;
            do
            {
                double randomDoubleX = (random.NextDouble() * maxValue) - maxValue / 2;
                double randomDoubleY = (random.NextDouble() * maxValue) - maxValue / 2;

                newPowerup = new Powerup(id, (int)randomDoubleX, (int)randomDoubleY, false);
            }
            while (CheckWalls(world, newPowerup)); // Continue until a location not colliding with any wall is found

            return newPowerup;
        }

        /// <summary>
        /// Private helper method to handle spawn collisions.Helps check if the powerup will spawn inside a wall
        /// if it will spawn in a wall, return true. if it is a location that is not inside a wall, return false
        /// </summary>
        /// <param name="world"></param>
        /// <param name="powerup"></param>
        /// <returns></returns>
        private static bool CheckWalls(World world, Powerup powerup)
        {
            double powerupPadding = 8;
            double wallPadding = 25;

            foreach (var wall in world.walls)
            {
                double wallXmax = Math.Max(wall.Value.p1.GetX(), wall.Value.p2.GetX()) + wallPadding;
                double wallXmin = Math.Min(wall.Value.p1.GetX(), wall.Value.p2.GetX()) - wallPadding;

                double wallYmax = Math.Max(wall.Value.p1.GetY(), wall.Value.p2.GetY()) + wallPadding;
                double wallYmin = Math.Min(wall.Value.p1.GetY(), wall.Value.p2.GetY()) - wallPadding;

                // Check if power-up (including its padding) intersects with the wall (including its padding)
                if (powerup.loc.GetX() - powerupPadding <= wallXmax &&
                    powerup.loc.GetX() + powerupPadding >= wallXmin &&
                    powerup.loc.GetY() - powerupPadding <= wallYmax &&
                    powerup.loc.GetY() + powerupPadding >= wallYmin)
                {
                    return true; // Collision detected
                }
            }

            return false; // No collision detected
        }

        /// <summary>
        /// Method to update the power-up timer. Count down from a set time
        /// </summary>
        public void UpdateTimer()
        {
            if (powerupTimer > 0)
            {
                powerupTimer--;
            }
        }

        /// <summary>
        /// Method to reset the timer with a random delay
        /// </summary>
        public void ResetTimer()
        {
            Random random = new Random();
            powerupTimer = random.Next(0, MaxPowerupDelay);
        }

        /// <summary>
        /// If the powerup Timer is equal to zero, return true, we can now generate powerups
        /// else it returns false, wait to generate powerups
        /// </summary>
        /// <returns></returns>
        public bool CanGenerateNewPowerup()
        {
            return powerupTimer == 0;
        }
    }
}

