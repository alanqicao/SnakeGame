# SnakeGame

## Description

This project is a multiplayer Snake game implemented using .NET MAUI for the user interface. The game allows
multiple players to control their snakes and compete in a shared game world. The project is designed to showcase
networking, graphical rendering, and user interaction in the context of a classic multiplayer game.

### Features

- Multiplayer Snake game with a shared game world.
- Real-time updates and synchronization between connected players.
- Graphical rendering of snakes, walls, and power-ups.
- Graphical rendering of snake name and score is below the players head.
- Graphical rendering of powerups are slightly more complicated than one circle...its two circles
- Graphical rendering of snakes when they reach a boundry of the world that is not covered by a wall, it will appear on the opposite side of the world.
- User-friendly controls for snake movement.
- Death Explosion feature: When a snake dies, it will become transparent, and then and explosion animation will play where the snakes head was.
- Connection features: When disconnected, the client may try to reconnect without restarting the application.
- When the game is in play, all clicks will be refocused to the movement text box.
    While playing, you won't be able to click connect, help, or about, and you won't be able to change servers, or change names while in play. All that
    must be done before connecting to a valid server.

**Known Issues:**

1. If you click two directions at the same time, the snake might move inside itself in the opposite direction. This is a server issue since when moving inside yourself, the snake does not die.
2. When spawning in a space close to a border with no walls, if a part of the snake spawns in the void, it will draw a line from the head, wrapped around the whole world to its tail, leaving a permanent line.
This can lead to collisions and should not happen. This is a server issue; ensure snakes spawn fully in the world to avoid this issue.


## Getting Started

### Dependencies

* .NET MAUI framework
* Microsoft Visual Studio or Visual Studio for Mac

### Installing

1. Clone this repository to your local machine.

2. Be sure that SpreadsheetGUI is the start-up project (right click it -> Set as startup project).

3. Open the project in Microsoft Visual Studio or Visual Studio for Mac.

4. Build the solution to ensure all dependencies are restored.


### Executing program

1. Make sure a snake game server is open that you can connect to, whether it is your own local server, or a server someone else is hosting

2. Build and run the project in your preferred development environment (Visual Studio or Visual Studio for Mac).

3. Launch the Snake game application! This application allows multiple players to connect, control their snakes, and compete in the game world.

### How to Play

1. Launch the application.
2. Enter the server address and your name.
3. Click the "Connect" button to join the game.
4. Control your snake using the keyboard:
   - W: Move up
   - A: Move left
   - S: Move down
   - D: Move right
5. Avoid collisions with walls and other snakes.
6. Collect power-ups to gain advantages and get bigger.
7. Have fun and compete with other players!

## Authors

* Jared Hildt
* Qi Cao

## Version History

* 1.0
    * Initial Release

## License

This project is licensed under the [university of utah]

## Acknowledgments

Special thanks to Jolie Uk and Alex Smith for the artwork, Daniel Kopta and Travis Martin for designing the game, class TA's
and finally thanks to Piazza Q & A for providing helpful insights and answers during the development of this project.
