using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase.Extensions;
using Firebase;
public class FriendSearchItem : MonoBehaviour
{
    public TextMeshProUGUI Name;
    public Button AddFriend;
    public string id;
    private void Start()
    {
        UpdateInfo();
    }
    public void UpdateInfo()
    {
        AddFriend.gameObject.SetActive(false);
        BackendManager.Database.RootReference.Child("requestsFrom").
            Child(BackendManager.Instance.user.Id).
            Child(id).
            GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                BackendManager.Database.RootReference.Child("friends").
                    Child(id).
                    Child(BackendManager.Instance.user.Id).
                    GetValueAsync().ContinueWithOnMainThread(task2 =>
                {
                    if (task2.IsCompleted)
                    {
                        if (!task.Result.Exists && !task2.Result.Exists)
                            AddFriend.gameObject.SetActive(true);
                    }
                });
            }
        });
    }
}