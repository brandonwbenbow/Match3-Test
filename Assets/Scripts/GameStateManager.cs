using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class GameStateManager
{
    // Set Player's Entered Name and Persistent Datapath
    public static string ENTERED_NAME { get; set; }
    public static string SAVE_PATH = Application.persistentDataPath + "/saves";
    public static string SCORE_PATH = Application.persistentDataPath + "/scores";

    // Public Card Data Class
    [System.Serializable]
    public class Card
    {
        public int _cardNumber;
        public bool _facingUp;
        public bool _completedTrio;
    }

    // Data Classes
    private class Player
    {
        public string _playerName;
        public GameState _gameState;
        public int[] _spriteOnPhoto;
    }

    [System.Serializable]
    private class GameState
    {
        public Card[] _cardPositions;
        public int _tries;
        public float _timeElapsed;
        public int _rowLength;
        public int _columnHieght;
    }

    // Leaderboard class
    [System.Serializable]
    public class TryScores
    {
        public int[] _tryScore;
        public float[] _timeScore;
        public string[] _playerName;
    }

    [System.Serializable]
    public class TimeScores
    {
        public float[] _timeScore;
        public int[] _tryScore;
        public string[] _playerName;
    }

    // Initalize Persistent Data
    public static void Initalize()
    {
        if(!Directory.Exists(SAVE_PATH))
        {
            Directory.CreateDirectory(SAVE_PATH);
        }

        if(!Directory.Exists(SCORE_PATH))
        {
            Directory.CreateDirectory(SCORE_PATH);
            File.Create(SCORE_PATH + "/tries.json").Dispose();
            File.Create(SCORE_PATH + "/times.json").Dispose();

            InitalizeLeaderboard();
        }
        else if(!File.Exists(SCORE_PATH + "/tries.json") || !File.Exists(SCORE_PATH + "/times.json"))
        {
            File.Create(SCORE_PATH + "/tries.json").Dispose();
            File.Create(SCORE_PATH + "/times.json").Dispose();

            InitalizeLeaderboard();
        }
    } 

    // Set up file for leaderboard saves
    static void InitalizeLeaderboard()
    {
        TryScores tryBoard = new TryScores
        {
            _tryScore = new int[10],
            _timeScore = new float[10],
            _playerName = new string[10]
        };

        TimeScores timeBoard = new TimeScores
        {
            _timeScore = new float[10],
            _tryScore = new int[10],
            _playerName = new string[10]
        };

        string jsonTry = JsonUtility.ToJson(tryBoard);
        string jsonTime = JsonUtility.ToJson(timeBoard);

        File.WriteAllText(SCORE_PATH + "/tries.json", jsonTry);
        File.WriteAllText(SCORE_PATH + "/times.json", jsonTime);

        Debug.Log("Initalized Scores");
    }

    // Methods for saving/loading
    public static void SaveGameState(Dictionary<GameObject, Card> CARD_LIST, GameObject[,] CardSpot, int tries, float timeElapsed, int[] tempSpritePasser)
    {
        if(!File.Exists(SAVE_PATH + "/" + ENTERED_NAME)) // Check if we have a viable file to save to
        {
            File.Create(SAVE_PATH + "/" + ENTERED_NAME).Dispose();
        }

        Card[] tempPasser = new Card[CardSpot.GetLength(0) * CardSpot.GetLength(1)]; // Creates the single dimension Card array for 2D CardSpot array
        for (int x = 0; x < CardSpot.GetLength(0); x++)
        {
            for (int y = 0; y < CardSpot.GetLength(1); y++)
            {
                tempPasser[(x * CardSpot.GetLength(1)) + y] = CARD_LIST[CardSpot[x, y]];
            }
        }

        // Set data classes
        GameState gameState = new GameState
        {
            _cardPositions = tempPasser,
            _tries = tries,
            _timeElapsed = timeElapsed,
            _rowLength = CardSpot.GetLength(0),
            _columnHieght = CardSpot.GetLength(1)
        };

        Player playerName = new Player
        {
            _playerName = ENTERED_NAME,
            _gameState = new GameState {
                _cardPositions = gameState._cardPositions,
                _tries = gameState._tries,
                _timeElapsed = gameState._timeElapsed,
                _rowLength = gameState._rowLength,
                _columnHieght = gameState._columnHieght
            },
            _spriteOnPhoto = tempSpritePasser
        };

        Debug.Log(JsonUtility.ToJson(playerName));

        // Save it to C:Users/<username>/AppData/LocalLow/Brandon Benbow/Chilltime Match 3/saves (windows)
        string json = JsonUtility.ToJson(playerName);
        File.WriteAllText(SAVE_PATH + "/" + ENTERED_NAME, json);

        Debug.Log("Saved.");

        
    }

    // Loads game state
    public static void LoadGameState(out string playerName, out Card[,] CardSpots, out int tries, out float timeElapsed, out int[] tempSpritePasser)
    {
        string saveState = File.ReadAllText(SAVE_PATH + "/" + ENTERED_NAME); // Grab data from save file and fill Player data class
        Player playerState = JsonUtility.FromJson<Player>(saveState);

        Card[,] tempCardPasser = new Card[playerState._gameState._rowLength, playerState._gameState._columnHieght]; // Transfer single dimension array back to 2D array
        for (int x = 0; x < tempCardPasser.GetLength(0); x++)
        {
            for (int y = 0; y < tempCardPasser.GetLength(1); y++)
            {
                tempCardPasser[x, y] = playerState._gameState._cardPositions[(x * tempCardPasser.GetLength(1)) + y];
            }
        }

        playerName = playerState._playerName;
        CardSpots = tempCardPasser;
        tries = playerState._gameState._tries;
        timeElapsed = playerState._gameState._timeElapsed;
        tempSpritePasser = playerState._spriteOnPhoto;
    }

    // Deletes file for completed board
    public static void ClearGameState()
    {
        Debug.Log("Clear Game State");

        File.Delete(SAVE_PATH + "/" + ENTERED_NAME);

        if (File.Exists(SAVE_PATH + "/" + ENTERED_NAME))
            Debug.LogError("Did not delete path for " + ENTERED_NAME);
    }

    public static void SaveHighScore(int tries, float timeElapsed)
    {
        if (!File.Exists(SCORE_PATH + "/tries.json") || !File.Exists(SCORE_PATH + "/times.json"))
        {
            File.Create(SCORE_PATH + "/tries.json").Dispose();
            File.Create(SCORE_PATH + "/times.json").Dispose();
        }

        bool updateScores = false;

        TryScores tryBoard;
        TimeScores timeBoard;

        LoadHighScores(out tryBoard, out timeBoard);

        // If better than 10th place, add to the board in sorted position, bump rest down
        if (tries < tryBoard._tryScore[tryBoard._tryScore.Length - 1] || tryBoard._tryScore[tryBoard._tryScore.Length - 1] == 0)
        {
            updateScores = true;
            for (int i = 0; i < tryBoard._tryScore.Length; i++)
            {
                if (tries < tryBoard._tryScore[i] || tryBoard._tryScore[i] == 0)
                {
                    int tempTries = tryBoard._tryScore[i];
                    float tempTime = tryBoard._timeScore[i];
                    string tempName = tryBoard._playerName[i];

                    tryBoard._tryScore[i] = tries;
                    tryBoard._timeScore[i] = timeElapsed;
                    tryBoard._playerName[i] = ENTERED_NAME;

                    for (int j = i + 1; j < tryBoard._tryScore.Length; j++)
                    {
                        int tryPasser = tryBoard._tryScore[j];
                        float timePasser = tryBoard._timeScore[j];
                        string namePasser = tryBoard._playerName[j];

                        tryBoard._tryScore[j] = tempTries;
                        tryBoard._timeScore[j] = tempTime;
                        tryBoard._playerName[j] = tempName;

                        tempTries = tryPasser;
                        tempTime = timePasser;
                        tempName = namePasser;
                    }

                    break;
                }
            }
        }

        if (timeElapsed < timeBoard._timeScore[timeBoard._timeScore.Length - 1] || timeBoard._timeScore[timeBoard._timeScore.Length - 1] == 0f)
        {
            updateScores = true;
            for (int i = 0; i < timeBoard._timeScore.Length; i++)
            {
                if(timeElapsed < timeBoard._timeScore[i] || timeBoard._timeScore[i] == 0f)
                {
                    float tempTime = timeBoard._timeScore[i];
                    int tempTries = timeBoard._tryScore[i];
                    string tempName = timeBoard._playerName[i];

                    timeBoard._timeScore[i] = timeElapsed;
                    timeBoard._tryScore[i] = tries;
                    timeBoard._playerName[i] = ENTERED_NAME;

                    for (int j = i + 1; j < timeBoard._timeScore.Length; j++)
                    {
                        float timePasser = timeBoard._timeScore[j];
                        int tryPasser = timeBoard._tryScore[j];
                        string namePasser = timeBoard._playerName[j];

                        timeBoard._timeScore[j] = tempTime;
                        timeBoard._tryScore[j] = tempTries;
                        timeBoard._playerName[j] = tempName;

                        tempTime = timePasser;
                        tempTries = tryPasser;
                        tempName = namePasser;
                    }

                    break;
                }
            }
        }

        if(updateScores)
        {
            string jsonTry = JsonUtility.ToJson(tryBoard);
            string jsonTime = JsonUtility.ToJson(timeBoard);

            File.WriteAllText(SCORE_PATH + "/tries.json", jsonTry);
            File.WriteAllText(SCORE_PATH + "/times.json", jsonTime);

            Debug.Log("Updated Scores");
        }
    }

    public static void LoadHighScores(out TryScores tries, out TimeScores times) // Returns top 10 tries and times, seperate lists
    {
        string triesLeaderboard = File.ReadAllText(SCORE_PATH + "/tries.json");
        string timesLeaderboard = File.ReadAllText(SCORE_PATH + "/times.json");

        TryScores tryScores = JsonUtility.FromJson<TryScores>(triesLeaderboard);
        TimeScores timeScores = JsonUtility.FromJson<TimeScores>(timesLeaderboard);

        tries = tryScores;
        times = timeScores;
    }

    // Checks if player name is associated with a saved game state
    public static bool CheckForPlayerName()
    {
        Debug.Log(File.Exists(SAVE_PATH + "/" + ENTERED_NAME));

        if (File.Exists(SAVE_PATH + "/" + ENTERED_NAME))
        {
            return true;
        }
        else
        {
            return false;
        }
    }
}
