using UnityEngine;
using System;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    //Attributes
    //References
    [SerializeField]
    private GameObject _player;

    [SerializeField]
    private InitializeLevel _initializeLevel;

    [SerializeField]
    private CoutdownManager _coutdownManager;

    //Values
    //Action - This use for tell to the player if can or not move
    public static Action<bool> ShowResultUI;
    public static Action<bool> ShowExplanation1;
    public static Action<bool> ShowExplanation2;
    public static Action<bool> ShowExplanation3;
    public static Action StopMusic;
    public static Action<bool> Pause;
    public static Action RestartGame;

    private bool _explanation1 = false;
    private bool _explanation2 = false;
    private bool _explanation3 = false;

    private bool _isPaused = false;
    private bool _win = false;

    //Singleton
    //private static GameManager Instance = null;

    //Functions
    /*
    void Awake()
    {
        // if the singleton hasn't been initialized yet
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
    }
     */


    void Start()
    {
        Grabber.PlayerWin += Win;
        CotcManager.StartGame += BeginPlay;
        CotcManager.Logout+= Restart;
        _initializeLevel.Initialize();
    }

    private void BeginPlay()
    {
        if (ShowExplanation1 != null)
        {
            ShowExplanation1(true);
        }
    }

    private void Update()
    {
        //Begin
        if(Input.GetKeyDown(KeyCode.Space) && (!_explanation1 || !_explanation2 || !_explanation3))
        {
            if(!_explanation1)
            {
                _explanation1 = true;
                if(ShowExplanation1 != null)
                {
                    ShowExplanation1(false);
                }

                if(ShowExplanation2 != null)
                {
                    ShowExplanation2(true);
                }
            }
            else if(!_explanation2)
            {
                _explanation2 = true;
                if (ShowExplanation2 != null)
                {
                    ShowExplanation2(false);
                }

                if(ShowExplanation3 != null)
                {
                    ShowExplanation3(true);
                }
            }
            else if (!_explanation3)
            {
                _explanation3 = true;
                if (ShowExplanation3 != null)
                {
                    ShowExplanation3(false);
                }
                _coutdownManager.StartCoutdown();
            }
        }

        //Pause menu
        else if(Input.GetKeyDown(KeyCode.Escape) && _explanation1 && _explanation2 && _explanation3 && !_win)
        {
            if(_isPaused)
            {
                _isPaused = false;
                if(Pause != null)
                {
                    Pause(false);
                }
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            else
            {
                _isPaused = true;
                if (Pause != null)
                {
                    Pause(true);
                }
                Cursor.lockState = CursorLockMode.Confined;
                Cursor.visible = true;
            }
        }
    }

    //The two following functions are made for the buttons
    public void Restart()
    {
        //Action for stop the musique
        if (StopMusic != null)
        {
            StopMusic();
        }


        //Action for hide the UI
        if(ShowResultUI != null)
        {
            ShowResultUI(false);
        }

        //Reset the bool
        /*
        _explanation1 = false;
        _explanation2 = false;
        _explanation3 = false;
        _isPaused = false;
        _win = false;

        //Initialize the level again
        _initializeLevel.Initialize();
         */

        //Action restart
        if (RestartGame != null)
        {
            RestartGame();
        }

        SceneManager.LoadScene("MainLevel");

    }

    public void Quit()
    {
        Application.Quit();
    }

    public void Resume()
    {
        _isPaused = false;
        if (Pause != null)
        {
            Pause(false);
        }
    }

    //Get if the player won
    private void Win()
    {
        _win = true;
    }
}
