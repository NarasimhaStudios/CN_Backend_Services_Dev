using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase.Extensions;
using Firebase.Auth;
using UnityEngine;
using UnityEngine.UI;
using Google;
using System.Net.Http;

public class BackendManager : MonoBehaviour
{

    [SerializeField] private string GoogleWebId;

    private GoogleSignInConfiguration config;

    private FirebaseAuth auth;
    private FirebaseUser user;

    public static BackendManager Instance;
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
            Instance = this;
        else
            Debug.LogError("DOUBLE SINGLETON");
    }
    private void Start()
    {
        InitializeFirebase();
    }
    private void InitializeFirebase()
    {
        auth = FirebaseAuth.DefaultInstance;
    }
    public void GoogleSignInClicked()
    {
        GoogleSignIn.Configuration = config;
        GoogleSignIn.Configuration.UseGameSignIn = false;
        GoogleSignIn.DefaultInstance.SignIn().ContinueWith(OnGoogleAuthFinished);
    }
    private void OnGoogleAuthFinished(Task<GoogleSignInUser> task)
    {
        if(task.IsFaulted || task.IsCanceled)
        {
            Debug.LogError("Fail");
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
                    user = auth.CurrentUser;
                    Debug.LogFormat("Login as {0}, {1}", user.DisplayName, user.Email);
                }
            });
        }
    }
    public void FacebookSignInClicked()
    {

    }
}