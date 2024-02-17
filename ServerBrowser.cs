using Godot;
using System;
using System.Linq;
using System.Text.Json;

public partial class ServerBrowser : Control
{
	[Export] PacketPeerUdp broadcaster;
	[Export] PacketPeerUdp listener = new PacketPeerUdp();
	[Export] int listenPort = 6970;
	[Export] int hostPort = 6971;
	[Export] string broadcastAddr = "10.10.31.255"; // fo UC secure

	[Export] PackedScene ServerInfo;

	[Signal] public delegate void JoinGameEventHandler(string ip);

	Timer broadcastTimer;
	ServerInfo serverInfo;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		broadcastTimer = GetNode<Timer>("BroadcastTimer");
		setupListener();
	}

	private void setupListener()
	{
		Error error = listener.Bind(listenPort);

		if (error != Error.Ok) {
			GD.Print("bitches b failin to bind to listen port " + listenPort.ToString());
			GetNode<Label>("Debug").Text = "Bound to Listen Port: false (host)";
		} else {
			GD.Print("bitches b bound to listen port " + listenPort.ToString());
			GetNode<Label>("Debug").Text = "Bound to Listen Port: true";
		}
	}

	public void SetupBroadcast(string name)
	{
		broadcaster = new PacketPeerUdp();
		serverInfo = new ServerInfo() {
			Name = name,
			PlayerCount = GameManager.Players.Count
		};

		broadcaster.SetBroadcastEnabled(true);
		broadcaster.SetDestAddress(broadcastAddr, listenPort);

		Error error = broadcaster.Bind(hostPort);

		if (error != Error.Ok) {
			GD.Print("bitches b failin to bind to broadcast port " + hostPort.ToString());
		} else {
			GD.Print("bitches b bound to broadcast port " + hostPort.ToString());
		}

		broadcastTimer.Start();
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (listener.GetAvailablePacketCount() > 0) {
			string serverIP = listener.GetPacketIP();
			int serverPort = listener.GetPacketPort();
			byte[] packetBytes = listener.GetPacket();
			ServerInfo info = JsonSerializer.Deserialize<ServerInfo>(packetBytes.GetStringFromUtf8());
			GD.Print("detitated wam: " + serverIP + ":" + serverPort.ToString() + " " + packetBytes.GetStringFromUtf8());

			Node currentNode = GetNode<VBoxContainer>("Panel/VBoxContainer").GetChildren().Where(x => x.Name == info.Name).FirstOrDefault();

			if (currentNode != null) {
				currentNode.GetNode<Label>("PlayerCount").Text = info.PlayerCount.ToString();
				currentNode.GetNode<Label>("IP").Text = serverIP; // doesnt need to update every time, but not sent with first TODO: fix lol
				return;
			}
			
			ServerBrowserInfoLine serverInfo = ServerInfo.Instantiate<ServerBrowserInfoLine>();
			serverInfo.Name = info.Name;
			serverInfo.GetNode<Label>("Name").Text = serverInfo.Name;
			serverInfo.GetNode<Label>("IP").Text = serverIP;
			serverInfo.GetNode<Label>("PlayerCount").Text = info.PlayerCount.ToString();
			GetNode<VBoxContainer>("Panel/VBoxContainer").AddChild(serverInfo);

			serverInfo.JoinGame += _on_join_game;
		}
	}

	private void _on_join_game(string ip)
	{
		EmitSignal(SignalName.JoinGame, ip);
	}

	private void _on_broadcast_timer_timeout()
	{
		GD.Print("B CASTING!!!");
		serverInfo.PlayerCount = GameManager.Players.Count;

		string json = JsonSerializer.Serialize(serverInfo);
		var packet = json.ToUtf8Buffer();

		broadcaster.PutPacket(packet);
	}

	public void CleanUp()
	{
		listener.Close();
		broadcastTimer.Stop();
		if (broadcaster != null) {
			broadcaster.Close();
		}
	}
}
