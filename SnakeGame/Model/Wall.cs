using SnakeGame;
using System.Text.Json.Serialization;
namespace Model
{
    /// <summary>
    /// This class represents a wall inside the world of the game. Every wall has an ID
    /// and location made up of two vectors. If both vectors are the same, it is a single wall
    /// if the two vectors are different, it will be a line of walls from the first wall to
    /// the last wall.
    /// </summary>
    public class Wall
    {
        /// <summary>
        /// an int representing the wall's unique ID.
        /// </summary>
        public int wall { get; set; }
        /// <summary>
        /// a Vector2D representing one endpoint of the wall.
        /// </summary>
        public Vector2D p1 { get; set; }
        /// <summary>
        /// a Vector2D representing the other endpoint of the wall.
        /// </summary>
        public Vector2D p2 { get; set; }

        /// <summary>
        /// Constructor for initializing wall
        /// </summary>
        /// <param name="wID">Wall's ID</param>
        /// <param name="x1">Wall1 x-coord</param>
        /// <param name="y1">Wall1 y-coord</param>
        /// <param name="x2">Wall2 x-coord</param>
        /// <param name="y2">Wall2 y-coord</param>
        public Wall(int wID, int x1, int y1, int x2, int y2)
        {
            this.wall = wID;
            this.p1 = new Vector2D(x1, y1);
            this.p2 = new Vector2D(x2, y2);
        }
        /// <summary>
        /// JsonConstructor for initializing wall
        /// </summary>
        /// <param name="wall">Wall's ID</param>
        /// <param name="p1">Location of the first wall</param>
        /// <param name="p2">Location of the second wall</param>
        [JsonConstructor]
        public Wall(int wall, Vector2D p1, Vector2D p2)
        {
            this.wall = wall;
            this.p1 = new Vector2D(p1.X, p1.Y);
            this.p2 = new Vector2D(p2.X, p2.Y);

        }
    }
}

