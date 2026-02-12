using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase.Extensions;
using Firebase.Firestore;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
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
    private Button _clearButton = null;

    [SerializeField]
    private Button _refreshButton = null;

    [SerializeField]
    private Button _testGetWeatherButton = null;
    
    private void Start()
    {
        _refreshButton.onClick.AddListener(UpdateTemperature);
        _testGetWeatherButton.onClick.AddListener(TestGetWeather);
        
        UpdateTemperature();
    }

    private void OnDestroy()
    {
        _refreshButton.onClick.RemoveListener(UpdateTemperature);
        _testGetWeatherButton.onClick.RemoveListener(TestGetWeather);
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (!pauseStatus)
        {
            UpdateTemperature();
        }
    }

    private void UpdateTemperature()
    {
        UpdateTemperatureAsync().ContinueWithOnMainThread(t =>
        {
            if (!t.IsCompletedSuccessfully)
            {
                Debug.LogError($"Failed to update temperature: {t.Exception?.InnerException}");
            }
        });
    }

    private async Task UpdateTemperatureAsync()
    {
        UserData userData = await FetchUserAsync();

        _tomorrowsLow.text = $"Tommorow's low: {userData.TomorrowsLow}";
        _tomorrowsDate.text = $"Tomorrow's date: {userData.TomorrowsDate}";
        _warningTemperature.text = $"Warning temperature: {userData.WarningTemperature}";
        
        _background.color = userData.TomorrowsLow <= userData.WarningTemperature ? Color.red : Color.green;
        _message.text = userData.TomorrowsLow <= userData.WarningTemperature ? "Turn on the taps!" : "Taps are okay!";
    }
    
    private async Task<UserData> FetchUserAsync()
    {
        DocumentSnapshot document = await FirebaseFirestore.DefaultInstance.Collection("Users").Document("User").GetSnapshotAsync();
        return document.ConvertTo<UserData>();;
    }

    private void TestGetWeather()
    {
        StartCoroutine(TestGetWeatherCoroutine());
    }
    
    private IEnumerator TestGetWeatherCoroutine(float latitude = 45.5019f, float longitude = -73.561668f)
    {
        using (UnityWebRequest webRequest = UnityWebRequest.Get($"https://api.open-meteo.com/v1/forecast?latitude={latitude}&longitude={longitude}&daily=temperature_2m_min&timezone=auto"))
        {
            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                ForecastData data = JsonUtility.FromJson<ForecastData>(webRequest.downloadHandler.text);
                if (data.daily?.temperature_2m_min.Count >= 2)
                {
                    float tomorrowLow = data.daily.temperature_2m_min[1];
                    string tomorrowDate = data.daily.time[1];
                    Debug.Log($"Tomorrow's Low ({tomorrowDate}): {tomorrowLow}°C");
                }
            }
            else
            {
                Debug.LogError($"Weather request failed: {webRequest.error}");
            }
        }
    }
    
    [FirestoreData]
    private class UserData
    {
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
    }
    
    #region Weather_Parsing
    
    [Serializable]
    public class ForecastData
    {
        public DailyData daily;
    }

    [Serializable]
    public class DailyData
    {
        public List<string> time;
        public List<float> temperature_2m_min; 
    }
    
    #endregion
}
