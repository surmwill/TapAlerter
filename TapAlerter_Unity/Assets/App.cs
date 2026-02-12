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
    
    private const string ConfigFileName = "config.json";

    private ListenerRegistration _listener;
    
    private string _userDocName;
    
    private void Start()
    {
        _userDocName = JsonUtility.FromJson<Config>(File.ReadAllText(Path.Combine(Application.streamingAssetsPath, ConfigFileName))).USER_DOC_GUID;
        
        FirebaseFirestore db = FirebaseFirestore.DefaultInstance;
        #if USE_EMULATOR
        db.Settings.Host = "localhost:8080";
        db.Settings.SslEnabled = false;
        #endif
        
        _refreshButton.onClick.AddListener(Refresh);
        
        #if UNITY_ANDROID && !UNITY_EDITOR
        FirebaseMessaging.MessageReceived += OnMessageReceived;
        FirebaseMessaging.TokenReceived += OnTokenReceived;
        #endif
        
        _listener = db.Collection("Users").Document(_userDocName).Listen(snapshot => UpdateUI(snapshot.ConvertTo<UserData>()));
    }
    
    private void OnTokenReceived(object sender, TokenReceivedEventArgs token) 
    {
        Debug.Log($"Token Received: {token.Token}");
        FirebaseFirestore.DefaultInstance.Collection("Users").Document(_userDocName).UpdateAsync("Token", token.Token);
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
        
        #if UNITY_ANDROID && !UNITY_EDITOR
        FirebaseMessaging.MessageReceived -= OnMessageReceived;
        FirebaseMessaging.TokenReceived -= OnTokenReceived;
        #endif
        
        _listener.Dispose();
    }

    private void Refresh()
    {
        FirebaseFirestore.DefaultInstance.Collection("Users").Document(_userDocName).UpdateAsync("Dirty", true);
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