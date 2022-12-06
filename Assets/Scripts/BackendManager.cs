using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase.Extensions;
using Firebase.Auth;
using Firebase.Database;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Facebook.Unity;
using Google;
using System.Globalization;
using UnityEngine.Networking;
public class BackendManager : MonoBehaviour
{

    [SerializeField] private string GoogleWebId;
    private GoogleSignInConfiguration config;

    private FirebaseAuth auth;
    private static FirebaseDatabase database;
    public static FirebaseDatabase Database
    {
        get
        {
            return database;
        }
    }    
    private FirebaseUser fUser;

    public User user;

    public static BackendManager Instance;

    public GameObject BlockImage;
    private void Awake()
    {
        Singleton();
        config = new GoogleSignInConfiguration
        {
            WebClientId = GoogleWebId,
            RequestIdToken = true,
            RequestEmail = true,
            UseGameSignIn = false,
        };
    }
    private void Singleton()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Debug.LogError("DOUBLE SINGLETON");
            Destroy(gameObject);
        }
    }
    private void Start()
    {
        InitializeFirebase();
        FB.Init(InitCallBack, OnHideUnity);
    }
    private void InitCallBack()
    {
        if (!FB.IsInitialized)
        {
            FB.ActivateApp();
        }
    }
    private void OnHideUnity(bool isgameshown)
    {
        if (!isgameshown)
        {
            Time.timeScale = 0;
        }
        else
        {
            Time.timeScale = 1;
        }
    }

    private void InitializeFirebase()
    {
        auth = FirebaseAuth.DefaultInstance;
        database = FirebaseDatabase.DefaultInstance;
    }
    public void GoogleSignInClicked()
    {
        BlockImage.SetActive(true);
        GoogleSignIn.Configuration = config;
        GoogleSignIn.Configuration.UseGameSignIn = false;
        GoogleSignIn.DefaultInstance.SignIn().ContinueWith(OnGoogleAuthFinished);
    }
    private void OnGoogleAuthFinished(Task<GoogleSignInUser> task)
    {
        if(task.IsFaulted || task.IsCanceled)
        {
            Debug.LogError("Fail");
            BlockImage.SetActive(false);
        }
        else
        {
            Credential credential = GoogleAuthProvider.GetCredential(task.Result.IdToken, null);
            auth.SignInWithCredentialAsync(credential).ContinueWithOnMainThread(task =>
            {
                if (task.IsCanceled || task.IsFaulted)
                {
                    Debug.LogError("Fail");
                }
                else
                {
                    fUser = auth.CurrentUser;
                    Debug.LogFormat("Login as {0}, {1}", fUser.DisplayName, fUser.Email);
                    user = new User(fUser.DisplayName, fUser.UserId, "", 0);
                    AddToDatabase(user);
                }
            });
        }
    }
    public void FacebookSignInClicked()
    {
        BlockImage.SetActive(true);
        var perms = new List<string>() { "gaming_profile", "email", "public_profile", "name" };
        FB.LogInWithReadPermissions(perms, OnFacebookLogin);
    }

    private void OnFacebookLogin(ILoginResult result)
    {
        if (FB.IsLoggedIn)
        {
            var accessToken = result.AccessToken;
            Debug.Log("/////" + result.RawResult);
            Debug.LogFormat("trying to firebase, 1: {0}", accessToken.TokenString);
            Credential credential = FacebookAuthProvider.GetCredential(accessToken.TokenString);
            auth.SignInWithCredentialAsync(credential).ContinueWith(task => {
                if (task.IsCanceled || task.IsFaulted)
                {
                    Debug.LogError("SignInWithCredentialAsync was canceled or failed");
                    BlockImage.SetActive(false);
                    return;
                }

                fUser = task.Result;
            });
            FB.API("/me?fields=name", HttpMethod.GET, NameCallBack);
        }
        else
        {
            Debug.LogError(result.Error);
            BlockImage.SetActive(false);
        }
    }
    private void NameCallBack(IGraphResult result)
    {
        user = new User(result.ResultDictionary["name"].ToString(), fUser.UserId, RegionInfo.CurrentRegion.DisplayName, 0);
        Debug.LogFormat("User name: {0}, id: {1}", user.Name, user.Id);
        AddToDatabase(user);
    }

    private void AddToDatabase(User user)
    {
        StartCoroutine(AddToDatabaseIE(user));
    }
    [Serializable]
    public class IpApiData
    {
        public string country_name;

        public static IpApiData CreateFromJSON(string jsonString)
        {
            return JsonUtility.FromJson<IpApiData>(jsonString);
        }
    }
    public IEnumerator AddToDatabaseIE(User user)
    {
        string ip = new System.Net.WebClient().DownloadString("https://api.ipify.org");
        string uri = $"https://ipinfo.io/{ip}/country/";


        using (UnityWebRequest webRequest = UnityWebRequest.Get(uri))
        {
            yield return webRequest.SendWebRequest();

            user.Country = webRequest.downloadHandler.text;

            database.RootReference.Child("users").Child(user.Id).
                GetValueAsync().ContinueWithOnMainThread(task => 
                {
                    if(!task.Result.Exists)
                    {
                        database.RootReference.Child("users").Child(user.Id).SetRawJsonValueAsync(JsonUtility.ToJson(user));
                    }
                    else
                    {
                        user = JsonUtility.FromJson<User>(task.Result.GetRawJsonValue());
                    }
                });
            SceneManager.LoadScene(1);
        }
    }
    public void UpdateScore(int score)
    {
        user.Score = score;
        database.RootReference.Child("users").Child(user.Id).Child("Score").SetValueAsync(score);
    }
}
public class Country
{

    public string businessName;
    public string businessWebsite;
    public string city;
    public string continent;
    public string country;
    public string countryCode;
    public string ipName;
    public string ipType;
    public string isp;
    public string lat;
    public string lon;
    public string org;
    public string query;
    public string region;
    public string status;

}
[Serializable] public class User
{
    public User(string name, string id, string country, int score)
    {
        Name = name;
        Id = id;
        Country = country;
        Score = score;
    }
    public string Name;
    public string Id;
    public string Country;
    public int Score;
}