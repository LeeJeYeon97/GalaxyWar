using System;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Leaderboards;
using UnityEngine;
using UnityEngine.UIElements;

class LeaderboardsDeployment : MonoBehaviour
{
    [SerializeField]
    UIDocument m_UiDocument;

    Button m_ButtonAdd;
    Button m_ButtonLog;
    IntegerField m_InputScore;

    async void Start()
    {
        InitializeUi();

        ToggleButtons(false);

        await InitializeServices();
        await SignInAnonymously();

        ToggleButtons(true);
    }

    void InitializeUi()
    {
        m_ButtonAdd = m_UiDocument.rootVisualElement.Q<Button>("ButtonAdd");
        m_ButtonLog = m_UiDocument.rootVisualElement.Q<Button>("ButtonLog");
        m_InputScore = m_UiDocument.rootVisualElement.Q<IntegerField>("IntField");

        m_ButtonAdd.clicked += async() => await AddScore_Async();
        m_ButtonLog.clicked += async() => await LogScore_Async();
    }

    static async Task InitializeServices()
    {
        if (UnityServices.State == ServicesInitializationState.Uninitialized)
        {
            await UnityServices.InitializeAsync();
        }
    }

    static async Task SignInAnonymously()
    {
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
    }

    async Task AddScore_Async()
    {
        ToggleButtons(false);

        try
        {
            var score = double.Parse(m_InputScore.text);
            var result = await LeaderboardsService.Instance.AddPlayerScoreAsync("Sample_Leaderboard", score);

            if (Math.Abs(score - result.Score) > double.Epsilon)
            {
                Debug.Log($"Attempted to add score {score}, but the current score of {result.Score} is better.");
            }
            else
            {
                Debug.Log($"Added score {result.Score} to the leaderboard.");
            }
        }
        finally
        {
            ToggleButtons(true);
        }
    }

    async Task LogScore_Async()
    {
        ToggleButtons(false);

        try
        {
            var result = await LeaderboardsService.Instance.GetPlayerScoreAsync("Sample_Leaderboard");
            Debug.Log($"Score: {result.Score}");
        }
        finally
        {
            ToggleButtons(true);
        }
    }

    void ToggleButtons(bool toggle)
    {
        m_ButtonAdd.SetEnabled(toggle);
        m_ButtonLog.SetEnabled(toggle);
    }
}
