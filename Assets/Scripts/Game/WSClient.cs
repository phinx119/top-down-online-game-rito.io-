using System;
using System.Collections;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class WSClient : MonoBehaviour
{
    private ClientWebSocket websocket;
    public string url = "wss://websocket-server-kutx.onrender.com";
    public float sendInterval = 0.1f;
    public GameObject player;
    public string playerId;
    public GameObject playerEnemyPrefab;

    async void Start()
    {
        string inputUrl = MainMenu.serverUrl;
        player = GameObject.FindGameObjectWithTag("Player");
        playerId = MainMenu.id;
        if (!String.IsNullOrEmpty(inputUrl))
            url = inputUrl;

        websocket = new ClientWebSocket();
        Uri serverUri = new Uri(url);

        try
        {
            await websocket.ConnectAsync(serverUri, CancellationToken.None);
            Debug.Log("Connected to WebSocket server");

            // Start receiving messages
            _ = ReceiveMessages();

            // Start sending player position at regular intervals
            StartCoroutine(SendPlayerPositionAtIntervals());
        }
        catch (WebSocketException e)
        {
            Debug.LogError($"WebSocketException: {e.Message}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Exception: {e.Message}");
        }
    }

    async Task SendMessage(string message)
    {
        var encodedMessage = Encoding.UTF8.GetBytes(message);
        var buffer = new ArraySegment<byte>(encodedMessage);

        try
        {
            await websocket.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
            Debug.Log("Message sent: " + message);
        }
        catch (Exception e)
        {
            Debug.LogError($"Exception while sending message: {e.Message}");
        }
    }

    async Task ReceiveMessages()
    {
        var buffer = new byte[1024];

        try
        {
            while (websocket.State == WebSocketState.Open)
            {
                var result = await websocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    Debug.Log("WebSocket closed by the server");
                    await websocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
                }
                else
                {
                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    Debug.Log("Message received: " + message);
                    HandleIncomingMessage(message);
                }
            }
        }
        catch (WebSocketException e)
        {
            Debug.LogError($"WebSocketException while receiving messages: {e.Message}");
        }
        catch (OperationCanceledException e)
        {
            Debug.LogError($"OperationCanceledException while receiving messages: {e.Message}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Exception while receiving messages: {e.Message}");
        }
    }

    private void HandleIncomingMessage(string message)
    {
        try
        {
            // Parse the incoming JSON message
            var playersData = JsonUtility.FromJson<PlayersData>(message);

            // Iterate through each player data in the JSON
            foreach (var playerData in playersData.players)
            {
                string playerId = playerData.id;
                Vector3 position = new Vector3(playerData.content.x, playerData.content.y, playerData.content.z);

                // Check if the player with this id already exists
                GameObject playerObject = GameObject.Find(playerId);

                if (playerObject == null)
                {
                    // If the player does not exist, instantiate a new player
                    GameObject newPlayer = Instantiate(playerEnemyPrefab, position, Quaternion.identity);
                    newPlayer.name = playerId; // Set the player's name to their id
                }
                else
                {
                    // If the player exists, update its position
                    playerObject.transform.position = position;
                }
            }
        } catch (Exception e)
        {
            Debug.LogError($"Exception while handling incoming messages: {e.Message}");
        }
    }



    private Vector3? ParsePosition(string xString, string yString, string zString)
    {
        float x = float.Parse(xString);
        float y = float.Parse(yString);
        float z = float.Parse(zString);

        return new Vector3(x, y, z);
    }

    private Vector3? ParsePosition(PlayerContent playerContent)
    {
        return new Vector3(playerContent.x, playerContent.y, playerContent.z);
    }

    public async void SendPlayerPosition()
    {
        /*if (player != null)
        {
            var position = player.transform.position;
            var messageObject = new PlayerPositionMessage
            {
                id = playerId,
                content = new PlayerContent { x = position.x, y = position.y, z = position.z }
            };

            var jsonMessage = JsonUtility.ToJson(messageObject);

            await SendMessage(jsonMessage);
        }*/
    }



    private IEnumerator SendPlayerPositionAtIntervals()
    {
        while (true)
        {
            SendPlayerPosition();
            yield return new WaitForSeconds(sendInterval);
        }
    }

    private void OnApplicationQuit()
    {
        if (websocket != null)
        {
            websocket.Abort();
        }
    }

    [Serializable]
    public class PlayersData
    {
        public PlayerData[] players;
    }

    [Serializable]
    public class PlayerData
    {
        public string id;
        public PlayerContent content;
    }

    [Serializable]
    public class PlayerContent
    {
        public float x;
        public float y;
        public float z;
    }

}