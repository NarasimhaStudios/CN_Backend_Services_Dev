using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using TMPro;
using Firebase.Database;
using Firebase.Extensions;

public class UIManager : MonoBehaviour
{
    public TextMeshProUGUI Name;
    public TextMeshProUGUI Score;
    public TMP_InputField ScoreInput;

    public TMP_InputField SearchInput;

    public GameObject LeaderboardItemPrefab;
    public GameObject LeaderboardContent;

    public GameObject FriendItemPrefab;
    public GameObject FriendContent;

    public GameObject FriendReqItemPrefab;
    public GameObject FriendReqContent;

    public GameObject FriendSearchItemPrefab;
    public GameObject FriendSearchContent;
    public bool IsLocal = false;
    User user;
    private void Start()
    {
        user = BackendManager.Instance.user;
        Name.text = BackendManager.Instance.user.Name;
        Score.text = BackendManager.Instance.user.Score.ToString();

        BackendManager.Database.RootReference.Child("friends").Child(user.Id).ChildAdded += FriendChildAdded;
        BackendManager.Database.RootReference.Child("friends").Child(user.Id).ChildRemoved += FriendChildRemoved;

        BackendManager.Database.RootReference.Child("requestsTo").Child(user.Id).ChildAdded += onRequestChildAdd;
        BackendManager.Database.RootReference.Child("requestsTo").Child(user.Id).ChildRemoved += onRequestChildRemove;
    }

    private void FriendChildAdded(object sender, ChildChangedEventArgs e)
    {
        AddToFriendsMenu(e.Snapshot.Key);
    }
    private void FriendChildRemoved(object sender, ChildChangedEventArgs e)
    {
        RemoveFromFriendsMenu(e.Snapshot.Key);
    }

    public void UpdateScore()
    {
        Score.text = ScoreInput.text;
        BackendManager.Instance.UpdateScore(int.Parse(ScoreInput.text));
    }
    private List<GameObject> leaderboardItems = new List<GameObject>();
    private void UpdateLeaderboard()
    {
        BackendManager.Database.RootReference.Child("users").OrderByChild("Score").GetValueAsync().ContinueWithOnMainThread(task =>
        {
            foreach (var a in leaderboardItems)
            {
                Destroy(a);
            }
            leaderboardItems.Clear();
            foreach (var a in task.Result.Children)
            {
                User temp = JsonUtility.FromJson<User>(a.GetRawJsonValue());
                if(IsLocal && user.Country != temp.Country)
                {
                    continue;
                }
                var obj = Instantiate(LeaderboardItemPrefab, LeaderboardContent.transform);
                var objj = obj.GetComponent<LeaderboardItem>();
                obj.transform.SetSiblingIndex(0);
                leaderboardItems.Add(obj);
                objj.Name.text = temp.Name;
                objj.Score.text = temp.Score.ToString();
            }
        });
    }
    public void LocalLeaderboard()
    {
        IsLocal = true;
        UpdateLeaderboard();
    }
    public void Leaderboard()
    {
        IsLocal = false;
        UpdateLeaderboard();
    }

    private List<GameObject> friendsSearchItems = new List<GameObject>();
    public void UpdateSearch()
    {
        BackendManager.Database.RootReference.Child("users").GetValueAsync().ContinueWithOnMainThread(task =>
        {
            foreach (var a in friendsSearchItems)
            {
                Destroy(a);
            }
            friendsSearchItems.Clear();
            foreach (var a in task.Result.Children)
            {
                User temp = JsonUtility.FromJson<User>(a.GetRawJsonValue());
                if (temp.Name.ToLower().Contains(SearchInput.text.ToLower()) && temp.Id != user.Id)
                {
                    var obj = Instantiate(FriendSearchItemPrefab, FriendSearchContent.transform);
                    var objj = obj.GetComponent<FriendSearchItem>();
                    friendsSearchItems.Add(obj);
                    objj.Name.text = temp.Name;
                    objj.id = temp.Id;
                    objj.AddFriend.onClick.AddListener(() => { SendRequest(temp.Id); objj.UpdateInfo(); });
                }
            }
        });
    }
    void SendRequest(string idTo)
    {
        //BackendManager.Database.RootReference.Child("requestsTo").Child(user.Id).Child(idTo).GetValueAsync().ContinueWithOnMainThread(task =>
        //{
        //    if (!task.Result.Exists)
        //    {
                BackendManager.Database.RootReference.Child("requestsTo").Child(idTo).Child(user.Id).SetValueAsync(1);
                BackendManager.Database.RootReference.Child("requestsFrom").Child(user.Id).Child(idTo).SetValueAsync(1);
        //    }
        //});
    }
    private void onRequestChildAdd(object sender, ChildChangedEventArgs args)
    {
        AddToFriendsRequestMenu(args.Snapshot.Key);
    }
    private void onRequestChildRemove(object sender, ChildChangedEventArgs args)
    {
        RemoveFromFriendsRequestMenu(args.Snapshot.Key);
    }
    private Dictionary<string, GameObject> FriendRequests = new();
    public void AddToFriendsRequestMenu(string uid)
    {
        BackendManager.Database.RootReference.Child("users").Child(uid).GetValueAsync().ContinueWithOnMainThread(friendData =>
        {
            User friend = JsonUtility.FromJson<User>(friendData.Result.GetRawJsonValue());
            GameObject temp = Instantiate(FriendReqItemPrefab, FriendReqContent.transform);
            temp.transform.SetAsFirstSibling();
            FriendReqItem friendItem = temp.GetComponent<FriendReqItem>();
            friendItem.Name.text = friend.Name;
            friendItem.id = friend.Id;
            FriendRequests.Add(uid, temp);
            Debug.LogFormat("{0} added to friend menu, {1}", uid, FriendRequests[uid].name);
            friendItem.Accept.onClick.AddListener(() => { accept(uid); });
            friendItem.Decline.onClick.AddListener(() => { decline(uid); });
        });
    }
    private Dictionary<string, GameObject> Friend = new();
    public void AddToFriendsMenu(string uid)
    {
        BackendManager.Database.RootReference.Child("users").Child(uid).GetValueAsync().ContinueWithOnMainThread(friendData =>
        {
            User friend = JsonUtility.FromJson<User>(friendData.Result.GetRawJsonValue());
            GameObject temp = Instantiate(FriendItemPrefab, FriendContent.transform);
            temp.transform.SetAsFirstSibling();
            FriendItem friendItem = temp.GetComponent<FriendItem>();
            friendItem.Name.text = friend.Name;
            Friend.Add(uid, temp);
            Debug.LogFormat("{0} added to friend menu, {1}", uid, Friend[uid].name);
            friendItem.RemoveFriend.onClick.AddListener(() => { remove(uid); });
        });
    }
    public void RemoveFromFriendsRequestMenu(string uid)
    {
        Destroy(FriendRequests[uid]);
        FriendRequests.Remove(uid);
    }
    public void RemoveFromFriendsMenu(string uid)
    {
        Destroy(Friend[uid]);
        Friend.Remove(uid);
    }
    private void accept(string id)
    {
        BackendManager.Database.RootReference.Child("requestsTo").Child(user.Id).Child(id).GetValueAsync().ContinueWithOnMainThread(task =>
        {
        if (task.IsCompleted)
            {

            BackendManager.Database.RootReference.Child("requestsTo").Child(user.Id).Child(id).RemoveValueAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted)
                {
                    BackendManager.Database.RootReference.Child("friends").Child(user.Id).Child(id).SetValueAsync(1);
                    BackendManager.Database.RootReference.Child("friends").Child(id).Child(user.Id).SetValueAsync(1);
                    BackendManager.Database.RootReference.Child("requestsFrom").Child(id).Child(user.Id).RemoveValueAsync();
                    BackendManager.Database.RootReference.Child("requestsFrom").Child(user.Id).Child(id).RemoveValueAsync();
                    BackendManager.Database.RootReference.Child("requestsTo").Child(id).Child(user.Id).RemoveValueAsync();
                    BackendManager.Database.RootReference.Child("requestsTo").Child(user.Id).Child(id).RemoveValueAsync();
                }
            });
            }
        });
    }
    private void decline(string id)
    {
        BackendManager.Database.RootReference.Child("requestsTo").Child(user.Id).Child(id).RemoveValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                BackendManager.Database.RootReference.Child("requestsFrom").Child(id).Child(user.Id).RemoveValueAsync();
            }
        });
    }
    private void remove(string id)
    {
        BackendManager.Database.RootReference.Child("friends").Child(user.Id).Child(id).RemoveValueAsync();
        BackendManager.Database.RootReference.Child("friends").Child(id).Child(user.Id).RemoveValueAsync();
    }
}