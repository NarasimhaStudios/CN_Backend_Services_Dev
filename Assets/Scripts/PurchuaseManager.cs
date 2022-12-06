using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Purchasing;
using TMPro;

public class PurchuaseManager : MonoBehaviour
{
    public GameObject BuyOnceButton;
    public TextMeshProUGUI Coins;

    public void OnPurchaseCompleted(Product product)
    {
        switch (product.definition.id)
        {
            case "com.ashwinnarasimha.backend.once":
                BuyOnce();
                break;
            case "com.ashwinnarasimha.backend.buy":
                Buy();
                break;
        }
    }
    private void Start()
    {
        if(PlayerPrefs.GetInt("Bought") == 1)
        {
            BuyOnceButton.SetActive(false);
        }
        Coins.text = "Coins: " + PlayerPrefs.GetInt("Coins");
    }
    public void BuyOnce()
    {
        PlayerPrefs.SetInt("Bought", 1);
        BuyOnceButton.SetActive(false);
    }
    public void Buy()
    {
        PlayerPrefs.SetInt("Coins", PlayerPrefs.GetInt("Coins") + 1);
        Coins.text = "Coins: " + PlayerPrefs.GetInt("Coins");
    }
}