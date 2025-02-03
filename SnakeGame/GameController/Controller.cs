using Model;
using NetworkUtil;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace GameController;

/// <summary>
/// This class represents the controller of the game. 
/// </summary>
public class Controller
{
    /// <summary>
    /// The world that will hold all snakes, powers, and walls
    /// </summary>
    private World world;
    /// <summary>
    /// World size
    /// </summary>
    private int worldSize;
    /// <summary>
    /// Player snakes ID
    /// </summary>
    private int ownSnakeID;
    /// <summary>
    /// SocketState for movement
    /// </summary>
    private SocketState? theState;
    /// <summary>
    /// Event delegate to be subscribed to
    /// </summary>
    public delegate void GameUpdateHandler();
    /// <summary>
    /// Events to be handled by the MainPage
    /// </summary>
    public event GameUpdateHandler UpdateArrived = () => { };

    /// <summary>
    /// Initialize the world
    /// </summary>
    public Controller()
    {
        this.world = new();
    }

    /// <summary>
    /// Process the Json data received from the server, and then organize the deserialized information
    /// into our world via dictionaries of the objects we are decoding. Incomplete data is ignored.
    /// </summary>
    /// <param name="state"></param>
    public void ProcessMessages(SocketState state)
    {
        theState = state;
        string totalData = state.GetData();
        string[] parts = Regex.Split(totalData, @"(?<=[\n])");

        // get id and world size first, 
        // Loop until we have processed all messages
        lock (world)// Lock the world so that data is processed in an orderly manner.
        {
            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i].Length != 0)
                {
                    if (parts[i].EndsWith('\n'))
                    {
                        JsonDocument document = JsonDocument.Parse(parts[i]);
                        JsonElement root = document.RootElement;
                        if (root.ValueKind == JsonValueKind.Number)
                        {
                            //Check if the root is the snake ID and Worldsize
                            if (root.TryGetInt32(out int IdS))
                            {
                                try
                                {
                                    if (i == 0)
                                    {
                                        ownSnakeID = IdS;
                                    }
                                    if (i == 1)
                                    {
                                        worldSize = IdS;
                                        world = new World(ownSnakeID, worldSize);
                                    }
                                }
                                catch (Exception)
                                {
                                    throw new Exception("map size or snake id error");
                                }
                            }
                        }

                        //check if the root is a wall, try to update it, if it doesn't exist yet, then add it to the worlds dictionary.
                        else if (root.TryGetProperty("wall", out JsonElement w))
                        {
                            Wall newWall = JsonSerializer.Deserialize<Wall>(root)!;
                            int wallId = newWall.wall;
                            if (wallId >= 0)
                            {
                                world.walls.AddOrUpdate(wallId, newWall, (wallId, existingVal) =>
                                {
                                    existingVal = newWall;
                                    return existingVal;
                                });
                            }
                        }
                        //check if the root is a snake, try to update it, if it doesn't exist yet, then add it to the worlds dictionary.
                        else if (root.TryGetProperty("snake", out JsonElement s))
                        {
                            Snake newSnake = JsonSerializer.Deserialize<Snake>(root)!;
                            int snakeId = newSnake.snake;
                            if (snakeId >= 0)
                            {
                                world.snakes.AddOrUpdate(snakeId, newSnake, (snakeId, existingVal) =>
                                {
                                    existingVal = newSnake;
                                    return existingVal;
                                });
                            }

                            //Check if the snake is Disconnected, if it is, then remove the snake from the world.
                            if (root.TryGetProperty("dc", out JsonElement d))
                            {
                                if (d.GetBoolean() == true)
                                {
                                    if (world.snakes.TryRemove(snakeId, out Snake? value))
                                    { }
                                }
                            }
                        }

                        //check if the root is a powerup, try to update it, if it doesn't exist yet, then add it to the worlds dictionary.
                        else if (root.TryGetProperty("power", out JsonElement p))
                        {
                            Powerup newPower = JsonSerializer.Deserialize<Powerup>(root)!;
                            int powerId = newPower.power;
                            if (powerId >= 0)
                            {
                                world.powerups.AddOrUpdate(powerId, newPower, (powerId, existingVal) =>
                                {
                                    existingVal = newPower;
                                    return existingVal;
                                });

                                //Check if the powerup is eaten, if it is, then remove the powerup from the world.
                                if (root.TryGetProperty("died", out JsonElement d))
                                {
                                    if (d.GetBoolean() == true)
                                    {
                                        if (world.powerups.TryRemove(powerId, out Powerup? value))
                                        { }
                                    }
                                }
                            }
                        }

                        state.RemoveData(0, parts[i].Length);
                    }
                    else
                    {
                        return;
                    }
                }
            }
        }
        OnUpdateArrived();
    }

    /// <summary>
    /// Event handler to connect the controller to MainPage
    /// </summary>
    private void OnUpdateArrived()
    {
        UpdateArrived?.Invoke();
    }

    /// <summary>
    /// Getter for the world
    /// </summary>
    /// <returns></returns>
    public World GetWorld()
    {
        return world;
    }

    /// <summary>
    /// SocketState for movement
    /// </summary>
    /// <returns></returns>
    public SocketState? GetState()
    {
        return theState;
    }


}
