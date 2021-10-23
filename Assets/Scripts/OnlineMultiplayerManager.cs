using System.Collections.Generic;
using UnityEngine;
using Mirror;

/*
	Documentation: https://mirror-networking.gitbook.io/docs/guides/networkbehaviour
	API Reference: https://mirror-networking.com/docs/api/Mirror.NetworkBehaviour.html
*/

// NOTE: Do not put objects in DontDestroyOnLoad (DDOL) in Awake.  You can do that in Start instead.

public class OnlineMultiplayerManager : NetworkBehaviour
{
    [SyncVar]
    public bool whiteMoved, blackMoved;  // whether white or black just did a move
    [SyncVar]
    public string lastMovedPiece;  // last piece that was moved by any player
    [SyncVar]
    public Vector3 lastMoveTargetPos;  // target position of last moved piece
    [SyncVar]
    public int joinedPlayers;  // number of joined players (range from 0 to 2)
    
    public string player;  // player associated with this client (server-client is "white", client-2 is "black")
    public GameObject textWaitForPlayer2;
    public GameObject textPlayer2Joined;
    public GameObject textPlayer2Disconnected;

    private void Update()
    {
        // only relevant for host player
        if ((textWaitForPlayer2.activeSelf || textPlayer2Disconnected.activeSelf) && joinedPlayers == 2)
        {
            // player2 joined -> update message
            textWaitForPlayer2.SetActive(false);
            textPlayer2Disconnected.SetActive(false);
            textPlayer2Joined.SetActive(true);
        }

        if (textPlayer2Joined.activeSelf && NetworkServer.connections.Count == 1)
        {
            // player2 leaved the game -> update message
            textPlayer2Joined.SetActive(false);
            textPlayer2Disconnected.SetActive(true);
            CmdClientDisconnected();
        }
    }

    public void OnlineMultiplayerReset()
    {
        whiteMoved = false;
        blackMoved = false;
        joinedPlayers = 0;
    }

    [Command(requiresAuthority = false)]
    public void CmdNewClientJoined()
    {
        joinedPlayers += 1;
    }

    [Command(requiresAuthority = false)]
    public void CmdClientDisconnected()
    {
        joinedPlayers -= 1;
    }

    [Command(requiresAuthority = false)]
    public void CmdSetMovedVariables(bool moveByWhite, bool moveByBlack)
    {
        whiteMoved = moveByWhite;
        blackMoved = moveByBlack;
    }

    [Command(requiresAuthority = false)]
    public void CmdTellServerLastMove(string piece, Vector3 targetPosition)
        // tells server which moved was just executed
    {
        lastMovedPiece = piece;
        lastMoveTargetPos = targetPosition;
    }


    #region Start & Stop Callbacks

    /// <summary>
    /// This is invoked for NetworkBehaviour objects when they become active on the server.
    /// <para>This could be triggered by NetworkServer.Listen() for objects in the scene, or by NetworkServer.Spawn() for objects that are dynamically created.</para>
    /// <para>This will be called for objects on a "host" as well as for object on a dedicated server.</para>
    /// </summary>
    public override void OnStartServer() 
    {
        OnlineMultiplayerReset();
    }

    /// <summary>
    /// Invoked on the server when the object is unspawned
    /// <para>Useful for saving object data in persistent storage</para>
    /// </summary>
    public override void OnStopServer() { }

    /// <summary>
    /// Called on every NetworkBehaviour when it is activated on a client.
    /// <para>Objects on the host have this function called, as there is a local client on the host. The values of SyncVars on object are guaranteed to be initialized correctly with the latest state from the server when this function is called on the client.</para>
    /// </summary>
    public override void OnStartClient() 
    {
        if (joinedPlayers == 0)
        {
            player = "white";
            CmdNewClientJoined(); //joinedPlayers += 1;
            textWaitForPlayer2.SetActive(true);  // message that player2 has not joined yet
        } else if (joinedPlayers == 1)
        {
            player = "black";
            CmdNewClientJoined(); // joinedPlayers += 1;
        }
    }

    /// <summary>
    /// This is invoked on clients when the server has caused this object to be destroyed.
    /// <para>This can be used as a hook to invoke effects or do client specific cleanup.</para>
    /// </summary>
    public override void OnStopClient() { }

    /// <summary>
    /// Called when the local player object has been set up.
    /// <para>This happens after OnStartClient(), as it is triggered by an ownership message from the server. This is an appropriate place to activate components or functionality that should only be active for the local player, such as cameras and input.</para>
    /// </summary>
    public override void OnStartLocalPlayer() { }

    /// <summary>
    /// This is invoked on behaviours that have authority, based on context and <see cref="NetworkIdentity.hasAuthority">NetworkIdentity.hasAuthority</see>.
    /// <para>This is called after <see cref="OnStartServer">OnStartServer</see> and before <see cref="OnStartClient">OnStartClient.</see></para>
    /// <para>When <see cref="NetworkIdentity.AssignClientAuthority">AssignClientAuthority</see> is called on the server, this will be called on the client that owns the object. When an object is spawned with <see cref="NetworkServer.Spawn">NetworkServer.Spawn</see> with a NetworkConnection parameter included, this will be called on the client that owns the object.</para>
    /// </summary>
    public override void OnStartAuthority() { }

    /// <summary>
    /// This is invoked on behaviours when authority is removed.
    /// <para>When NetworkIdentity.RemoveClientAuthority is called on the server, this will be called on the client that owns the object.</para>
    /// </summary>
    public override void OnStopAuthority() { }

    #endregion

    
}