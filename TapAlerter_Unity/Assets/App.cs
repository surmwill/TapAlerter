// #define USE_EMULATOR

using System;
using System.IO;
using Firebase.Firestore;
using Firebase.Messaging;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class App : MonoBehaviour
{
    [SerializeField]
    private TMP_Text _tomorrowsLow = null;

    [SerializeField]
    private TMP_Text _tomorrowsDate = null;

    [SerializeField]
    private TMP_Text _warningTemperature = null;
    
    [SerializeField]
    private TMP_Text _message = null;

    [SerializeField]
    private Image _background = null;

    [SerializeField]
    private Button _refreshButton = null;

    [SerializeField]
    private Button _increaseButton = null;
    
    [SerializeField]
    private Button _decreaseButton = null;

    private DocumentReference UserDoc => FirebaseFirestore.DefaultInstance.Collection("Users").Document(_userDocName);
    
    private const string ConfigFileName = "config";

    private ListenerRegistration _listener;
    
    private string _userDocName;
    
    private void Start()
    {
        _userDocName = JsonUtility.FromJson<Config>(Resources.Load<TextAsset>(ConfigFileName).text).USER_DOC_GUID;
        
        FirebaseFirestore db = FirebaseFirestore.DefaultInstance;
        #if USE_EMULATOR
        db.Settings.Host = "localhost:8080";
        db.Settings.SslEnabled = false;
        #endif
        
        _refreshButton.onClick.AddListener(Refresh);
        _increaseButton.onClick.AddListener(IncrementWarningTemperature);
        _decreaseButton.onClick.AddListener(DecrementWarningTemperature);
        
        #if UNITY_ANDROID && !UNITY_EDITOR
        FirebaseMessaging.MessageReceived += OnMessageReceived;
        FirebaseMessaging.TokenReceived += OnTokenReceived;
        #endif
        
        _listener = UserDoc.Listen(snapshot => UpdateUI(snapshot.ConvertTo<UserData>()));
    }

    private void IncrementWarningTemperature()
    {
        IncrementWarningTemperature(1);
    }

    private void DecrementWarningTemperature()
    {
        IncrementWarningTemperature(-1);
    }

    private void IncrementWarningTemperature(int increment)
    {
        UserDoc.UpdateAsync("WarningTemperature", FieldValue.Increment(increment));
        Refresh();
    }
    
    private void OnTokenReceived(object sender, TokenReceivedEventArgs token) 
    {
        Debug.Log($"Token Received: {token.Token}");
        UserDoc.UpdateAsync("Token", token.Token);
        Refresh();
    }

    private void OnMessageReceived(object sender, MessageReceivedEventArgs e) 
    {
        // Empty
        Debug.Log("Message Received");
    }

    private void OnDestroy()
    {
        _refreshButton.onClick.RemoveListener(Refresh);
        _increaseButton.onClick.RemoveListener(IncrementWarningTemperature);
        _decreaseButton.onClick.RemoveListener(DecrementWarningTemperature);
        
        #if UNITY_ANDROID && !UNITY_EDITOR
        FirebaseMessaging.MessageReceived -= OnMessageReceived;
        FirebaseMessaging.TokenReceived -= OnTokenReceived;
        #endif
        
        _listener.Dispose();
    }

    private void Refresh()
    {
        UserDoc.UpdateAsync("Dirty", true);
    }

    private void UpdateUI(UserData userData)
    {
        _tomorrowsLow.text = $"Tommorow's low: {userData.TomorrowsLow}";
        _tomorrowsDate.text = $"Tomorrow's date: {userData.TomorrowsDate}";
        _warningTemperature.text = $"Warning temperature: {userData.WarningTemperature}";
        
        _background.color = userData.TomorrowsLow <= userData.WarningTemperature ? Color.red : Color.green;
        _message.text = userData.TomorrowsLow <= userData.WarningTemperature ? "Turn on the taps!" : "Taps are okay!";
    }

    [Serializable]
    private class Config
    {
        public string USER_DOC_GUID;
    }
    
    [FirestoreData]
    private class UserData
    {
        [FirestoreProperty]
        public bool Dirty { get; set; }
        
        [FirestoreProperty]
        public float Latitude { get; set; }
        
        [FirestoreProperty]
        public float Longitude { get; set; }
        
        [FirestoreProperty]
        public float TomorrowsLow { get; set; }
        
        [FirestoreProperty]
        public string TomorrowsDate { get; set; }
        
        [FirestoreProperty]
        public float WarningTemperature { get; set; }
        
        [FirestoreProperty]
        public string Token { get; set; }
    }
}