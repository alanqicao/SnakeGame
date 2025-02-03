using System.Text.Json.Serialization;
using SnakeGame;

namespace Model
{

    /// <summary>
    /// This class represents a snake in the world of the game.Every Snake has an ID, a name, a body, a direction
    /// that the head is pointing,a score, bools pertaining to whether the snake is: dead, alive, disconnected
    /// from the server, joined to the server.
    ///
    /// Information NOT decided by the snake, but that effect the snake
    /// The snakes can move in the world, and will die if their head makes
    /// contact with itself, another snake, or a wall. If a snake eats a powerup it will grow. 
    /// </summary>
    public class Snake
    {
        /// <summary>
        /// an int representing the snake's unique ID.  
        /// </summary>
        public int snake { get; set; }
        /// <summary>
        /// a string representing the player's name.
        /// </summary>
        public string name { get; set; }
        /// <summary>
        /// a List<Vector2D> representing the entire body of the snakeEach point in this list
        /// represents one vertex of the snake's body, where consecutive vertices make up a
        /// straight segment of the body. The first point of the list gives the location of
        /// the snake's tail, and the last gives the location of the snake's head. 
        /// </summary>
        public List<Vector2D> body { get; set; }
        /// <summary>
        /// a Vector2D representing the snake's orientation. This will always be an axis-aligned
        /// vector (purely horizontal or vertical).
        /// </summary>
        public Vector2D dir { get; set; }
        /// <summary>
        /// an int representing the player's score (the number of powerups it has eaten)
        /// </summary>
        public int score { get; set; } = 0;
        /// <summary>
        ///  a bool indicating if the snake died on this frame. This will only be true on the exact frame in which the snake died.
        /// </summary>
        public bool died { get; set; } = false;
        /// <summary>
        /// a bool indicating whether a snake is alive or dead.
        /// </summary>
        public bool alive { get; set; } = true;
        /// <summary>
        ///  a bool indicating if the player controlling that snake disconnected on that frame. The server will send the snake
        ///  with this flag set to true only once, then it will discontinue sending that snake for the rest of the game.
        /// </summary>
        public bool dc { get; set; } = false;
        /// <summary>
        /// Sa bool indicating if the player joined on this frame. This will only be true for one frame.
        /// </summary>
        public bool join { get; set; } = false;
        /// <summary>
        /// The current frame
        /// </summary>
        public int frameNum { get; set; }
        /// <summary>
        /// The current direction that is different from the previous direction
        /// </summary>
        public int nextDirChange { get; set; }

        //______________________________________________________________________
        //Setting set in the settings.xml
        /// <summary>
        /// Property for how far a snake should travel in one frame
        /// </summary>
        public int SnakeSpeed { get; set; }

        /// <summary>
        /// Property for how long the snake should spawn when initially spawning, or respawning
        /// </summary>
        public int SnakeStartingLength { get; set; }

        /// <summary>
        /// Property for how much the snake grows when it eats a powerup
        /// </summary>
        public int SnakeGrowth { get; set; }
        //______________________________________________________________________

        /// <summary>
        /// number to countdown till the snake respawns
        /// </summary>
        public int RespawnTimer { get; set; } = 0;

        /// <summary>
        /// property to show that if a snake has received walls, it no longer needs
        /// to receive walls in the Json information since walls don't change position.
        /// </summary>
        public bool ReceivedWalls { get; set; } = false;

        /// <summary>
        /// Default constructor
        /// </summary>
        public Snake()
        {
            snake = 0;
            name = "newSnake";
            body = new List<Vector2D>();
            dir = new Vector2D();
        }

        /// <summary>
        /// Constructor for initializing a snake.
        /// </summary>
        /// <param name="s">ID of the snake</param>
        /// <param name="b">Body of the snake</param>
        /// <param name="n">Name of the snake</param>
        /// <param name="x">x-coord of the snake</param>
        /// <param name="y">y-coord of the snake</param>
        public Snake(int s, List<Vector2D> b, string n, int x, int y)
        {
            snake = s;
            name = n;
            body = b;
            dir = new Vector2D(x, y);
        }

        /// <summary>
        /// Use Default constructor for normal information initialization, in addition to default
        /// we set the properties as specified by the settings.xml
        /// </summary>
        /// <param name="snakeSpeed">Property for how far a snake should travel in one frame</param>
        /// <param name="snakeStartingLength">Property for how long the snake should spawn when initially spawning, or respawning</param>
        /// <param name="snakeGrowth">Property for how much the snake grows when it eats a powerup</param>
        public Snake(int snakeSpeed, int snakeStartingLength, int snakeGrowth) : this()
        {
            this.SnakeSpeed = snakeSpeed;
            this.SnakeStartingLength = snakeStartingLength;
            this.SnakeGrowth = snakeGrowth;
        }

        /// <summary>
        /// JsonConstructor for initializing a snake.
        /// </summary>
        /// <param name="snake">ID of the snake</param>
        /// <param name="body">Body of the snake</param>
        /// <param name="dir">Direction of the snake</param>
        /// <param name="name">Name of the player's snake</param>
        /// <param name="score">Score of the player's snake</param>
        /// <param name="died">State of the snake</param>
        /// <param name="alive">State of the snake</param>
        /// <param name="dc">Connection state of the snake</param>
        /// <param name="join">Connection state of the snake</param>
        [JsonConstructor]
        public Snake(int snake, List<Vector2D> body, Vector2D dir, string name, int score, bool died, bool alive, bool dc, bool join)
        {
            this.snake = snake;
            this.body = body;
            this.dir = new Vector2D(dir.X, dir.Y);
            this.name = name;
            this.score = score;
            this.died = died;
            this.alive = alive;
            this.dc = dc;
            this.join = join;
        }

        /// <summary>
        /// Method that takes one step in the direction the head is pointing.
        /// </summary>
        /// <param name="world">the world where everything exists</param>
        public void Moving(World world)
        {// Calculate new head position based on direction
            Vector2D newHead = new Vector2D(body.Last().X, body.Last().Y);
            //Get direction, and move in that direction
            switch (dir.ToAngle())
            {
                case 0: newHead.Y -= SnakeSpeed; break; // Up
                case 90: newHead.X += SnakeSpeed; break; // Right
                case 180: newHead.Y += SnakeSpeed; break; // Down
                case -90: newHead.X -= SnakeSpeed; break; // Left
            }

            // Wrap around world boundaries if the new head is outside
            WrapAroundWorldBounds(newHead, world.Size);

            // Move the snake: insert the new head at the end and remove the tail
            body.Add(newHead);
            Vector2D tempV = body.First() - body.Last();

            if (CalculateSnakeLength() > SnakeStartingLength + (score * SnakeGrowth))
            {
                body.RemoveAt(0); // Remove the tail
            }

            // Handle collisions and death logic
            if (checkCollisionWalls(world, this) || checkCollisionSnakes(world, this))
            {
                this.died = true;
                this.alive = false;
            }

            if (checkCollisionPowerUp(this, world))
            {
                Grow();
            }
        }

        /// <summary>
        /// Helper method to calculate the total length of the body, from head to tail
        /// </summary>
        /// <returns></returns>
        public double CalculateSnakeLength()
        {
            double totalLength = 0;
            for (int i = 0; i < body.Count - 1; i++)
            {
                totalLength += (body[i + 1] - body[i]).Length();
            }
            return totalLength;
        }

        /// <summary>
        /// Helper method to handle world wrapping logic once a snake head hits the edge of the world.
        /// </summary>
        /// <param name="position">Position of the head</param>
        /// <param name="worldSize">Size of the world</param>
        private void WrapAroundWorldBounds(Vector2D position, int worldSize)
        {
            int halfWorldSize = worldSize / 2;

            if (position.X > halfWorldSize) position.X = -halfWorldSize;
            else if (position.X < -halfWorldSize) position.X = halfWorldSize;

            if (position.Y > halfWorldSize) position.Y = -halfWorldSize;
            else if (position.Y < -halfWorldSize) position.Y = halfWorldSize;
        }

        

        /// <summary>
        /// check for own snake and other snakes, walls, powerups, and the edge of the world
        /// </summary>
        /// <param name="world">the world where everything exists</param>
        /// <param name="snake"> the snake we are checking</param>
        /// <returns></returns>
        private bool checkCollisionWalls(World world, Snake snake)
        {
            // Define padding values for the snake and walls since the vector is in the middle of the wall and head
            double snakePadding = 5;
            double wallPadding = 25;

            // Get the snake head position
            Vector2D snakeHead = snake.body.Last();
            double snakeHeadX = snakeHead.GetX();
            double snakeHeadY = snakeHead.GetY();

            // Check collision with each wall
            foreach (var wall in world.walls)
            {
                // Define the min and max points for the wall with padding
                double wallXmin = Math.Min(wall.Value.p1.GetX(), wall.Value.p2.GetX()) - wallPadding;
                double wallXmax = Math.Max(wall.Value.p1.GetX(), wall.Value.p2.GetX()) + wallPadding;
                double wallYmin = Math.Min(wall.Value.p1.GetY(), wall.Value.p2.GetY()) - wallPadding;
                double wallYmax = Math.Max(wall.Value.p1.GetY(), wall.Value.p2.GetY()) + wallPadding;

                // Check if the snake's head is within the wall bounds
                if (snakeHeadX + snakePadding >= wallXmin &&
                    snakeHeadX - snakePadding <= wallXmax &&
                    snakeHeadY + snakePadding >= wallYmin &&
                    snakeHeadY - snakePadding <= wallYmax)
                {

                    return true; // Collision detected
                }
            }
            return false; // No collision detected
        }

        //private bool checkCollisionPowerUp(Snake snakeCheck, World world)
        //{
        //    if (snakeCheck.died)
        //    {
        //        snakeCheck.alive = false;
        //        return false;
        //    }
        //    else
        //    {
        //        double snakePadding = 5;
        //        double powerupPadding = 8;

        //        foreach (var powerUp in world.powerups)
        //        {
        //            double snakeHeadX = snakeCheck.body.Last<Vector2D>().GetX();
        //            double snakeHeadY = snakeCheck.body.Last<Vector2D>().GetY();
        //            string snakeDirection = checkSnakeDirection(snakeCheck);

        //            switch (snakeDirection)
        //            {
        //                case "up":
        //                    snakeHeadY -= snakePadding;
        //                    break;
        //                case "right":
        //                    snakeHeadX += snakePadding;
        //                    break;
        //                case "down":
        //                    snakeHeadY += snakePadding;
        //                    break;
        //                case "left":
        //                    snakeHeadX -= snakePadding;
        //                    break;
        //                default:
        //                    Console.WriteLine("Adding snake padding error");
        //                    break;
        //            }

        //            if (snakeHeadX >= powerUp.Value.loc.GetX() - powerupPadding &&
        //                snakeHeadX <= powerUp.Value.loc.GetX() + powerupPadding &&
        //                snakeHeadY >= powerUp.Value.loc.GetY() - powerupPadding &&
        //                snakeHeadY <= powerUp.Value.loc.GetY() + powerupPadding
        //                )
        //            {
        //                powerUp.Value.died = true;
        //                powerUp.Value.ResetTimer();
        //                powerUp.Value.UpdateTimer();

        //                if (powerUp.Value.CanGenerateNewPowerup())
        //                {
        //                    Powerup newPowerUp = Powerup.GeneratePowerUp(powerUp.Value.power, world);

        //                    world.powerups.AddOrUpdate(
        //                        key: powerUp.Key,
        //                        addValue: newPowerUp,
        //                        updateValueFactory: (existingKey, existingValue) => newPowerUp
        //                    );
        //                }


        //                return true;
        //            }
        //        }
        //    }

        //    return false;
        //}

        /// <summary>
        /// check for own snake and other snakes, walls, powerups, and the edge of the world
        /// </summary>
        /// <param name="snakeCheck">Snake to check</param>
        /// <param name="world">World where everything exists</param>
        /// <returns></returns>
        private bool checkCollisionPowerUp(Snake snakeCheck, World world)
        {
            if (snakeCheck.died)
            {
                snakeCheck.alive = false;
                return false;
            }
            else
            {
                double snakePadding = 5;
                double powerupPadding = 8;

                Vector2D snakeHead = snakeCheck.body.Last<Vector2D>();
                Vector2D snakeDirection = getSnakeDirectionVector(snakeCheck);

                // Calculate the perpendicular direction to the snake's movement
                Vector2D perpendicularDirection = new Vector2D(-snakeDirection.Y, snakeDirection.X);

                foreach (var powerUp in world.powerups)
                {
                    Vector2D powerUpLocation = powerUp.Value.loc;

                    // Check if the power-up is within the padded area around the snake's head
                    if (isWithinPaddedArea(powerUpLocation, snakeHead, snakeDirection, perpendicularDirection, snakePadding, powerupPadding))
                    {
                        powerUp.Value.died = true;
                        powerUp.Value.ResetTimer();
                        powerUp.Value.UpdateTimer();
                        //If we have waited long enough, generate the powerup
                        if (powerUp.Value.CanGenerateNewPowerup())
                        {
                            Powerup newPowerUp = Powerup.GeneratePowerUp(powerUp.Value.power, world);
                            world.powerups.AddOrUpdate(
                            key: powerUp.Key,
                            addValue: newPowerUp,
                            updateValueFactory: (existingKey, existingValue) => newPowerUp
                            );
                        }
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Gets the snakes direction it is pointing
        /// </summary>
        /// <param name="snake">Snake to check which direciton it is pointing</param>
        /// <returns></returns>
        private Vector2D getSnakeDirectionVector(Snake snake)
        {
            switch (snake.dir.ToAngle())
            {
                case 0: return new Vector2D(0, -1); // Up
                case 90: return new Vector2D(1, 0); // Right
                case 180: return new Vector2D(0, 1); // Down
                case -90: return new Vector2D(-1, 0); // Left
                default: return new Vector2D(0, -1); // Default to Up if direction is unknown
            }
        }
        /// <summary>
        /// Calculates the padded area of our powerups, and returns true a point(center) is touching the padded area.
        /// </summary>
        /// <param name="point">Powerups location in the world</param>
        /// <param name="center">The snake's head</param>
        /// <param name="direction"> The snakes direction of the head</param>
        /// <param name="perpDirection">Perpendicular direction of our snakes head's direction</param>
        /// <param name="padding">Padding of the snakes head vector location, to make the snake's head detect outside of the center</param>
        /// <param name="powerupRadius">Padding of the powerup to allow easier detection of the powerup</param>
        /// <returns></returns>
        private bool isWithinPaddedArea(Vector2D point, Vector2D center, Vector2D direction, Vector2D perpDirection, double padding, double powerupRadius)
        {
            // Calculate the extended bounding box of the snake's head including padding
            Vector2D extendedMin = center - direction * padding - perpDirection * padding;
            Vector2D extendedMax = center + direction * padding + perpDirection * padding;

            // Check if the power-up (considering its radius) intersects with the extended bounding box
            return point.X + powerupRadius >= extendedMin.X && point.X - powerupRadius <= extendedMax.X &&
                   point.Y + powerupRadius >= extendedMin.Y && point.Y - powerupRadius <= extendedMax.Y;
        }

        /// <summary>
        /// Checking collsions with all snakes
        /// </summary>
        /// <param name="world">The world where everything exists</param>
        /// <param name="snakeCheck">This snake</param>
        /// <returns></returns>
        private bool checkCollisionSnakes(World world, Snake snakeCheck)
        {
            Vector2D snakeHead = snakeCheck.body.Last(); // The head is the last element in the body list

            foreach (Snake otherSnake in world.snakes.Values)
            {
                if (snakeCheck.died || !snakeCheck.alive)
                {
                    continue;
                }
                else
                {
                    // Determine the segments to check based on whether it's the same snake or a different one
                    int segmentsToCheck = (otherSnake.snake == snakeCheck.snake) ? otherSnake.body.Count - 1 : otherSnake.body.Count;

                    for (int i = 0; i < segmentsToCheck; i++)
                    {
                        Vector2D segment = otherSnake.body[i];

                        // Using a proximity check instead of exact coordinate match
                        if (IsCloseEnough(snakeHead, segment, 4.0)) // Threshold may need adjustment
                        {
                            snakeCheck.died = true;
                            snakeCheck.alive = false;
                            return true; // Collision detected
                        }
                    }
                }
               
            }
            return false; // No collision detected
        }
        /// <summary>
        /// Threshold to check if a snake's head is close to a segment of a snake, if it is close enough, it means
        /// a snake is colliding with some snake segment, either itself, or another snake, and returns true.
        /// </summary>
        /// <param name="snakeHead">Snake Head</param>
        /// <param name="snakeSegment">Segment of snake to check</param>
        /// <param name="threshold">Threshold that tells how close we need to be</param>
        /// <returns></returns>
        private bool IsCloseEnough(Vector2D snakeHead, Vector2D snakeSegment, double threshold =1.0)
        {
            double dx = snakeHead.GetX() - snakeSegment.GetX();
            double dy = snakeHead.GetY() - snakeSegment.GetY();
            return (dx * dx + dy * dy) <= (threshold * threshold);
        }



        private string checkSnakeDirection(Snake snake)
        {
            Vector2D direction = snake.dir;
            double directionX = direction.GetX();
            double directionY = direction.GetY();

            if (directionX == 0 && directionY == -1)
            { return "up"; }
            if (directionX == 1 && directionY == 0)
            { return "right"; }
            if (directionX == 0 && directionY == 1)
            { return "down"; }
            if (directionX == -1 && directionY == 0)
            { return "left"; }

            return "error";

        }


        /// <summary>
        /// change direction of the snake.
        /// </summary>
        /// <param name="newDirection">New direction the snake will be heading in</param>
        public void changeDirection(string newDirection)
        {
            Vector2D newDirVector = dir;

            switch (newDirection)
            {
                case "up": newDirVector = new Vector2D(0, -1); break;
                case "right": newDirVector = new Vector2D(1, 0); break;
                case "down": newDirVector = new Vector2D(0, 1); break;
                case "left": newDirVector = new Vector2D(-1, 0); break;
            }

            // Normalize directions to ensure they are valid unit vectors
            newDirVector.Normalize();
            dir.Normalize();

            // Check if the new direction is opposite to the current one
            if (!dir.IsOppositeCardinalDirection(newDirVector))
            {
                dir = newDirVector;
            }
        }

        /// <summary>
        /// Generate a snake in a new randomized  location
        /// </summary>
        /// <param name="state">Snake ID</param>
        /// <param name="name">Snake name</param>
        /// <param name="world">The world</param>
        /// <param name="snakeSpeed">Snake speed</param>
        /// <param name="snakeStartingLength">Snake starting length</param>
        /// <param name="snakeGrowth">Snake growth factor when powerup eaten</param>
        /// <returns>Returns a snake</returns>
        /// <exception cref="InvalidOperationException">Only Up, right, down, left</exception>
        public Snake GenerateSnake(int state, string name, World world, int snakeSpeed, int snakeStartingLength, int snakeGrowth)
        {
            Snake newSnake;
            Random random = new Random();
            double maxValue = (double)world.Size;

            //Do while loop to ensure that a snake is generated in an empty location.
            do
            {
                double randomDoubleX1 = (random.NextDouble() * maxValue) - maxValue / 2;
                double randomDoubleY1 = (random.NextDouble() * maxValue) - maxValue / 2;
                Vector2D newVetcor2DHead = new Vector2D(randomDoubleX1, randomDoubleY1);

                int randomDir = random.Next(1, 5);
                Vector2D randomDirection;
                double randomDoubleX2, randomDoubleY2;

                switch (randomDir)
                {
                    case 1:
                        randomDirection = new Vector2D(0, -1); // Up
                        randomDoubleX2 = randomDoubleX1;
                        randomDoubleY2 = randomDoubleY1 - snakeStartingLength;
                        break;
                    case 2:
                        randomDirection = new Vector2D(1, 0); // Right
                        randomDoubleX2 = randomDoubleX1 + snakeStartingLength;
                        randomDoubleY2 = randomDoubleY1;
                        break;
                    case 3:
                        randomDirection = new Vector2D(0, 1); // Down
                        randomDoubleX2 = randomDoubleX1;
                        randomDoubleY2 = randomDoubleY1 + snakeStartingLength;
                        break;
                    case 4:
                        randomDirection = new Vector2D(-1, 0); // Left
                        randomDoubleX2 = randomDoubleX1 - snakeStartingLength;
                        randomDoubleY2 = randomDoubleY1;
                        break;
                    default:
                        throw new InvalidOperationException("Invalid direction");
                }

                Vector2D newVetcor2DTail = new Vector2D(randomDoubleX2, randomDoubleY2);
                List<Vector2D> newBody = new List<Vector2D> { newVetcor2DHead, newVetcor2DTail };

                newSnake = new Snake
                {
                    snake = state,
                    name = name,
                    body = newBody,
                    dir = randomDirection,
                    score = 0,
                    died = false,
                    alive = true,
                    dc = false,
                    join = true,
                    SnakeSpeed = snakeSpeed,
                    SnakeStartingLength = snakeStartingLength,
                    SnakeGrowth = snakeGrowth
                };
            }
            //if we spawn in a bad location, keep generating snake in random location
            while (checkCollisionWalls(world, newSnake) || !IsWithinWorldBounds(newSnake, world.Size));

            return newSnake;//After a snake is safe to spawn in a location, return the snake
        }

        /// <summary>
        /// Helper method to check if a snake is within world boundaries specified by the world size
        /// set in the settings.xml
        /// </summary>
        /// <param name="snake">Snake to check </param>
        /// <param name="worldSize">World size</param>
        /// <returns></returns>
        private bool IsWithinWorldBounds(Snake snake, int worldSize)
        {
            int halfWorldSize = worldSize / 2;

            // Check if the head and tail are within the world boundaries
            bool isHeadInside = IsPointWithinBounds(snake.body.Last(), halfWorldSize);
            bool isTailInside = IsPointWithinBounds(snake.body.First(), halfWorldSize);

            return isHeadInside && isTailInside;
        }

        /// <summary>
        /// Helper method to check if a point is within world boundary
        /// </summary>
        /// <param name="point">Check if a vector point is withing world boundary</param>
        /// <param name="halfWorldSize">Half of the world size, since the world is centered at (0,0), it is half big in all directions since it is a square.</param>
        /// <returns></returns>
        private bool IsPointWithinBounds(Vector2D point, int halfWorldSize)
        {
            return point.GetX() >= -halfWorldSize && point.GetX() <= halfWorldSize &&
                   point.GetY() >= -halfWorldSize && point.GetY() <= halfWorldSize;
        }

        //public void Grow()
        //{
        //    // Increment the score for each power-up consumed
        //    score++;

        //    // Grow the snake by adding 'SnakeGrowth' segments at the tail
        //    for (int i = 0; i < SnakeGrowth; i++)
        //    {
        //        Vector2D tail = body.First();
        //        Vector2D newSegment = new Vector2D(tail.X - dir.X, tail.Y - dir.Y);

        //        // Add new segment at the tail
        //        body.Insert(0, newSegment);
        //    }
        //}
        //public void Grow()
        //{
        //    // Increment the score for each power-up consumed
        //    score++;

        //    // Calculate the length to grow
        //    double lengthToGrow = SnakeGrowth;
        //    double grownLength = 0;

        //    while (grownLength < lengthToGrow)
        //    {
        //        Vector2D tail = body.First();
        //        Vector2D newSegment = new Vector2D(tail.X - dir.X, tail.Y - dir.Y);

        //        // Calculate the length of the new segment
        //        double segmentLength = (newSegment - tail).Length();

        //        // Add new segment at the tail
        //        body.Insert(0, newSegment);

        //        // Update the total grown length
        //        grownLength += segmentLength;

        //        // If the growth exceeds the required length, adjust the last segment
        //        if (grownLength > lengthToGrow)
        //        {
        //            double excessLength = grownLength - lengthToGrow;
        //            AdjustLastSegment(excessLength);
        //        }
        //    }
        //}

        /// <summary>
        /// method to grow a snake once a snake eats a powerup
        /// </summary>
        public void Grow()
        {
            // Increment the score for each power-up consumed
            score++;

            // Determine the direction for growth based on the last two segments
            Vector2D tail = body.First();
            Vector2D secondLastSegment = body[1];
            Vector2D growthDirection = (tail - secondLastSegment);
            growthDirection.Normalize();
            // Add segments until the total length added equals SnakeGrowth
            double lengthAdded = 0;
            while (lengthAdded < SnakeGrowth)
            {
                // Calculate the new segment position
                Vector2D newSegment = tail + growthDirection;

                // Add the new segment at the beginning of the body list (tail end)
                body.Insert(0, newSegment);

                // Update the total length added
                lengthAdded += growthDirection.Length();

                // Update the tail reference for the next iteration
                tail = newSegment;
            }
        }

        /// <summary>
        /// Once enough time has passed, respawn a snake
        /// </summary>
        /// <param name="world"></param>
        public void UpdateRespawn(World world)
        {
            if (died && RespawnTimer <= 0)
            {
                RespawnTimer = world.RespawnRate; // Assuming this is in frames or milliseconds
            }
            else if (RespawnTimer > 0)
            {
                RespawnTimer--;

                if (RespawnTimer <= 0)
                {
                    Respawn(world);
                }
            }
        }

        /// <summary>
        /// Respawn a snake once it has died within the world
        /// </summary>
        /// <param name="world"></param>
        private void Respawn(World world)
        {
            // Resetting the basic properties of the snake
            this.score = 0;
            this.died = false;
            this.alive = true;

            // Try a set number of times to find a valid respawn position
            Random random = new Random();
            double maxValue = (double)world.Size;
            double randomDoubleX1 = (random.NextDouble() * maxValue) - maxValue / 2;
            double randomDoubleY1 = (random.NextDouble() * maxValue) - maxValue / 2;
            Vector2D newVetcor2DHead = new Vector2D(randomDoubleX1, randomDoubleY1);

            int randomDir = random.Next(1, 5);

            Vector2D randomDirection = new Vector2D(0, -1); //default
            double randomDoubleX2 = randomDoubleX1;
            double randomDoubleY2 = randomDoubleY1 - SnakeStartingLength;

            switch (randomDir)
            {
                case 1:
                    this.dir = new Vector2D(0, -1); // Up
                    randomDoubleX2 = randomDoubleX1;
                    randomDoubleY2 = randomDoubleY1 - SnakeStartingLength;
                    break;
                case 2:
                    this.dir = new Vector2D(1, 0);// Right
                    randomDoubleX2 = randomDoubleX1 + SnakeStartingLength;
                    randomDoubleY2 = randomDoubleY1;
                    break;
                case 3:
                    this.dir = new Vector2D(0, 1);// Down
                    randomDoubleX2 = randomDoubleX1;
                    randomDoubleY2 = randomDoubleY1 + SnakeStartingLength;
                    break;
                case 4:
                    this.dir = new Vector2D(-1, 0); // Left
                    randomDoubleX2 = randomDoubleX1 - SnakeStartingLength;
                    randomDoubleY2 = randomDoubleY1;
                    break;
                default:
                    break;
            }

            body.Clear();
            Vector2D newVetcor2DTail = new Vector2D(randomDoubleX2, randomDoubleY2);
            // List<Vector2D> newBody = new List<Vector2D>();
            body.Add(newVetcor2DHead);
            body.Add(newVetcor2DTail);

            // Check if the new position is valid
            if (!checkCollisionWalls(world, this) && !checkCollisionSnakes(world, this) && IsWithinWorldBounds(this, world.Size))
            {
                return; // Valid position found
            }
            else
            {
                Respawn(world);
            }
        }
    }
}
