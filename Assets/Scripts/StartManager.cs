using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class StartManager : MonoBehaviour
{
    // This is for grabbing player name for loading board and transitioning to game scene only

    public Text placeholderText;
    public Text playerNameText; // If empty, disable start game button

    public Button playGameButton; // Changes scene

    private void Start()
    {
        placeholderText.text = "Name";
        playerNameText.text = "";

        playGameButton.interactable = false;

        GameStateManager.Initalize();
    }

    private void Update()
    {
        if(playerNameText.text != "")
        {
            playGameButton.interactable = true;
        }
        else if(playerNameText.text == "")
        {
            playGameButton.interactable = false;
        }

        if(playGameButton.interactable)
        {
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                StartGame();
        }

        if (Input.GetKeyDown(KeyCode.Escape))
            ExitGame();
    }

    public void ExitGame()
    {
        Application.Quit();
    }

    public void StartGame()
    {
        GameStateManager.ENTERED_NAME = playerNameText.text;
        SceneManager.LoadScene("Game");
    }
}
