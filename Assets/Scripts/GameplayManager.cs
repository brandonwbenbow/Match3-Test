using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameplayManager : MonoBehaviour
{
    public GameObject PlayArea;                 // Parent of cards
    public GameObject CardPrefab;               // Card Preset

    public GameObject endGamePanel;             // Board Complete panel
    public GameObject leaderboardPanel;         // Leaderboard panel
    public GameObject newGamePanel;             // Escape Key panel

    public Button playAreaButton;               // Game Board tab
    public Button leaderboardButton;            // Leaderboard tab

    // Leaderboard Slots
    public GameObject[] tryLeaderboardSlots;
    public GameObject[] timeLeaderboardSlots;

    // Colors for buttons
    public Color normalColorBlue;
    public Color highlightedColorBlue;
    public Color pressedColorBlue;

    public Color normalColorWhite;
    public Color highlightedColorWhite;
    public Color pressedColorWhite;

    // Right side panel - completed cards 
    public GameObject[] Photoboard;
    public Sprite photoboardPlaceholder;
    int photoUp = 0;

    // Top right - stats
    Text playerNameText;
    Text triesText;
    Text timeText;

    // Update top right stats
    int triesString = 0;
    float timeString = 0;

    // Board dimensions
    const int _xDimension = 9;
    const int _yDimension = 5;

    // Multiplied by the board spot for propper game object spacing
    const float spacingMultX = 1.5f;
    const float spacingMultY = 2f;

    // List of cards associated with card data
    public Dictionary<GameObject, GameStateManager.Card> CARD_LIST; // Access for card details from the game object
    public GameObject[,] CardSpot;

    public Sprite[] SpriteList;

    // Stats and calculations
    private List<GameObject> selectedCards = new List<GameObject>();
    private int tries = 0;
    private float timeElapsed = 0;

    void Start()
    {
        // Set other screens to unactive
        endGamePanel.SetActive(false);
        leaderboardPanel.SetActive(false);
        newGamePanel.SetActive(false);

        // New GameObject/Card list
        CARD_LIST = new Dictionary<GameObject, GameStateManager.Card>();

        // Set stats
        playerNameText = GameObject.FindGameObjectWithTag("PlayerNameText").GetComponent<Text>();
        triesText = GameObject.FindGameObjectWithTag("TriesText").GetComponent<Text>();
        timeText = GameObject.FindGameObjectWithTag("TimeText").GetComponent<Text>();

        playerNameText.text = GameStateManager.ENTERED_NAME;
        triesText.text = "Tries: 0";
        timeText.text = "Time: 0:00";

        // Load if we have a save, or generate new board
        if (GameStateManager.CheckForPlayerName())
        {
            LoadFromSave();
        }
        else
        {
            GenerateBoard();
        }

        // Set colors
        ColorBlock paButton = playAreaButton.colors;
        ColorBlock lbButton = leaderboardButton.colors;

        paButton.normalColor = normalColorWhite;
        paButton.highlightedColor = highlightedColorWhite;
        paButton.pressedColor = pressedColorWhite;

        lbButton.normalColor = normalColorBlue;
        lbButton.highlightedColor = highlightedColorBlue;
        lbButton.pressedColor = pressedColorBlue;

        playAreaButton.colors = paButton;
        leaderboardButton.colors = lbButton;

        // Start timer
        StartCoroutine(Tick());
    }

    void Update()
    {
        // If no UI screens are active, allow for card picks
        if(!endGamePanel.activeSelf && !leaderboardPanel.activeSelf && !newGamePanel.activeSelf)
            MouseControls();

        // Update stats
        UpdateUI();

        // In game exit
        if(Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleNewGamePanel();
        }
    }

    // All mouse calculations
    void MouseControls()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, 100f))
        {
            hit.transform.GetComponent<CardHandler>().CheckRaycast();

            if (Input.GetMouseButtonDown(0) && !CARD_LIST[hit.transform.gameObject]._facingUp && selectedCards.Count < 3)
            {
                hit.transform.GetComponent<CardHandler>().Flip();

                if (selectedCards.Count < 3)
                    selectedCards.Add(hit.transform.gameObject);

                OnAction();
            }
        }
    }

    void UpdateUI()
    {
        if (triesString != tries)
        {
            triesString = tries;
            triesText.text = "Tries: " + triesString;
        }

        if (timeString != timeElapsed)
        {
            timeString = timeElapsed;
            timeText.text = "Time: " + Mathf.FloorToInt(timeString / 60) + ":" + (timeString % 60).ToString("00");
        }
    }

    private void GenerateBoard()
    {
        CardSpot = new GameObject[_xDimension, _yDimension]; // 2D array for card placement

        int spriteLimit = (_xDimension * _yDimension) / 3; // Sets limit on what card units we will pick from based on amount of cards on game board

        List<int> spritesAvalible = new List<int>(); // Set avalible card numbers in this list to be removeable after 3 have been picked

        for (int i = 0; i < spriteLimit * 3; i++) // Populate the array/list
        {
            spritesAvalible.Add(i / 3);
        }

        // Going through column then row, create a copy of the card prefab, assign in a random number, and add to the gameObject/card dictionary
        for (int x = 0; x < _xDimension; x++)
        {
            for (int y = 0; y < _yDimension; y++)
            {
                CardSpot[x, y] = Instantiate(CardPrefab, new Vector3(x * spacingMultX, y * spacingMultY, 0f), Quaternion.identity, PlayArea.transform);

                int rand = Random.Range(0, spritesAvalible.Count); // Random number for sprite in SpriteList

                CardSpot[x, y].transform.Find("CardMask").transform.Find("CardUnit").GetComponent<SpriteRenderer>().sprite = SpriteList[spritesAvalible[rand]];
                CardSpot[x, y].name = spritesAvalible[rand].ToString();

                spritesAvalible.RemoveAt(rand);
                rand = Random.Range(0, spritesAvalible.Count);

                // Set Card in dictionary
                CARD_LIST.Add(CardSpot[x, y], new GameStateManager.Card
                {
                    _cardNumber = int.Parse(CardSpot[x, y].name),
                    _facingUp = false,
                    _completedTrio = false
                });

            }
        }

        int[] tempPasser = new int[15]; // Right side completed cards list for saving

        Debug.Log("Called Save.");
        GameStateManager.SaveGameState(CARD_LIST, CardSpot, 0, 0f, tempPasser); // Save gameState to json

    }

    public void LoadFromSave()
    {
        CARD_LIST.Clear(); // Clear list for load

        string playerName;
        GameStateManager.Card[,] tempCardHolder;

        Debug.Log("Called Load.");
        GameStateManager.LoadGameState(out playerName, out tempCardHolder, out tries, out timeElapsed, out int[] tempSpritePasser);

        CardSpot = new GameObject[_xDimension, _yDimension];

        for (int x = 0; x < _xDimension; x++)
        {
            for (int y = 0; y < _yDimension; y++)
            {
                // Create board for spot on [x, y] array
                int spriteID = tempCardHolder[x, y]._cardNumber;
                bool facingUp = tempCardHolder[x, y]._facingUp;
                bool completedTrio = tempCardHolder[x, y]._completedTrio;

                CardSpot[x, y] = Instantiate(CardPrefab, new Vector3(x * spacingMultX, y * spacingMultY, 0f), Quaternion.identity, PlayArea.transform);
                CardSpot[x, y].transform.Find("CardMask").transform.Find("CardUnit").GetComponent<SpriteRenderer>().sprite = SpriteList[spriteID];
                CardSpot[x, y].name = spriteID.ToString();

                // Sees if card should be facing up
                if (facingUp)
                {
                    CardSpot[x, y].transform.Rotate(Vector3.up, 180f);
                    if (completedTrio)
                    {
                        // turn on green glow for cards
                    }
                }

                // Create new card data for associated gameObject
                CARD_LIST.Add(CardSpot[x, y], new GameStateManager.Card
                {
                    _cardNumber = tempCardHolder[x, y]._cardNumber,
                    _facingUp = tempCardHolder[x, y]._facingUp,
                    _completedTrio = tempCardHolder[x, y]._completedTrio
                });

                // See if card is completed in a trio or just selected
                if(CARD_LIST[CardSpot[x, y]]._facingUp && !CARD_LIST[CardSpot[x, y]]._completedTrio)
                {
                    selectedCards.Add(CardSpot[x, y].transform.gameObject);

                    OnAction();
                }
            }
        }

        // Test if the board is completed from load
        bool completedBoard = true;
        for (int i = 0; i < Photoboard.Length; i++)
        {
            if (tempSpritePasser[i] == 0)
            {
                photoUp = i;
                completedBoard = false;
                break;
            }
            else
            {
                Debug.Log(i);
                Photoboard[i].transform.Find("Sprite").GetComponent<SpriteRenderer>().sprite = SpriteList[tempSpritePasser[i] - 1];
                Photoboard[i].transform.Find("Sprite").GetComponent<SpriteRenderer>().color = Color.white;
            }
        }

        if (completedBoard)
            EndGameScreen();

        UpdateUI();
    }

    public void OnAction()
    {
        // If theres 3 selected cards, check if matching or not
        if (selectedCards.Count == 3)
            StartCoroutine(CalculateMatchingCards());
    }

    IEnumerator CalculateMatchingCards()
    {
        bool matching = true;
        string cardNum = selectedCards[0].name;

        for (int i = 1; i < 3; i++)
        {
            if (selectedCards[i].name != cardNum)
            {
                matching = false;
            }
        }

        if (matching)
        {
            // Matched, set completed trio
            for (int i = 0; i < selectedCards.Count; i++)
            {
                selectedCards[i].GetComponent<CardHandler>().CompletedTrio();
                GameObject tempHold = selectedCards[i].transform.Find("CardMask").transform.Find("CardUnit").gameObject;
                Instantiate(photoboardPlaceholder, tempHold.transform);
            }

            Photoboard[photoUp].transform.Find("Sprite").GetComponent<SpriteRenderer>().sprite = selectedCards[0].transform.Find("CardMask").transform.Find("CardUnit").GetComponent<SpriteRenderer>().sprite;
            Photoboard[photoUp].transform.Find("Sprite").GetComponent<SpriteRenderer>().color = Color.white;
            photoUp++;

            selectedCards.Clear();
            tries++;
            Debug.Log("Match");
        }
        else
        {
            // Wait a second and flip cards
            yield return new WaitForSecondsRealtime(1f);

            for (int i = 0; i < selectedCards.Count; i++)
            {
                selectedCards[i].GetComponent<CardHandler>().Flip();          // Can call resetgameobject just to be safe but this should do the same     
            }
            selectedCards.Clear();
            tries++;
            Debug.Log("Not match");

        }

        SaveGamePasser();

        yield return 0;

    }

    // Grab data and pass it to GameStateManager for saving
    void SaveGamePasser()
    {
        int[] tempSpitePasser = new int[15];
        bool compeletedBoard = true;

        for (int i = 0; i < tempSpitePasser.Length; i++)
        {
            if (Photoboard[i].transform.Find("Sprite").GetComponent<SpriteRenderer>().sprite == photoboardPlaceholder)
            {
                tempSpitePasser[i] = 0;
                compeletedBoard = false;
                continue;
            }
            else
            {
                for (int j = 0; j < SpriteList.Length; j++)
                {
                    if (Photoboard[i].transform.Find("Sprite").GetComponent<SpriteRenderer>().sprite.name == SpriteList[j].name)
                    {
                        tempSpitePasser[i] = j + 1;
                    }
                }
            }
        }

        if (compeletedBoard)
        {
            GameStateManager.SaveHighScore(tries, timeElapsed);
            EndGameScreen();
        }
        else
            GameStateManager.SaveGameState(CARD_LIST, CardSpot, tries, timeElapsed, tempSpitePasser);
    }

    // Activate board completed screen
    void EndGameScreen()
    {
        GameStateManager.ClearGameState();
        Debug.LogWarning("Called ClearGameState()");
        endGamePanel.SetActive(true);
    }

    // If application is quit out, save
    private void OnApplicationQuit()
    {
        SaveGamePasser();
    }

    // Button commands to start new game
    public void StartGameButtonCommand()
    {
        GameStateManager.ClearGameState();
        StartCoroutine(StartGameOver());
    }

    // Checks twice a second for deleted file so we can make a new one
    IEnumerator StartGameOver()
    {
        if (!GameStateManager.CheckForPlayerName())
        {
            Debug.Log("Transitioning scenes");
            SceneManager.LoadScene("Game");
        }
        else
            Debug.LogWarning("Player name save data not deleted");

        yield return new WaitForSeconds(0.5f);
    }

    // Set leaderboard screen active
    public void LeaderboardShow()
    {
        if (!leaderboardPanel.activeSelf)
        {
            ToggleButtonColors();
            leaderboardPanel.SetActive(true);
            UpdateLeaderboard();
        }
    }

    // Set leaaderboard sceen to inactive
    public void LeaderboardHide()
    {
        if (leaderboardPanel.activeSelf)
        {
            ToggleButtonColors();
            leaderboardPanel.SetActive(false);
        }
    }

    // Update slots on leaderboard
    void UpdateLeaderboard()
    {
        GameStateManager.LoadHighScores(out GameStateManager.TryScores tryScores, out GameStateManager.TimeScores timeScores);

        for (int i = 0; i < 10; i++)
        {
            if (tryScores._tryScore[i] != 0)
            {
                tryLeaderboardSlots[i].transform.Find("Name").GetComponent<Text>().text = (i + 1) + ": " + tryScores._playerName[i];
                tryLeaderboardSlots[i].transform.Find("Tries").GetComponent<Text>().text = tryScores._tryScore[i].ToString();
                tryLeaderboardSlots[i].transform.Find("Time").GetComponent<Text>().text = Mathf.FloorToInt(tryScores._timeScore[i] / 60) + ":" + (tryScores._timeScore[i] % 60).ToString("00");
            }
            else
            {
                tryLeaderboardSlots[i].transform.Find("Name").GetComponent<Text>().text = "";
                tryLeaderboardSlots[i].transform.Find("Tries").GetComponent<Text>().text = "";
                tryLeaderboardSlots[i].transform.Find("Time").GetComponent<Text>().text = "";
            }
        }

        for (int i = 0; i < 10; i++)
        {
            if (tryScores._tryScore[i] != 0)
            {
                timeLeaderboardSlots[i].transform.Find("Name").GetComponent<Text>().text = (i + 1) + ": " + timeScores._playerName[i];
                timeLeaderboardSlots[i].transform.Find("Tries").GetComponent<Text>().text = timeScores._tryScore[i].ToString();
                timeLeaderboardSlots[i].transform.Find("Time").GetComponent<Text>().text = Mathf.FloorToInt(timeScores._timeScore[i] / 60) + ":" + (timeScores._timeScore[i] % 60).ToString("00");
            }
            else
            {
                timeLeaderboardSlots[i].transform.Find("Name").GetComponent<Text>().text = "";
                timeLeaderboardSlots[i].transform.Find("Tries").GetComponent<Text>().text = "";
                timeLeaderboardSlots[i].transform.Find("Time").GetComponent<Text>().text = "";
            }
        }
    }

    // Switch color palette for button 
    void ToggleButtonColors()
    {
        ColorBlock paButton = playAreaButton.colors;
        ColorBlock lbButton = leaderboardButton.colors;

        if (paButton.normalColor == normalColorWhite)
        {
            paButton.normalColor = normalColorBlue;
            paButton.highlightedColor = highlightedColorBlue;
            paButton.pressedColor = pressedColorBlue;
        }
        else
        {
            paButton.normalColor = normalColorWhite;
            paButton.highlightedColor = highlightedColorWhite;
            paButton.pressedColor = pressedColorWhite;
        }

        if (lbButton.normalColor == normalColorWhite)
        {
            lbButton.normalColor = normalColorBlue;
            lbButton.highlightedColor = highlightedColorBlue;
            lbButton.pressedColor = pressedColorBlue;
        }
        else
        {
            lbButton.normalColor = normalColorWhite;
            lbButton.highlightedColor = highlightedColorWhite;
            lbButton.pressedColor = pressedColorWhite;
        }

        playAreaButton.colors = paButton;
        leaderboardButton.colors = lbButton;

        Text paText = playAreaButton.transform.Find("Text").GetComponent<Text>();
        Text lbText = leaderboardButton.transform.Find("Text").GetComponent<Text>();

        if (paText.color == Color.white)
            paText.color = Color.black;
        else
            paText.color = Color.white;

        if (lbText.color == Color.white)
            lbText.color = Color.black;
        else
            lbText.color = Color.white;

        Debug.Log("Toggled Colors");
    }

    // Allow players to start new board or quit mid game
    public void ToggleNewGamePanel()
    {
        if (newGamePanel.activeSelf)
            newGamePanel.SetActive(false);
        else
            newGamePanel.SetActive(true);
    }

    // Quit game from button
    public void ExitGame()
    {
        Application.Quit();
    }

    // Go back to the main menu for exit or new game
    public void LoadMainMenu()
    {
        SaveGamePasser();
        SceneManager.LoadScene("MainMenu");
    }

    // Runs every second of ACTIVE gameboard
    IEnumerator Tick()
    {
        yield return new WaitForSeconds(1f);

        if(!endGamePanel.activeSelf && !leaderboardPanel.activeSelf && !newGamePanel.activeSelf)
            timeElapsed += 1f;

        Debug.Log("tick");
        StartCoroutine(Tick());
    }
}
