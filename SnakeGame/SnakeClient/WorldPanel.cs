using IImage = Microsoft.Maui.Graphics.IImage;
#if MACCATALYST
using Microsoft.Maui.Graphics.Platform;
#else
using Microsoft.Maui.Graphics.Win2D;
#endif
using Color = Microsoft.Maui.Graphics.Color;
using System.Reflection;
using Font = Microsoft.Maui.Graphics.Font;
using SizeF = Microsoft.Maui.Graphics.SizeF;
using Model;

namespace SnakeGame;
/// <summary>
/// This class represents the view of the world state. All drawing logic for the world
/// is handled here
/// </summary>
public class WorldPanel : IDrawable
{
    /// <summary>
    /// A delegate for DrawObjectWithTransform
    /// Methods matching this delegate can draw whatever they want onto the canvas 
    /// </summary>
    /// <param name="o"></param>
    /// <param name="canvas"></param>
    public delegate void ObjectDrawer(object o, ICanvas canvas);

    /// <summary>
    /// View size of the window for players
    /// </summary>
    private readonly int viewSize = 900;

    /// <summary>
    /// The world where everything inside the game is contained
    /// </summary>
    private World theWorld;

    /// <summary>
    /// Wall Image
    /// </summary>
    private IImage wall;
    /// <summary>
    /// Background Image
    /// </summary>
    private IImage background;
    /// <summary>
    /// Ensure the images are loaded and ready to be used
    /// </summary>
    private bool initializedForDrawing = false;

    /// <summary>
    /// Explotion field
    /// </summary>
    private bool isExploding = false;
    private float explosionRadius = 0;
    private const float maxExplosionRadius = 50; // Maximum size of the explosion
    private Vector2D explosionLocation;

    /// <summary>
    /// Private helper method to load images from the resources folder in the view solution
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    private IImage LoadImage(string name)
    {
        Assembly assembly = GetType().GetTypeInfo().Assembly;
        string path = "SnakeClient.Resources.Images";
        using (Stream stream = assembly.GetManifestResourceStream($"{path}.{name}"))
        {
#if MACCATALYST
            return PlatformImage.FromStream(stream);
#else
            return new W2DImageLoadingService().FromStream(stream);
#endif
        }
    }

    /// <summary>
    /// Initialize the world
    /// </summary>
    public WorldPanel()
    {
        theWorld = new World();
    }

    /// <summary>
    /// Initialize assets for drawing by loading outside assets
    /// </summary>
    private void InitializeDrawing()
    {
        wall = LoadImage("wallsprite.png");
        background = LoadImage("background.png");
        initializedForDrawing = true;
    }

    /// <summary>
    /// Draw method: Contains all logic and power when it comes to drawing the state of the world
    /// Ensures that the world is locked so that it draws things one at a time.
    /// </summary>
    /// <param name="canvas"></param>
    /// <param name="dirtyRect"></param>
    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        int count = 0;
        if (!initializedForDrawing)
            InitializeDrawing();

        // undo previous transformations from last frame
        canvas.ResetState();

        lock (theWorld)
        {
            int ownSnakeID = theWorld.OwnSnakeID;
            float playerX = 0;
            float playerY = 0;
            if (theWorld.snakes.TryGetValue(ownSnakeID, out Snake s))
            {
                Vector2D snakeHead = s.body.Last<Vector2D>();
                playerX = Convert.ToSingle(snakeHead.GetX()); //(the player's world-space X coordinate)
                playerY = Convert.ToSingle(snakeHead.GetY()); //(the player's world-space Y coordinate)
            }

            //Player View
            canvas.Translate(-playerX + (viewSize / 2), -playerY + (viewSize / 2));
            //Background Image
            canvas.DrawImage(background, -theWorld.Size / 2, -theWorld.Size / 2, theWorld.Size, theWorld.Size);

            // Powerup Drawing logic
            foreach (var p in theWorld.powerups.Values)
            {
                DrawObjectWithTransform(canvas, p,
                  p.loc.GetX(), p.loc.GetY(), 0,
                  PowerupDrawer);
            }

            //Wall Drawing logic
            foreach (var p in theWorld.walls.Values)
            {

                // Draw the first wall
                DrawObjectWithTransform(canvas, p, p.p1.GetX(), p.p1.GetY(), 0, WallDrawer);

                // Calculate the direction vector from p1 to p2: Calculate X or Y distance between p1 and p2 (Walls only left to right OR up to down)
                Vector2D direction = p.p2 - p.p1;

                // Calculate the distance between p1 and p2
                double distance = direction.Length();

                // Normalize the direction vector
                direction.Normalize();

                // Walls 50X50 pixels apart
                double wallSpacing = 50;

                // Calculate the number of walls needed to fill the distance between p1 and p2
                int numberOfWalls = (int)(distance / wallSpacing);

                // Calculate the increment values for each step
                double newX = direction.GetX() * wallSpacing;
                double newY = direction.GetY() * wallSpacing;

                // Draw the remaining walls between p1 and p2
                for (int i = 1; i < numberOfWalls; i++)
                {
                    double nextWallX = p.p1.GetX() + i * newX;
                    double nextWallY = p.p1.GetY() + i * newY;

                    DrawObjectWithTransform(canvas, p, nextWallX, nextWallY, 0, WallDrawer);
                }

                // Draw the last wall
                DrawObjectWithTransform(canvas, p, p.p2.GetX(), p.p2.GetY(), 0, WallDrawer);
            }

            //if snake turns, seperate the snake into segments
            foreach (var snake in theWorld.snakes.Values)
            {
                //Color choices
                Color color = Colors.Red;
                Color[] colorArray = new Color[]
                {
                    Colors.Red,
                    Colors.Green,
                    Colors.Blue,
                    Colors.Yellow,
                    Colors.Orange,
                    Colors.Purple,
                    Colors.Pink,
                    Colors.Black,
                    Colors.Brown,
                    Colors.Gray
                };

                //Color choosing logic
                if (count < colorArray.Length - 1)
                {
                    count++;
                }
                else
                {
                    count = 0;
                }
                color = colorArray[count];

                // Draw the snake's head
                Vector2D snakeHead = snake.body.Last();
                if (snake.died || !snake.alive)
                {
                    isExploding = true;
                    explosionLocation = new Vector2D(snakeHead.GetX(), snakeHead.GetY());
                    UpdateExplosion();
                    DrawExplosion(canvas);
                    DrawSnake(canvas, Colors.Transparent, snakeHead, snake);
                }
                else
                {
                    DrawSnake(canvas, color, snakeHead, snake);
                }
            }
        }
    }

    private void DrawSnake(ICanvas canvas, Color color, Vector2D snakeHead, Snake snake)
    {
        DrawObjectWithTransform(canvas, color, snakeHead.GetX(), snakeHead.GetY(), snake.dir.ToAngle(), SnakeHeadDrawer);
        DrawObjectWithTransform(canvas, snake, snakeHead.GetX() + 5, snakeHead.GetY() + 5, 0, SnakeNameDrawer);
        // Draw the snake's body segments
        for (int i = 0; i < snake.body.Count - 1; i++)
        {
            Vector2D segment = snake.body[i];
            Vector2D direction = snake.body[i + 1] - segment;
            double distance = direction.Length();
            if (distance >= theWorld.Size)
                continue;

            TupleSnake tuple = new TupleSnake(color, distance);
            direction.Normalize();

            DrawObjectWithTransform(canvas, tuple, segment.GetX(), segment.GetY(), direction.ToAngle(), SnakeDrawer);
        }
        Vector2D snakeTail = snake.body.First();
        DrawObjectWithTransform(canvas, color, snakeTail.GetX(), snakeTail.GetY(), snake.dir.ToAngle(), SnakeHeadDrawer);
    }

    /// <summary>
    /// This method performs a translation and rotation to draw an object.
    /// </summary>
    /// <param name="canvas">The canvas object for drawing onto</param>
    /// <param name="o">The object to draw</param>
    /// <param name="worldX">The X component of the object's position in world space</param>
    /// <param name="worldY">The Y component of the object's position in world space</param>
    /// <param name="angle">The orientation of the object, measured in degrees clockwise from "up"</param>
    /// <param name="drawer">The drawer delegate. After the transformation is applied, the delegate is invoked to draw whatever it wants</param>
    private void DrawObjectWithTransform(ICanvas canvas, object o, double worldX, double worldY, double angle, ObjectDrawer drawer)
    {
        // "push" the current transform
        canvas.SaveState();

        canvas.Translate((float)worldX, (float)worldY);
        canvas.Rotate((float)angle);
        drawer(o, canvas);

        // "pop" the transform
        canvas.RestoreState();
    }

    /// <summary>
    /// Setter for the world
    /// </summary>
    /// <param name="w"></param>
    public void SetWorld(World w)
    {
        theWorld = w;
    }

    /// <summary>
    /// A method that can be used as an ObjectDrawer delegate
    /// Contains logic for how a wall should be drawn.
    /// </summary>
    /// <param name="o">The wall to draw</param>
    /// <param name="canvas"></param>
    private void WallDrawer(object o, ICanvas canvas)
    {
        float w = wall.Width;
        float h = wall.Height;
        canvas.DrawImage(wall, -w / 2, -h / 2, w, h);
    }

    /// <summary>
    /// A method that can be used as an ObjectDrawer delegate
    /// Contains logic for how a powerup should be drawn, and what it looks like
    /// </summary>
    /// <param name="o">The powerup to draw</param>
    /// <param name="canvas"></param>
    private void PowerupDrawer(object o, ICanvas canvas)
    {
        int width = 16;
        canvas.FillColor = Colors.Orange;
        canvas.FillEllipse(-(width / 2), -(width / 2), width, width);

        canvas.FillColor = Colors.Blue; // You can change this to your desired color
        int innerWidth = width / 2; // Inner shape size, adjust as needed
        canvas.FillEllipse(-(innerWidth / 2), -(innerWidth / 2), innerWidth, innerWidth);
    }

    /// <summary>
    /// A method that can be used as an ObjectDrawer delegate
    /// Contains logic for how a snakes head is drawn
    /// </summary>
    /// <param name="o"></param>
    /// <param name="canvas"></param>
    private void SnakeHeadDrawer(object o, ICanvas canvas)
    {
        Color c = o as Color;
        int width = 10;
        canvas.FillColor = c;
        canvas.FillEllipse(-(width / 2), -(width / 2), width, width);
    }

    /// <summary>
    /// A method that can be used as an ObjectDrawer delegate
    /// Contains logic for how a snakes segments are drawn.
    /// </summary>
    /// <param name="o">The snake to draw</param>
    /// <param name="canvas"></param>
    private void SnakeDrawer(object o, ICanvas canvas)
    {
        TupleSnake tuple = o as TupleSnake;
        canvas.StrokeColor = tuple.ReturnColor();
        float length2 = Convert.ToSingle(tuple.Returndist());
        canvas.StrokeLineCap = LineCap.Round;
        canvas.StrokeSize = 10;
        canvas.DrawLine(0, 0, 0, -length2);
    }

    /// <summary>
    /// Helper method for drawing the players name under the snakes head
    /// </summary>
    /// <param name="o"></param>
    /// <param name="canvas"></param>
    private void SnakeNameDrawer(object o, ICanvas canvas)
    {
        Snake snake = (Snake)o;

        canvas.Font = new Font("Arial");
        canvas.FontColor = Colors.White;
        canvas.SetShadow(new SizeF(6, 6), 4, Colors.Gray);
        canvas.DrawString(snake.name + ": " + snake.score, 0, 0, 300, 100, HorizontalAlignment.Left, VerticalAlignment.Top);

    }
    private void UpdateExplosion()
    {
        if (isExploding)
        {
            explosionRadius += 2; // Increase the radius each frame
            if (explosionRadius > maxExplosionRadius)
            {
                isExploding = false; // End the explosion
                explosionRadius = 0;
            }
        }
    }

    private void DrawExplosion(ICanvas canvas)
    {
        if (isExploding)
        {
            canvas.SaveState();

            canvas.Translate((float)explosionLocation.GetX(), (float)explosionLocation.GetY());
            canvas.FillColor = Colors.White;
            canvas.FillEllipse(-explosionRadius / 2, -explosionRadius / 2, explosionRadius, explosionRadius);

            canvas.RestoreState();
        }
    }
    /// <summary>
    /// Tuple to allow us to send in a color and a distance to Drawer helper methods
    /// </summary>
    internal class TupleSnake
    {
        /// <summary>
        /// Color of the snake.
        /// </summary>
        private readonly Color Color;
        /// <summary>
        /// Distance to draw for a snakes segment
        /// </summary>
        private readonly double dist;
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="ToCall"></param>
        /// <param name="listener"></param>
        public TupleSnake(Color Color, double dist)
        {
            this.Color = Color;
            this.dist = dist;
        }

        /// <summary>
        /// Getter for color
        /// </summary>
        /// <returns></returns>
        public Color ReturnColor()
        {
            return Color;
        }

        /// <summary>
        /// Getter for distance
        /// </summary>
        /// <returns></returns>
        public double Returndist()
        {
            return dist;
        }
    }
}
