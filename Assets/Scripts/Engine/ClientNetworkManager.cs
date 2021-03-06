/*
    Copyright (c) 2018, Szymon Jakóbczyk, Paweł Płatek, Michał Mielus, Maciej Rajs, Minh Nhật Trịnh, Izabela Musztyfaga
    All rights reserved.

    Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met:

        * Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
        * Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation 
          and/or other materials provided with the distribution.
        * Neither the name of the [organization] nor the names of its contributors may be used to endorse or promote products derived from this software 
          without specific prior written permission.

    THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT 
    LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT 
    HOLDER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT 
    LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON 
    ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE 
    USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/

using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using UnityEngine.UI;
using UnityEngine.Networking.NetworkSystem;
using System;

/*
 *  This is singleton object
 *  Created at "JoinGameScene"
 *  Destroyed at game exit or "Back" button in "JoinGameScene" (in LevelLoader.Back)
 */
public class ClientNetworkManager : NetworkManager
{

    private GameController gameController;
    private GameApp gameApp;
    private LevelLoader levelLoader;
    private ErrorInfoPanel errorInfoPanel;

    public NetworkClient networkClient;
    public NetworkConnection connection;

    private static ClientNetworkManager instance;

    void Awake()
    {
        if (instance == null)
        {
            gameApp = GameObject.Find("GameApp").GetComponent<GameApp>();
            levelLoader = GameObject.Find("LevelLoader").GetComponent<LevelLoader>();
            errorInfoPanel = GameObject.Find("ErrorInfoCanvas").GetComponent<ErrorInfoPanel>();

            DontDestroyOnLoad(this.gameObject);
            instance = this;
            Debug.Log("Awake: " + this.gameObject);
        } else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    /*
     *   After "Join" button in "JoinGameScene"
     *   Setup server address and port, start client
     */
    public void SetupClient()
    {
        Debug.Log("SetupClient");

        try
        {
            // persists config data from menu scene
            gameApp.PersistAllParameters("JoinGameScene");

            this.networkAddress = gameApp.GetAndRemoveInputField("ServerAddress");
            this.networkPort = int.Parse(gameApp.GetAndRemoveInputField("ServerPort"));

            // uncomment for testing
            //this.networkAddress = "127.0.0.1";
            //this.networkPort = 7777;
            this.StartClient();
        } catch(Exception e)
        {
            Debug.Log("SetupClient error: " + e.Message);
            errorInfoPanel.Show("SetupClient error: " + e.Message);
            return;
        }
    }

    // Client callbacks

    /*
     *  After connection to the server, scene should changed to "GameScene"
     *  After the change, server is informed that client is ready and he spawns objects
     *  Then server calls 
     */
    public override void OnClientSceneChanged(NetworkConnection conn)
    {
        Debug.Log("OnClientSceneChanged: " + conn);
        base.OnClientSceneChanged(conn);
    }

    /*
     *  Invoked when client connects to the server
     *  It sends message with player name (connAssignPlayerId), server should then send connAssignPlayerErrorId, connAssignPlayerSuccessId or connClientReadyId
     */
    public override void OnClientConnect(NetworkConnection conn)
    {
        //base.OnClientConnect(conn);
        Debug.Log("OnClientConnect: Connected successfully to server");

        ClientScene.RegisterPrefab(gameApp.PlayerPrefab);
        ClientScene.RegisterPrefab(gameApp.PlanetPrefab);
        ClientScene.RegisterPrefab(gameApp.StartPrefab);
        ClientScene.RegisterPrefab(gameApp.ScoutPrefab);
        ClientScene.RegisterPrefab(gameApp.ColonizerPrefab);
        ClientScene.RegisterPrefab(gameApp.MinerPrefab);
        ClientScene.RegisterPrefab(gameApp.WarshipPrefab);
        ClientScene.RegisterPrefab(gameApp.ExplosionPrefab);
        ClientScene.RegisterPrefab(gameApp.AttackPrefab);
        ClientScene.RegisterPrefab(gameApp.HitPrefab);

        networkClient.RegisterHandler(gameApp.connAssignPlayerErrorId, OnClientAssignPlayerError);
        networkClient.RegisterHandler(gameApp.connAssignPlayerSuccessId, OnClientAssignPlayerSuccess);
        networkClient.RegisterHandler(gameApp.connSetupTurnId, OnClientSetupTurn);
        networkClient.RegisterHandler(gameApp.connClientLoadGameId, OnClientLoadGame);
        networkClient.RegisterHandler(gameApp.connClientEndGame, OnClientEndGame);

        string playerName = gameApp.GetAndRemoveInputField("PlayerName");
        string password = gameApp.GetAndRemoveInputField("Password");

        Debug.Log("OnClientConnect: sending player name: " + playerName);

        string playerJson = JsonUtility.ToJson(new GameApp.PlayerMenu
        {
            name = playerName,
            password = password
        });
        StringMessage playerMsg = new StringMessage(playerJson);
        networkClient.Send(gameApp.connAssignPlayerId, playerMsg);
    }


    /*
     *  Custom callback (on connClientEndGame)
     *  Server invoke it at the end of the game
     */
    public void OnClientEndGame(NetworkMessage netMsg)
    {
        string msg = netMsg.ReadMessage<StringMessage>().value;
        Debug.Log("OnClientEndGame: " + msg);

        if(gameController == null)
            gameController = GameObject.Find("GameController").GetComponent<GameController>();
        gameController.GameEnded(msg);
    }

    /*
     *  Custom callback (on connAssignPlayerErrorId)
     *  Server invoke it when client can't join the game
     *  May be wrong player name, or the player is taken already
     */
    public void OnClientAssignPlayerError(NetworkMessage netMsg)
    {
        Debug.Log("OnClientAssignPlayerError: " + netMsg.ReadMessage<StringMessage>().value);
        netMsg.conn.Disconnect();
    }

    /*
     * Custom callback (on connAssignPlayerSuccessId)
     * Server invoke it when client joinned to the game
     * Client will wait for the turn
     */
    public void OnClientAssignPlayerSuccess(NetworkMessage netMsg)
    {
        Debug.Log("OnClientAssignPlayerSuccess: " + netMsg.ReadMessage<StringMessage>().value);
    }


    /*
     *  Custom callback (on connSetupTurnId)
     *  Server invoke it from "OnServerReady" when the client finished loading scene
     *  netMsg contains number: 0 - wait, 1 - your turn, play, 2 - you lost
     */
    public void OnClientSetupTurn(NetworkMessage netMsg)
    {
        Debug.Log("OnClientSetupTurn");

        if (gameController == null)
            gameController = GameObject.Find("GameController").GetComponent<GameController>();
        string turnStatusJson = netMsg.ReadMessage<StringMessage>().value;
        GameApp.TurnStatus turnStatus  = JsonUtility.FromJson<GameApp.TurnStatus>(turnStatusJson);

        switch(turnStatus.status)
        {
            case 0:
                gameController.WaitForTurn(turnStatus.msg);
                break;
            case 1:
                gameController.StopWaitForTurn(turnStatus.msg);
                break;
            case 2:
            default:
                gameController.LostTurn(turnStatus.msg);
                break;
        }         
    }

    /*
     *   Custom callback (on connClientLoadGameId)
     *   Server invoke it from "OnServerReady" just after connSetupTurnId
     *   Client should load game from the server
     */
    public void OnClientLoadGame(NetworkMessage netMsg)
    {
        Debug.Log("OnClientLoadGame");

        if (gameController == null)
            gameController = GameObject.Find("GameController").GetComponent<GameController>();

        string savedGameCompressed = netMsg.ReadMessage<StringMessage>().value;
        string savedGame = gameController.Decompress(savedGameCompressed);

        gameController.ClientNextTurnGame(savedGame);
    }

    /*
     *  Called in GameController after next turn
     *  Client should wait
     */
    public override void OnClientNotReady(NetworkConnection conn)
    {
        Debug.Log("Server has set client to be not-ready (stop getting state updates): " + conn);
        if (gameController == null)
            gameController = GameObject.Find("GameController").GetComponent<GameController>();
        gameController.WaitForTurn("Wait...");
    }


    public override void OnClientDisconnect(NetworkConnection conn)
    {
        StopClient();
        if (conn.lastError != NetworkError.Ok)
        {
            if (LogFilter.logError) { Debug.LogError("ClientDisconnected due to error: " + conn.lastError); }
        }
        Debug.Log("Client disconnected from server: " + conn);

        if (levelLoader == null)
            levelLoader = GameObject.Find("LevelLoader").GetComponent<LevelLoader>();
        levelLoader.Back("MainMenuScene");
    }

    public override void OnClientError(NetworkConnection conn, int errorCode)
    {
        Debug.Log("Client network error occurred: " + (NetworkError)errorCode);
    }

    public override void OnStartClient(NetworkClient client)
    {
        Debug.Log("Client has started");
        networkClient = client;
    }

    public override void OnStopClient()
    {
        Debug.Log("Client has stopped");
        if(levelLoader == null)
            levelLoader = GameObject.Find("LevelLoader").GetComponent<LevelLoader>();
        levelLoader.Back("MainMenuScene");
    }

}
