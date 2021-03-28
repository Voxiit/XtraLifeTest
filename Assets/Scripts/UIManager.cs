using UnityEngine;
using System;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class UIManager : MonoBehaviour
{
    //Attributes
    //References
    [SerializeField]
    private Canvas _mainCavas;

    [SerializeField]
    private Image _startImage;

    [SerializeField]
    private TextMeshProUGUI _startText;

    [SerializeField]
    private Image _timerImage;

    [SerializeField]
    private TextMeshProUGUI _timerText;

    [SerializeField]
    private Image _arrowSecondsImage;

    [SerializeField]
    private Canvas _background;

    [SerializeField]
    private Canvas _noteblock;

    [SerializeField]
    private Canvas _explanation1;

    [SerializeField]
    private Canvas _explanation2;

    [SerializeField]
    private Canvas _explanation3;

    [SerializeField]
    private Canvas _pauseRestartQuitCanvas;

    [SerializeField]
    private Image _pauseRestartQuitImage;

    [SerializeField]
    private Sprite _pauseSprite;

    [SerializeField]
    private Sprite _winSprite;

    [SerializeField]
    private Sprite _looseSprite;

    [SerializeField]
    private Image _missingHolderCanvas;

    [SerializeField]
    private Canvas _cutcCanvas;

    [SerializeField]
    private Button _restartButton;

    [SerializeField]
    private Button _quitButton;

    //Values
    private int _totalTime = 90;
    private CotcManager cotcM = null;

    //Singleton
    private static UIManager Instance = null;

    // Start is called before the first frame update
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

    void Start()
    {
        //Regsiter to action
        CoutdownManager.UpdateStartTimerText += UpdateStartText;
        CoutdownManager.ShowTimerUI += ShowTimerUI;
        CoutdownManager.UpdateTimerText += UpdateTimer;
        CoutdownManager.PlayerLoose += PlayerLooseUI;
        CoutdownManager.GetCoutdown += GetTotalTime;
        GameManager.ShowResultUI += ShowResultUI;
        GameManager.ShowExplanation1 += ShowExplanation1;
        GameManager.ShowExplanation2 += ShowExplanation2;
        GameManager.ShowExplanation3 += ShowExplanation3;
        GameManager.RestartGame += Restart;
        Grabber.PlayerWin += PlayerWin;
        GameManager.Pause += ShowPause;
        CotcManager.ShowLog += ShowCutc;

        //Show UI
        ShowStartUI(true);

        //Get cotcmanager
        cotcM = GameObject.Find("CotcManager").GetComponent<CotcManager>();
        if(cotcM != null && cotcM.GetIsLog())
        {
            Debug.Log("UIManager  - Call cotcM play");
            cotcM.Play();
        }
    }


    //Start funciton
    private void UpdateStartText(int time)
    {
        if(time == -1)
        {
            _startText.text = "GO!";
            ShowStartUI(false);
        }
        else
        {
            _startText.text = time.ToString();
        }
    }

    private void ShowStartUI(bool show)
    {
        _startImage.gameObject.SetActive(show);
        _startText.gameObject.SetActive(show);
    }

    private void Restart()
    {
        ShowStartUI(true);
        _startText.text = "";
    }

    //Timer function
    private void UpdateTimer(float currentTime)
    {
        //Get seconds and milliseconds
        int seconds = Mathf.RoundToInt(currentTime);
        double miliseconds = currentTime % 1;
        miliseconds = Math.Round(miliseconds, 2);
        miliseconds *= 100;

        //Update the text
        _timerText.text = seconds < 10 ? "0" + seconds + " :" + miliseconds : seconds + " :" + miliseconds;

        //Update second clock
        float secondsPassed = _totalTime - currentTime;
        Quaternion currentRot = _arrowSecondsImage.rectTransform.rotation;
        currentRot.eulerAngles = new Vector3(0, 0, -6 * secondsPassed);
        _arrowSecondsImage.rectTransform.rotation = currentRot;
    }

    private void ShowTimerUI(bool show)
    {
        _timerImage.gameObject.SetActive(show);
        _timerText.gameObject.SetActive(show);
        _arrowSecondsImage.gameObject.SetActive(show);
        _missingHolderCanvas.gameObject.SetActive(show);
    }

    //Result function
    private void PlayerWinUi()
    {
        ShowResultUI(true);

        _pauseRestartQuitImage.sprite = _winSprite;
    }

    private void PlayerLooseUI()
    {
        ShowResultUI(true);
        _pauseRestartQuitImage.sprite = _looseSprite;
    }

    private void ShowResultUI(bool show)
    {
        //We unlock the mouse
        Cursor.lockState = CursorLockMode.Confined;
        Cursor.visible = true;


        _background.gameObject.SetActive(show);
        _pauseRestartQuitCanvas.gameObject.SetActive(show);
        _pauseRestartQuitImage.gameObject.SetActive(show);
        _missingHolderCanvas.gameObject.SetActive(!show);
    }

    //Player win
    private void PlayerWin()
    {
        ShowTimerUI(false);
        PlayerWinUi();
    }

    //Explanation Canvas
    private void ShowExplanation1(bool show)
    {
        _background.gameObject.SetActive(show);
        _noteblock.gameObject.SetActive(show);
        _explanation1.gameObject.SetActive(show);
    }

    private void ShowExplanation2(bool show)
    {
        _background.gameObject.SetActive(show);
        _noteblock.gameObject.SetActive(show);
        _explanation2.gameObject.SetActive(show);
    }

    private void ShowExplanation3(bool show)
    {
        _background.gameObject.SetActive(show);
        _noteblock.gameObject.SetActive(show);
        _explanation3.gameObject.SetActive(show);
    }

    private void ShowPause(bool show)
    {
        ShowTimerUI(!show);
        _background.gameObject.SetActive(show);
        _missingHolderCanvas.gameObject.SetActive(!show);
        _pauseRestartQuitCanvas.gameObject.SetActive(show);
        _pauseRestartQuitImage.gameObject.SetActive(show);

        _pauseRestartQuitImage.sprite = _pauseSprite;

        if(show)
        {
            RelinkButtons();
        }
    }

    private void GetTotalTime(float time)
    {
        _totalTime = (int)time;
    }

    private void ShowCutc(bool show)
    {
        _cutcCanvas.gameObject.SetActive(show);
    }

    //Relink quit and restart buttons
    private void RelinkButtons()
    {
        GameManager manager = GameObject.Find("GameManager").GetComponent<GameManager>();

        if(manager != null)
        {
            _restartButton.onClick.AddListener(manager.Restart);
            _quitButton.onClick.AddListener(manager.Quit);
        }
    }
}
