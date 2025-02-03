using GameController;
using NetworkUtil;

namespace SnakeGame;

/// <summary>
/// This class is a part of the view, and handles keyboard and button clicks from the view by
/// sending them to the Model, NetworkController, or GameController to handle the logic.
/// </summary>
public partial class MainPage : ContentPage
{
    /// <summary>
    /// Controller, represents the controller in MVC
    /// </summary>
    private readonly Controller controller;

    /// <summary>
    /// Constructor to initialize everything
    /// </summary>
    public MainPage()
    {
        InitializeComponent();
        graphicsView.Invalidate();
        controller = new Controller();
        controller.UpdateArrived += Controller_UpdateArrived;
        keyboardHack.IsEnabled = false;
    }

    /// <summary>
    /// We get the world from the controller, set the world,
    /// and then tell worldPanel it needs to draw something.
    /// </summary>
    private void Controller_UpdateArrived()
    {
        worldPanel.SetWorld(controller.GetWorld());
        OnFrame();
    }

    /// <summary>
    /// When the mouse is right clicked, this method refocuses the curser to the
    /// movement box.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="args"></param>
    void OnTapped(object sender, EventArgs args)
    {
        if (keyboardHack.IsEnabled)
        {
            keyboardHack.Focus();
        }
        else
        {
            // If keyboardHack is not enabled, it means the game area is clicked,
            // so enable keyboardHack and focus on it.
            keyboardHack.IsEnabled = true;
            keyboardHack.Focus();
        }
    }

    /// <summary>
    /// Handles the movement sending requests. W moves up, a moves left, s moves down,
    /// and d moves right
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="args"></param>
    void OnTextChanged(object sender, TextChangedEventArgs args)
    {
        Entry entry = (Entry)sender;
        String text = entry.Text.ToLower();
        int snake = controller.GetWorld().OwnSnakeID;
        bool snakeLive = controller.GetWorld().snakes.ContainsKey(snake);
        if (snakeLive)
        {
            if (text == "w")
            {
                Networking.Send(controller.GetState().TheSocket, "{ \"moving\":\"up\"}" + "\n");
            }
            else if (text == "a")
            {
                Networking.Send(controller.GetState().TheSocket, "{ \"moving\":\"left\"}" + "\n");
            }
            else if (text == "s")
            {
                Networking.Send(controller.GetState().TheSocket, "{ \"moving\":\"down\"}" + "\n");
            }
            else if (text == "d")
            {
                Networking.Send(controller.GetState().TheSocket, "{ \"moving\":\"right\"}" + "\n");
            }
        }

        entry.Text = "";
    }

    /// <summary>
    /// If there was an error, allows client to rety connecting to a server
    /// </summary>
    private void NetworkErrorHandler()
    {
        //Must add dispatcher for front end displays to set on main thread
        Dispatcher.Dispatch(() =>
        {
            DisplayAlert("Error", "Disconnected from server", "OK");
            // Enable UI elements for reconnection
            serverText.IsEnabled = true;
            nameText.IsEnabled = true;
            connectButton.IsEnabled = true;
            keyboardHack.IsEnabled = false;
        });
    }


    /// <summary>
    /// Event handler for the connect button
    /// We will put the connection attempt interface here in the view.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="args"></param>
    private void ConnectClick(object sender, EventArgs args)
    {
        if (serverText.Text == "")
        {
            DisplayAlert("Error", "Please enter a server address", "OK");
            return;
        }
        if (nameText.Text == "")
        {
            DisplayAlert("Error", "Please enter a name", "OK");
            return;
        }
        if (nameText.Text.Length > 16)
        {
            DisplayAlert("Error", "Name must be less than 16 characters", "OK");
            return;
        }
        //Once connected, disable server connection options and enable movement text box.
        serverText.IsEnabled = false;
        nameText.IsEnabled = false;
        keyboardHack.IsEnabled = true;
        Networking.ConnectToServer(OnConnect, serverText.Text, 11000);

        keyboardHack.Focus();

    }

    /// <summary>
    /// Callback for ConnectClick. Handles sending and receiving data.
    /// </summary>
    /// <param name="state"></param>
    private void OnConnect(SocketState state)
    {
        if (state.ErrorOccurred)
        {
            NetworkErrorHandler();
            return;
        }

        Networking.GetData(state);
        Networking.Send(state.TheSocket, nameText.Text + "\n");
        state.OnNetworkAction = ReceiveMessage;

        // Disable UI elements during connection attempt
        Dispatcher.Dispatch(() =>
        {
            serverText.IsEnabled = false;
            nameText.IsEnabled = false;
            connectButton.IsEnabled = false;
            keyboardHack.Focus();
        });


    }
    /// <summary>
    /// Handles logic for processing data
    /// </summary>
    /// <param name="state"></param>
    private void ReceiveMessage(SocketState state)
    {
        if (state.ErrorOccurred)
        {
            NetworkErrorHandler();
            return;
        }

        controller.ProcessMessages(state);
        Networking.GetData(state);

    }

    /// <summary>
    /// Use this method as an event handler for when the controller has updated the world
    /// </summary>
    public void OnFrame()
    {
        Dispatcher.Dispatch(() => graphicsView.Invalidate());
    }

    /// <summary>
    /// Display that instructs players how to move
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void ControlsButton_Clicked(object sender, EventArgs e)
    {
        DisplayAlert("Controls",
                     "W:\t\t Move up\n" +
                     "A:\t\t Move left\n" +
                     "S:\t\t Move down\n" +
                     "D:\t\t Move right\n",
                     "OK");
    }

    /// <summary>
    /// Display that tells 
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void AboutButton_Clicked(object sender, EventArgs e)
    {
        DisplayAlert("About",
      "SnakeGame solution\nArtwork by Jolie Uk and Alex Smith\nGame design by Daniel Kopta and Travis Martin\n" +
      "Implementation by Jared Hildt and Qi Cao\n" +
        "CS 3500 Fall 2023, University of Utah", "OK");
    }
    /// <summary>
    /// If we are connected to a game, focus on movement typing box.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void ContentPage_Focused(object sender, FocusEventArgs e)
    {
        if (!connectButton.IsEnabled)
            keyboardHack.Focus();
    }

}