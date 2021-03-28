using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CotcSdk;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;


public class CotcManager : MonoBehaviour
{
    //Basic Cotc object
    private Cloud Cloud;
    private Gamer Gamer;
    private DomainEventLoop Loop;

    //Serialize field elements
    [SerializeField]
    private Canvas cotcCanvas;

    [SerializeField]
    private TMP_InputField EmailInput;

    [SerializeField]
    private TMP_InputField passwordInput;

    [SerializeField]
    private TextMeshProUGUI displayInfoText;

    [SerializeField]
    private Button logAnnoButton;

    [SerializeField]
    private Button logButton;

    [SerializeField]
    private TextMeshProUGUI convertText;

    [SerializeField]
    private Button convertButton;

    [SerializeField]
    private Button playButton;

    //Leaderboard
    [SerializeField]
    private Canvas leaderboardCanvas;

    [SerializeField]
    private List<TextMeshProUGUI> leaderboardPlayerText;

    //Player's info
    [SerializeField]
    private Canvas playersInfoCanvas;

    [SerializeField]
    private TMP_InputField playerNameInputField;

    [SerializeField]
    private TMP_InputField playerEmailInputField;

    [SerializeField]
    private TextMeshProUGUI displayPlayersInfoText;

    //Convert ano account
    [SerializeField]
    private Canvas convertAnonymousAccountCanvas;

    [SerializeField]
    private TMP_InputField anoEmailInputField;

    [SerializeField]
    private TMP_InputField anoPasswordInputField;

    [SerializeField]
    private TextMeshProUGUI displayAnoAccountInfoText;

    [SerializeField]
    private Button convertButtonAno;

    //Default parameters
    private float endCoutdown = 90f;
    private string leaderboardName = "HurryOfficeTime";
    
    [SerializeField]
    private bool isLog = false;

    [SerializeField]
    private bool isLogAno = false;

    //Action
    public static Action<bool> ShowLog;
    public static Action StartGame;
    public static Action Logout;

    //Singleton
    private static CotcManager Instance = null;

    //-------------------------------------------------------
    //Initialization
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
        //Register to action
        CoutdownManager.EndCoutdown += SetEncCoutdown;
        GameManager.RestartGame += Restart;

        //Show ui
        cotcCanvas.gameObject.SetActive(true);

        InitializeCotc();

        //Try to load player info
        LoadFile();
    }

    private void InitializeCotc()
    {
        // Link with the CotC Game Object
        var cb = FindObjectOfType<CotcGameObject>();
        if (cb == null)
        {
            Debug.LogError("Please put a Clan of the Cloud prefab in your scene!");
            return;
        }

        // Log unhandled exceptions (.Done block without .Catch -- not called if there is any .Then)
        Promise.UnhandledException += (object sender, ExceptionEventArgs e) => {
            Debug.LogError("Unhandled exception: " + e.Exception.ToString());
        };

        // Initiate getting the main Cloud object
        cb.GetCloud().Done(cloud => {
            Cloud = cloud;
            // Retry failed HTTP requests once
            Cloud.HttpRequestFailedHandler = (HttpRequestFailedEventArgs e) => {
                if (e.UserData == null)
                {
                    e.UserData = new object();
                    e.RetryIn(1000);
                }
                else
                    e.Abort();
            };
            Debug.Log("Setup done");
        });
    }

    private void Restart()
    {
        if(!isLog)
        {
            cotcCanvas.gameObject.SetActive(true);
        }

        else
        {
            Play();
        }
    }

    //-------------------------------------------------------
    //Log in
    //Anonymous account
    public void LogAnonymousAccount()
    {
        Cloud.LoginAnonymously()
            .Then(gamer =>
            {
                DidLogin(gamer);
                displayInfoText.text = "Login anonymously successfully";
                Debug.Log("Gamer id: " + gamer["gamer_id"]);

                //show play button
                playButton.gameObject.SetActive(true);

                //show convert button
                ShowConvert(true);

                //hide log button
                ShowLogButton(false);

                //Change bool
                isLog = true;
                isLogAno = true;

                //SaveFile
                SaveType player = new SaveType { networkType = gamer["network"], playerId = gamer["gamer_id"], playerSecret = gamer["gamer_secret"] };
                string data = JsonUtility.ToJson(player);
                SaveFile(data);
            }) 
            .Catch(ex => {
                // The exception should always be CotcException
                CotcException error = (CotcException)ex;
                displayInfoText.text = "Failed to login anonymously";
                Debug.LogError("Failed to login anonymously : " + error.ErrorCode + " (" + error.HttpStatusCode + ")");
            });
    }

    //Email account
    public void LogEmailAccount()
    {
        Cloud.Login(
            network: LoginNetwork.Email.Describe(),
            networkId: EmailInput.text,
            networkSecret: passwordInput.text)
        .Done(gamer =>
        {
            DidLogin(gamer);
            displayInfoText.text = "Login successfully";
            Debug.Log("Log in succesfully");
            Debug.Log("Gamer id: " + gamer["gamer_id"]);

            //hide log button
            ShowLogButton(false);

            //show play button
            playButton.gameObject.SetActive(true);

            //Change bool
            isLog = true;

            //SaveFile
            SaveType player = new SaveType { networkType = gamer["network"], playerId = gamer["gamer_id"], 
                playerSecret = gamer["gamer_secret"], networkID = gamer["networkid"], networkSecret = passwordInput.text
            };
            string data = JsonUtility.ToJson(player);
            SaveFile(data);
        }, ex =>
        {
            // The exception should always be CotcException
            CotcException error = (CotcException)ex;

            string myError = PrepareErrorMessage(error.ToString());

            //print error message
            displayInfoText.text = "Failed to login : " + myError;
            Debug.LogError("Failed to login: " + error.ErrorCode + " (" + error.HttpStatusCode + ")");
        });
    }

    //Convert your ano to email account
    public void ConvertToEmailAccount()
    {
        if (!GamerIsConnected()) return;
        Gamer.Account.Convert(
            network: LoginNetwork.Email.ToString().ToLower(),
            networkId: EmailInput.text,
            networkSecret: passwordInput.text)
        .Done(dummy => {
            displayInfoText.text = "Successfully converted account";
            Debug.Log("Successfully converted account");

            //show convert button
            ShowConvert(false);

            //Change bool
            isLogAno = false;

            //SaveFile
            SaveType player = new SaveType { networkType = Gamer["network"], playerId = Gamer["gamer_id"], 
                playerSecret = Gamer["gamer_secret"], networkID = Gamer["networkid"], networkSecret = passwordInput.text
            };
            string data = JsonUtility.ToJson(player);
            SaveFile(data);
        }, ex =>
        {
            // The exception should always be CotcException
            CotcException error = (CotcException)ex;

            string myError = PrepareErrorMessage(error.ToString());

            //print error message
            displayInfoText.text = "Failed to convert : " + myError;
            Debug.LogError("Failed to convert: " + error.ErrorCode + " (" + error.ErrorInformation + ")");
        });
    }

    // Invoked when any sign in operation has completed
    private void DidLogin(Gamer newGamer)
    {
        //Kick the old gimer if a new one connect
        if (Gamer != null)
        {
            Debug.LogWarning("Current gamer " + Gamer.GamerId + " has been dismissed");
            Loop.Stop();
        }
        Gamer = newGamer;
        Loop = Gamer.StartEventLoop();
        Loop.ReceivedEvent += Loop_ReceivedEvent;
        Debug.Log("Signed in successfully (ID = " + Gamer.GamerId + ")");
    }

    private void Loop_ReceivedEvent(DomainEventLoop sender, EventLoopArgs e)
    {
        Debug.Log("Received event of type " + e.Message.Type + ": " + e.Message.ToJson());
    }

    private bool GamerIsConnected()
    {
        if (Gamer == null)
        {
            Debug.LogError("You need to login first. Click on a login button.");
            displayInfoText.text = "You need to login first. Click on a login button.";
        }
        return Gamer != null;
    }

   //Logout
    public void LogOut()
    {
       Cloud.Logout(Gamer)
        .Done(result => {
            Debug.Log("Logout succeeded");
            if (Logout != null)
            {
                Logout();

                //Prepapre ui
                cotcCanvas.gameObject.SetActive(true);
                ShowLogButton(true);
                displayInfoText.text = "";
                playButton.gameObject.SetActive(false);
                EmailInput.text ="";
                passwordInput.text = "";

                //Prepare mouse
                 Cursor.lockState = CursorLockMode.Confined;
                Cursor.visible = true;

                //Change bool
                isLog = false;
                isLogAno = false;
            }
        }, ex => {
           // The exception should always be CotcException
           CotcException error = (CotcException)ex;
           Debug.LogError("Failed to logout: " + error.ErrorCode + " (" + error.HttpStatusCode + ")");
        });
    }

    private string PrepareErrorMessage(string errorMessage)
    {
        //Get error message
        string toBeSearched = "\"message\":";
        string myError = errorMessage.Substring(errorMessage.IndexOf(toBeSearched) + toBeSearched.Length);
        myError = myError.Substring(1);
        myError = myError.Remove(myError.Length - 3);
        return myError;
    }

    //-------------------------------------------------------
    //REQUEST
    public void FindUsers()
    {
        Cloud.ListUsers("", 30, 0)
        .Done(listUsersRes => {
            foreach (var userInfo in listUsersRes)
            {
                Debug.Log("User: " + userInfo.ToString());
            }
        }, ex => {
            // The exception should always be CotcException
            CotcException error = (CotcException)ex;
            Debug.LogError("Failed to list users: " + error.ErrorCode + " (" + error.ErrorInformation + ")");
        });
    }

    //--------------------------------------------------------
    //UI

    //Show log button
    private void ShowLogButton(bool show)
    {
        logAnnoButton.gameObject.SetActive(show);
        logButton.gameObject.SetActive(show);
    }

    //Show convert
    private void ShowConvert(bool show)
    {
        convertText.gameObject.SetActive(show);
        convertButton.gameObject.SetActive(show);
    }

    //Leaderboard display
    public void ShowLeaderboard()
    {
        leaderboardCanvas.gameObject.SetActive(true);
        PrintLeaderboard();
    }

    public void HideLeaderboard()
    {
        leaderboardCanvas.gameObject.SetActive(false);
    }

    //-------------------------------------------------------
    //Leaderboard
    private void PrintLeaderboard()
    {
        Gamer.Scores.Domain("private").BestHighScores(leaderboardName, 5, 1)
        .Done(bestHighScoresRes => {

            //Print each score
            for(int i = 0; i< bestHighScoresRes.Count; i++)
            {
                string leaderboardText = "Name : " + bestHighScoresRes[i].GamerInfo["profile"]["displayName"] + " - Time : " + bestHighScoresRes[i].Value + " sec";
                leaderboardPlayerText[i].text = leaderboardText;
            }
            
        }, ex => {
            CotcException error = (CotcException)ex;
            Debug.LogError("Could not get best high scores: " + error.ErrorCode + " (" + error.ErrorInformation + ")");
        });
    }

    private void SendScoreToLeaderboard()
    {
        Debug.Log("Total time = " + endCoutdown);
        Gamer.Scores.Domain("private").Post((long)endCoutdown, leaderboardName, ScoreOrder.LowToHigh,
        "Time", false)
        .Done(postScoreRes => {
            Debug.Log("Post score: " + postScoreRes.ToString());
        }, ex => {
            CotcException error = (CotcException)ex;
            Debug.LogError("Could not post score: " + error.ErrorCode + " (" + error.ErrorInformation + ")");
        });
    }

    //If we win, this is called, and it will send the player score to the leaderboard
    private void SetEncCoutdown(float c)
    {
        endCoutdown = c;
        SendScoreToLeaderboard();
    }

    //-------------------------------------------------------
    //Start the game
    public void Play()
    {
        if (StartGame != null)
        {
            StartGame();
        }

        if (ShowLog != null)
        {
            ShowLog(false);
        }
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    //-------------------------------------------------------
    //Getter
    public bool GetIsLog()
    {
        return isLog;
    }

    //-------------------------------------------------------
    //Players info
    public void ShowPlayersInfoCanvas()
    {
        //If player is log ano, we send him to a convert account canvas 
        if(isLogAno)
        {
            ShowPlayerConvertCanvas();
            return;
        }

        //Show ui
        playersInfoCanvas.gameObject.SetActive(true);
        displayPlayersInfoText.text = "";

        //Get players info
        Gamer.Profile.Get()
        .Done(profileRes => {
            Debug.Log("Profile data: " + profileRes.ToString());

            //Clear input field
            playerNameInputField.text = "";
            playerEmailInputField.text = "";

            //Show on the input field the current player data
            string currentPlayerName = profileRes["displayName"];
            string currentPlayerEmail = profileRes["email"];
            playerNameInputField.placeholder.gameObject.GetComponent<TextMeshProUGUI>().text = currentPlayerName;
            playerEmailInputField.placeholder.gameObject.GetComponent<TextMeshProUGUI>().text = currentPlayerEmail;
        }, ex => {
            // The exception should always be CotcException
            CotcException error = (CotcException)ex;
            Debug.LogError("Could not get profile data due to error: " + error.ErrorCode + " (" + error.ErrorInformation + ")");

            //Print error message
            string myError = PrepareErrorMessage(error.ToString());
            displayPlayersInfoText.text = myError;
        });

    }

    public void HidePlayersInfoCanvas()
    {
        //Hide ui
        playersInfoCanvas.gameObject.SetActive(false);
    }

    public void ChangePlayersInfo()
    {
        //Clean display
        displayPlayersInfoText.text = "";

        //check info
        string newName = playerNameInputField.text;
        if(newName.Length !=0 && newName.Length > 15)
        {
            displayPlayersInfoText.text = "The name must be 15 characters max";
            return;
        }

        string newEmail = playerEmailInputField.text;
        if(newEmail.Length != 0 &&  !VerifyEmailAddress(newEmail))
        {
            displayPlayersInfoText.text = "The email adress is invalid";
            return;
        }


        //Prepare new info
        Bundle profileUpdates = Bundle.CreateObject();
        if(newName.Length != 0)
        {
            profileUpdates["displayName"] = new Bundle(newName);
        }

        if(newEmail.Length != 0)
        {
            profileUpdates["email"] = new Bundle(newEmail);
        }

        //if ther's no info, return
        if(newName.Length == 0 && newEmail.Length == 0)
        {
            return;
        }

        //Send them !
        Gamer.Profile.Set(profileUpdates)
        .Done(profileRes => {
            Debug.Log("Profile data set: " + profileRes.ToString());
            displayPlayersInfoText.text = "Change successfully applied !";

            //Get the current save
            string oldData = File.ReadAllText(Application.dataPath + "/playerData.txt");
            SaveType loadGamer = JsonUtility.FromJson<SaveType>(oldData);

            //Change it
            loadGamer.networkID = newEmail;

            //Save it
            string data = JsonUtility.ToJson(loadGamer);
            SaveFile(data);
        }, ex => {
            // The exception should always be CotcException
            CotcException error = (CotcException)ex;
            Debug.LogError("Could not set profile data due to error: " + error.ErrorCode + " (" + error.ErrorInformation + ")");

            //Print error message
            string myError = PrepareErrorMessage(error.ToString());
            displayPlayersInfoText.text = myError;
        });
    }

    //-------------------------------------------------------
    //Convert canvas
    public void ShowPlayerConvertCanvas()
    {
        //Show ui
        convertAnonymousAccountCanvas.gameObject.SetActive(true);
        displayAnoAccountInfoText.text = "";
        anoEmailInputField.text = "";
        anoPasswordInputField.text = "";
        convertButtonAno.gameObject.SetActive(true);
    }

    public void ConvertAccount()
    {
        if(anoEmailInputField.text.Length == 0 || anoPasswordInputField.text.Length == 0)
        {
            displayAnoAccountInfoText.text = "Please, fill the email and password input field";
            return;
        }

        //Try to convert
        if (!GamerIsConnected()) return;
        Gamer.Account.Convert(
            network: LoginNetwork.Email.ToString().ToLower(),
            networkId: anoEmailInputField.text,
            networkSecret: anoPasswordInputField.text)
        .Done(dummy => {
            displayAnoAccountInfoText.text = "Successfully converted account";
            Debug.Log("Successfully converted account");

            //hide convert button
            convertButtonAno.gameObject.SetActive(false);

            //Change bool
            isLogAno = false;

            //SaveFile
            SaveType player = new SaveType { networkType = Gamer["network"], playerId = Gamer["gamer_id"], 
                playerSecret = Gamer["gamer_secret"], networkID = Gamer["networkid"], networkSecret = anoPasswordInputField.text
            };
            string data = JsonUtility.ToJson(player);
            SaveFile(data);
        }, ex =>
        {
            // The exception should always be CotcException
            CotcException error = (CotcException)ex;

            string myError = PrepareErrorMessage(error.ToString());

            //print error message
            displayAnoAccountInfoText.text = "Failed to convert : " + myError;
            Debug.LogError("Failed to convert: " + error.ErrorCode + " (" + error.ErrorInformation + ")");
        });

    }

    public void HidePlayerConvertCanvas()
    {
        //Hide ui
        convertAnonymousAccountCanvas.gameObject.SetActive(false);
    }

    //--------------------------------------------------------
    //Validation function (thanks melmonkey)
    static bool VerifyEmailAddress(String email)
    {
        string[] atCharacter;
        string[] dotCharacter;
        atCharacter = email.Split("@"[0]);
        if (atCharacter.Length == 2)
        {
            dotCharacter = atCharacter[1].Split("."[0]);
            if (dotCharacter.Length >= 2)
            {
                if (dotCharacter[dotCharacter.Length - 1].Length == 0)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
            else
            {
                return false;
            }
        }
        else
        {
            return false;
        }
    }


    //--------------------------------------------------------
    //Load and save function
    private void LoadFile()
    {
        //Check if the file exist
        if(File.Exists(Application.dataPath + "/playerData.txt"))
        {
            //Get the data
            string data = File.ReadAllText(Application.dataPath + "/playerData.txt");
            SaveType loadGamer = JsonUtility.FromJson<SaveType>(data);

            //Try to resume session
            var cotc = FindObjectOfType<CotcGameObject>();
            cotc.GetCloud().Done(cloud => {
                Cloud.ResumeSession(
                    gamerId: loadGamer.playerId,
                    gamerSecret: loadGamer.playerSecret)
                .Done(gamer => {
                    Debug.Log("Signed in succeeded (ID = " + gamer.GamerId + ")");
                    Debug.Log("Login data: " + gamer);
                    Debug.Log("Server time: " + gamer["servertime"]);

                    //Save gamer
                    DidLogin(gamer);

                    //Change bool
                    isLog = true;
                    isLogAno = loadGamer.networkType == "anonymous" ? true : false;

                    //It work ! Let's play
                    Play();
                }, ex => {
                // The exception should always be CotcException
                CotcException error = (CotcException)ex;
                    string myError = PrepareErrorMessage(error.ToString());

                    //print error message
                    displayInfoText.text = "Failed to login : " + myError;
                    Debug.LogError("Failed to login: " + error.ErrorCode + " (" + error.HttpStatusCode + ")");
                });
            });
        }
    }

    private void SaveFile(string data)
    {
        //Save the recieved data
        File.WriteAllText(Application.dataPath + "/playerData.txt", data);
    }

    private class SaveType
    {
        public string networkType;
        public string playerId;
        public string playerSecret;
        public string networkID;
        public string networkSecret;
    }
}
