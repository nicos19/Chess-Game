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
    [SyncVar]
    public bool pawnPromotion;  // whether last move resulted in pawn promotion or not
    [SyncVar]
    public string pawnPromotionResult = "";  // to which piece was a pawn promoted? ("Queen", "Rook", "Bishop" or "Knight")
    [SyncVar]
    public string savegameOfHost = "savegameOfHost";  // string representation of the host's savegame
    [SyncVar]
    public string savegameOfPlayer2 = "savegameOfPlayer2";  // string representation of player2's savegame
    [SyncVar]
    public bool checkSavegameSync;  // whether the synchronisation of host's/player2's savegames shall be checked
    [SyncVar]
    public bool isLoadedGame;  // whether the host loaded a game or started a new one
    [SyncVar]
    public bool player2ShallSave;  // true: tells player2 that he shall save and disconnect afterwards
    [SyncVar]
    public bool activePawnPromotion;  // whether any player has an open pawn promotion menu
    [SyncVar]
    public bool newGameReady;  // true if a new game is ready for playing for both players
    [SyncVar]
    public bool loadedGameReady;  // true if a loaded game is ready for playing for both players
    
    public string player;  // player associated with this client (server-client/host is "white", client-2/player2 is "black")
    public bool isHost;  // is the client also server and therefore host of the game?
    public GameObject textWaitForPlayer2;
    public GameObject textPlayer2Joined;
    public GameObject textPlayer2Disconnected;
    public GameObject textJoinedSuccessfully;
    public GameObject asynSavegamesErrorScreen;
    public GameObject board;
    public GameObject buttonControllerObject;
    public ButtonController buttonController;

    private void Start()
    {
        buttonController = buttonControllerObject.GetComponent<ButtonController>();
    }

    private void Update()
    {
        // only relevant for host player
        if ((textWaitForPlayer2.activeSelf) && NetworkServer.connections.Count == 2 && (newGameReady || loadedGameReady))
        {
            // player2 joined (and game is ready for playing) -> update message
            textWaitForPlayer2.SetActive(false);
            textPlayer2Disconnected.SetActive(false);
            textPlayer2Joined.SetActive(true);
        }

        // for both host and player2, only for load game
        if (checkSavegameSync && savegameOfHost != "savegameOfHost" && savegameOfPlayer2 != "savegameOfPlayer2" && !loadedGameReady)
        {
            if (savegameOfHost == savegameOfPlayer2)
            {
                // savegames are synchronized/identical -> game can start
                if (!isHost)
                {
                    CmdSetLoadedGameReady();  // tell host that the loaded game is ready for playing
                }
            } else
            {
                // savegames are asynchronous -> game is aborted (show asynSavegamesErrorScreen)
                asynSavegamesErrorScreen.SetActive(true);
                MenuManager.Instance.gamePaused = true;
            }
        }

        // for player2 when host initiates disconnect
        if (!isHost && player2ShallSave)
        {
            if (activePawnPromotion)
            {
                // this player has an open pawn promotion menu
                ButtonController.Disconnect();  // disconnect without saving (saving would not work properly with open pawn promotion menu)
                return;
            }

            // player2 saves and disconnects afterwards
            MenuManager.Instance.player2DisconnectedThroughHost = true;
            buttonController.SaveAndDisconnect();
        }

        // for host when host initiated disconnect (and has been waiting for player2's disconnect first)
        if (isHost && buttonController.waitForDisconnect && NetworkServer.connections.Count == 1)
        {
            // player2 has saved and disconnected -> host can save and disconnect too
            buttonController.BackToMainMenu();
        }

        // for host when player2 disconnected
        if (isHost && !buttonController.waitForDisconnect && textPlayer2Joined.activeSelf && NetworkServer.connections.Count == 1)
        {
            if (asynSavegamesErrorScreen.activeSelf)
            {
                // game is aborted because of asynchronous savegames -> host stays at AsyncSavegames Error Screen even if player2 disconnects now
                // host disconnects as soon as the host player clicks on "Back to Main Menu" Button
                return;
            }

            if (activePawnPromotion)
            {
                // this player has an open pawn promotion menu
                ButtonController.Disconnect();  // disconnect without saving (saving would not work properly with open pawn promotion menu)
                return;
            }

            // host disconnects, too (and saves previously)
            MenuManager.Instance.hostDisconnectedThroughPlayer2 = true;
            buttonController.SaveAndDisconnect();
        }
    }

    public void OnlineMultiplayerReset()
    {
        whiteMoved = false;
        blackMoved = false;
        joinedPlayers = 0;
        checkSavegameSync = false;
        pawnPromotion = false;
        isLoadedGame = false;
        newGameReady = false;
        loadedGameReady = false;
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
    public void CmdTellServerLastMoveAll(bool moveByWhite, bool moveByBlack, string piece, Vector3 targetPosition, bool moveWithPawnPromotion)
        // tells server all required information about the move that was just executed
    {
        whiteMoved = moveByWhite;
        blackMoved = moveByBlack;

        lastMovedPiece = piece;
        lastMoveTargetPos = targetPosition;

        pawnPromotion = moveWithPawnPromotion;
    }

    [Command(requiresAuthority = false)]
    public void CmdTellServerPlayer2Savegame(string savegameString)
        // tell server what player2's savegame looks like
    {
        savegameOfPlayer2 = savegameString;
    }

    [Command(requiresAuthority = false)]
    public void CmdCheckSavegameSynchronization()
        // tells both players that they shall check whether their savegames are identical or not
    {
        checkSavegameSync = true;
    }

    [Command(requiresAuthority = false)]
    public void CmdTellServerPawnPromotionResult(string tagOfNewPiece)
        // tells server the result ("tagOfNewPiece") of the pawn promotion of the last move
    {
        pawnPromotionResult = tagOfNewPiece;
    }

    [Command(requiresAuthority = false)]
    public void CmdResetPawnPromotion()
    {
        pawnPromotion = false;
        pawnPromotionResult = "";
    }

    [Command(requiresAuthority = false)]
    public void CmdSetIsLoadedGame()
    {
        isLoadedGame = true;
    }

    [Command(requiresAuthority = false)]
    public void CmdPlayer2ShallSave()
    {
        player2ShallSave = true;
    }

    [Command(requiresAuthority = false)]
    public void CmdSetActivePawnPromotion(bool activePawnPromotionNow)
    {
        activePawnPromotion = activePawnPromotionNow;
    }

    [Command(requiresAuthority = false)]
    public void CmdSetNewGameReady()
    {
        newGameReady = true;
    }

    [Command(requiresAuthority = false)]
    public void CmdSetLoadedGameReady()
    {
        loadedGameReady = true;
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
            isHost = true;
            CmdNewClientJoined(); //joinedPlayers += 1;
            textWaitForPlayer2.SetActive(true);  // host: message that player2 has not joined yet
            board.GetComponent<BoardManager>().SetWhitesTurnBlacksTurn();
        } else if (joinedPlayers == 1)
        {
            player = "black";
            isHost = false;
            CmdNewClientJoined(); // joinedPlayers += 1;
            textJoinedSuccessfully.SetActive(true);  // player2: message that the game was joined successfully
            board.GetComponent<BoardManager>().SetWhitesTurnBlacksTurn();
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
