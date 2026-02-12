using Firebase.Extensions;
using UnityEngine;

public class FirebaseInit : MonoBehaviour
{
    [SerializeField]
    private GameObject _enableOnInit = null;
    
    private void Start()
    {
        Firebase.FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task => {
            Firebase.DependencyStatus dependencyStatus = task.Result;
            if (dependencyStatus != Firebase.DependencyStatus.Available)
            {
                Debug.LogError("Unavailable");
            }
            else
            {
                _enableOnInit.gameObject.SetActive(true);
            }
        });
    }
}
