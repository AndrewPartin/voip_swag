using Godot;
using System;
using System.Linq;

public partial class MultiplayerController : Control
{
	private ENetMultiplayerPeer peer;

	[Export] private int port = 6969;

	[Export] private string addr = "127.0.0.1";

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Multiplayer.PeerConnected += PeerConnected;
		Multiplayer.PeerDisconnected += PeerDisconnected;
		Multiplayer.ConnectedToServer += ConnectedToServer;
		Multiplayer.ConnectionFailed += ConnectionFailed;

		GetNode<ServerBrowser>("ServerBrowser").JoinGame += joinGame;
	}
	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	// runs only on client
    private void ConnectionFailed()
    {
        GD.Print("bitches b failin to connect");
    }

	// runs only on client
    private void ConnectedToServer()
    {
        GD.Print("bitches b connected to server");

		// send only to host
		RpcId(1, nameof(sendPlayerInfo), GetNode<LineEdit>("Username").Text, Multiplayer.GetUniqueId());
    }

	// runs on all peers
    private void PeerDisconnected(long id)
    {
        GD.Print("bitch disconnected: " + id.ToString());
		GameManager.Players.Remove(GameManager.Players.Where(i => i.Id == id).First<PlayerInfo>());
		
		// gotta be a better way to do this lol
		var players = GetTree().GetNodesInGroup("Player");
		foreach (var player in players) {
			if (player.Name == id.ToString()) {
				player.QueueFree();
			}
		}
    }

	// runs on all peers
    private void PeerConnected(long id)
    {
        GD.Print("bitch connected: " + id.ToString());

		// Node player = PlayerScene.Instantiate();
		// GetNode<Node>(SpawnLocation).AddChild(player);
		// player.Name = id.ToString();
		// player.GetNode<AudioManager>("AudioManager").SetupAudio(Convert.ToInt32(id));
	}

	private void _on_host_button_down()
	{
		peer = new ENetMultiplayerPeer();

		Error error = peer.CreateServer(port, 4);
		if (error != Error.Ok) {
			GD.Print("bitches b failin to host: " + error);
			return;
		}

		peer.Host.Compress(ENetConnection.CompressionMode.RangeCoder);

		Multiplayer.MultiplayerPeer = peer;
		GD.Print("waitin for bitches");

		GetNode<ServerBrowser>("ServerBrowser").SetupBroadcast(GetNode<LineEdit>("Username").Text + "'s server");

		sendPlayerInfo(GetNode<LineEdit>("Username").Text, 1);
	}
	private void _on_join_button_down()
	{
		joinGame(addr);
	}

	private void joinGame(string ip)
	{
		peer = new ENetMultiplayerPeer();
		peer.CreateClient(ip, port);
		peer.Host.Compress(ENetConnection.CompressionMode.RangeCoder);
		Multiplayer.MultiplayerPeer = peer;
	}

	private void _on_start_game_button_down()
	{
		Rpc(nameof(startGame));
	}

	[Rpc(mode: MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
	private void startGame()
	{
		GetNode<ServerBrowser>("ServerBrowser").CleanUp();

		var scene = ResourceLoader.Load<PackedScene>("res://test_world.tscn").Instantiate<Node3D>();
		GetTree().Root.AddChild(scene);
		this.Hide(); // hide ui
		// this.QueueFree();
	}

	[Rpc(mode: MultiplayerApi.RpcMode.AnyPeer)]
	private void sendPlayerInfo(string name, long id)
	{
		PlayerInfo playerInfo = new PlayerInfo(){
			Name = name,
			Id = id
		};

		if (!GameManager.Players.Contains(playerInfo)) {
			GameManager.Players.Add(playerInfo);
		}

		if (Multiplayer.IsServer()) {
			foreach (var player in GameManager.Players) {
				Rpc(nameof(sendPlayerInfo), player.Name, player.Id);
			}
		}
	}
}
