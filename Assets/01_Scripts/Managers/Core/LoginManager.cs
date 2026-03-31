using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Authentication.PlayerAccounts;
using Unity.Services.Core;
using UnityEngine;

#if UNITY_IOS
using Unity.Advertisment.IosSupport;
#endif

#if UNITY_ANDROID
using GooglePlayGames;
using GooglePlayGames.BasicApi;
using System;
#endif
public class LoginManager
{
    private string _GooglePlayGamesToken;

    // 누군가 이 Action을 구독해두면, 로그인이 끝났을 때 알려줍니다!
    public Action OnLoginSuccess;
    public bool IsLoginFinished { get; private set; } = false;

    // 유니티 서비스 초기화
    public async void Init()
    {
        // 1. 구글 플레이 게임즈 초기화 (안드로이드)
#if UNITY_ANDROID
        PlayGamesPlatform.DebugLogEnabled = true;
        PlayGamesPlatform.Activate();
        LoginGooglePlayGames();
#endif

        // 2. 유니티 서비스 초기화
        if (UnityServices.State == ServicesInitializationState.Uninitialized)
        {
            Debug.Log("Services Initializing...");
            await UnityServices.InitializeAsync();
            Debug.Log("Services Initialized Successfully!");
        }

        PlayerAccountService.Instance.SignedIn += SignInOrLinkWithUnity;

        // 3. 익명 로그인 캐시 확인
        if (AuthenticationService.Instance.SessionTokenExists == false)
        {
            Debug.Log("Session Token not Found. Waiting for user input...");
            return;
        }

        Debug.Log("Returning player signing in...");
        await SignInAnonymouslyAsync();
    }

    #region 익명 로그인
    public async void StartAnonymousSignIn()
    {
        await SignInAnonymouslyAsync();
    }
    // 익명 로그인
    private async Task SignInAnonymouslyAsync()
    {
        try
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            Debug.Log("Sign in anonymously succeeded!");

            // Shows how to get the playerID
            Debug.Log($"PlayerID: {AuthenticationService.Instance.PlayerId}");
            IsLoginFinished = true;
            OnLoginSuccess?.Invoke();
        }
        catch (AuthenticationException ex)
        {
            // Compare error code to AuthenticationErrorCodes
            // Notify the player with the proper error message
            Debug.LogException(ex);
        }
        catch (RequestFailedException ex)
        {
            // Compare error code to CommonErrorCodes
            // Notify the player with the proper error message
            Debug.LogException(ex);
        }
    }

    #endregion

    #region Unity Player Account
    public async void StartUnitySignInAsync()
    {
        if (PlayerAccountService.Instance.IsSignedIn)
        {
            SignInOrLinkWithUnity();
            return;
        }

        try
        {
            await PlayerAccountService.Instance.StartSignInAsync();
        }
        catch (RequestFailedException ex)
        {
            Debug.LogException(ex);

        }
    }

    private async void SignInOrLinkWithUnity()
    {
        try
        {
            // 1. 플레이어가 인증되지 않았기 때문에 유니티에 가입
            if(AuthenticationService.Instance.IsSignedIn == false)
            {
                Debug.Log("Signing up with Unity Player Account...");
                await AuthenticationService.Instance.SignInWithUnityAsync(PlayerAccountService.Instance.AccessToken);
                Debug.Log("Successfully signed up with Unity Player Account");
                return;
            }
            // 2. 플레이어가 인증되었지만 링크되어 있지않을때
            if(HasUnityID() == false)
            {
                // 유니티 아이디가 없을 때 연결
                Debug.Log("Linking anonymous account to Unity...");
                await LinkWithUnityAsync(PlayerAccountService.Instance.AccessToken);
                Debug.Log("Successfully linked anonymous account!");
                return;
            }

            // 3. 플레이어가 로그인 되었고 유니티 계정에 연결 되었을 때
            Debug.Log("Player is already signed in to their Unity Player Account");
        }
        catch (RequestFailedException ex)
        {
            Debug.LogException(ex);
        }
    }

    // 유니티 아이디 확인함수
    private bool HasUnityID()
    {
        return AuthenticationService.Instance.PlayerInfo.GetUnityId() != null;
    }
    // 유니티 계정 연결
    async Task LinkWithUnityAsync(string accessToken)
    {
        try
        {
            await AuthenticationService.Instance.LinkWithUnityAsync(accessToken);
            Debug.Log("Link is successful.");
        }
        catch (AuthenticationException ex) when (ex.ErrorCode == AuthenticationErrorCodes.AccountAlreadyLinked)
        {
            // Prompt the player with an error message.
            Debug.LogError("This user is already linked with another account. Log in instead.");
        }

        catch (AuthenticationException ex)
        {
            // Compare error code to AuthenticationErrorCodes
            // Notify the player with the proper error message
            Debug.LogException(ex);
        }
        catch (RequestFailedException ex)
        {
            // Compare error code to CommonErrorCodes
            // Notify the player with the proper error message
            Debug.LogException(ex);
        }
    }

    #endregion

    #region 구글 플레이 게임 서비스

    public void LoginGooglePlayGames()
    {
        PlayGamesPlatform.Instance.Authenticate((status) =>
        {
            if (status == SignInStatus.Success)
            {
                Debug.Log("Login with Google Play games successful.");

                PlayGamesPlatform.Instance.RequestServerSideAccess(true, code =>
                {
                    Debug.Log("Authorization code: " + code);
                    _GooglePlayGamesToken = code;
                    // This token serves as an example to be used for SignInWithGooglePlayGames
                });
            }
            else
            {
                Debug.Log($"Google Play Games Login Unsuccessful status : {status}");
            }
        });
    }
    public void StartSignInWithGooglePlayGames()
    {
        //if(PlayGamesPlatform.Instance.IsAuthenticated() == false)
        //{
        //    Debug.LogWarning("Not yet authenticated with Google Play Games -- attempting login again");
        //    LoginGooglePlayGames();
        //    return;
        //}
        //SignInOrLinkWithGooglePlayGames();

        // 1. 이미 구글에 로그인이 되어 있다면 바로 UGS 연동으로 넘어갑니다.
        if (PlayGamesPlatform.Instance.IsAuthenticated() == true)
        {
            SignInOrLinkWithGooglePlayGames();
            return;
        }

        // 2. 로그인이 안 되어 있다면 무조건 강제 팝업(수동 로그인)을 띄웁니다!
        Debug.LogWarning("구글 로그인 팝업을 강제로 띄웁니다!");

        PlayGamesPlatform.Instance.ManuallyAuthenticate((status) =>
        {
            if (status == SignInStatus.Success)
            {
                Debug.Log("수동 로그인 성공! 팝업에서 권한을 허락받았습니다.");

                // 성공했으니 유니티 서버(UGS)에 넘길 암호(Token)를 달라고 요청합니다.
                PlayGamesPlatform.Instance.RequestServerSideAccess(true, code =>
                {
                    Debug.Log("Authorization code: " + code);
                    _GooglePlayGamesToken = code;

                    // 토큰을 받았으니 이제 UGS 익명 계정과 구글 계정을 하나로 합칩니다(Link)!
                    SignInOrLinkWithGooglePlayGames();
                });
            }
            else
            {
                Debug.Log($"수동 로그인 실패 또는 유저가 팝업을 닫음: {status}");
            }
        });
    }
    private async void SignInOrLinkWithGooglePlayGames()
    {
        if(string.IsNullOrEmpty(_GooglePlayGamesToken))
        {
            Debug.LogWarning("Authorization code is null or empty!");
            return;
        }
        if(AuthenticationService.Instance.IsSignedIn == false)
        {
            await SignInWithGooglePlayGamesAsync(_GooglePlayGamesToken);
        }
        else
        {
            await LinkWithGooglePlayGamesAsync(_GooglePlayGamesToken);
        }
    }
    /// <summary>기존 플레이어 로그인 또는 신규 플레이어 생성
    /// 1. Google Play 게임즈 자격 증명을 사용해 새 Unity Authentication 플레이어 생성
    /// 2. Google Play 게임즈 자격 증명을 사용해 기존 플레이어 로그인  
    /// 프로젝트에서 자격 증명과 연결된 Unity Authentication 플레이어가 존재하지 않는 경우, 
    /// SignInWithGooglePlayGamesAsync가 새 플레이어를 생성합니다. 
    /// 프로젝트에서 자격 증명과 연결된 Unity Authentication 플레이어가 존재하는 경우, 
    /// SignInWithGooglePlayGamesAsync가 해당 플레이어의 계정으로 로그인합니다. 
    /// 이 기능은 캐시된 플레이어를 고려하지 않으며, SignInWithGooglePlayGamesAsync가 캐시된 플레이어를 대체합니다.
    /// </summary>
    /// <param name="authCode"></param>
    /// <returns></returns>
    async Task SignInWithGooglePlayGamesAsync(string authCode)
    {
        try
        {
            await AuthenticationService.Instance.SignInWithGooglePlayGamesAsync(authCode);
            Debug.Log("SignIn is successful.");
            IsLoginFinished = true;
            OnLoginSuccess?.Invoke();
        }
        catch (AuthenticationException ex)
        {
            // Compare error code to AuthenticationErrorCodes
            // Notify the player with the proper error message
            Debug.LogException(ex);
        }
        catch (RequestFailedException ex)
        {
            // Compare error code to CommonErrorCodes
            // Notify the player with the proper error message
            Debug.LogException(ex);
        }
    }

    /// <summary> 플레이어를 익명 로그인에서 Google Play 게임즈 계정 로그인으로 업데이트
    /// 익명 인증을 설정한 후, 
    /// 플레이어가 Google Play 게임즈 계정을 생성하고 Google Play 게임즈를 통해 로그인하도록 업그레이드하려는 경우, 
    /// 게임이 플레이어에게 Google Play 게임즈 로그인 창을 표시하고 Google에서 일회성 인증 코드를 가져와야 합니다. 
    /// 그런 다음 LinkWithGooglePlayGamesAsync API를 호출해 플레이어를 연결합니다.
    /// SDK에 캐시된 플레이어가 존재하는 경우, 캐시된 플레이어를 Google Play 게임즈 계정에 연결할 수 있습니다.
    /// 
    /// 1. SignInAnonymouslyAsync를 사용해 캐시된 플레이어의 계정에 로그인합니다.
    /// 2. LinkWithGooglePlayGamesAsync를 사용해 캐시된 플레이어의 계정을 Google Play 게임즈 계정에 연결합니다.
    /// </summary>
    /// <param name="authCode"></param>
    /// <returns></returns>

    async Task LinkWithGooglePlayGamesAsync(string authCode)
    {
        try
        {
            await AuthenticationService.Instance.LinkWithGooglePlayGamesAsync(authCode);
            Debug.Log("Link is successful.");
            IsLoginFinished = true;
            OnLoginSuccess?.Invoke();
        }
        catch (AuthenticationException ex) when (ex.ErrorCode == AuthenticationErrorCodes.AccountAlreadyLinked)
        {
            // Prompt the player with an error message.
            Debug.LogError("This user is already linked with another account. Log in instead.");
        }

        catch (AuthenticationException ex)
        {
            // Compare error code to AuthenticationErrorCodes
            // Notify the player with the proper error message
            Debug.LogException(ex);
        }
        catch (RequestFailedException ex)
        {
            // Compare error code to CommonErrorCodes
            // Notify the player with the proper error message
            Debug.LogException(ex);
        }
    }

    /// <summary> Google Play 계정 연결 해제
    ///  플레이어가 Google Play 게임즈 계정 연결을 해제할 수 있도록 UnlinkGooglePlayGamesAsync API를 사용합니다. 
    ///  연결이 해제되면 계정이 다른 ID에 연결되지 않은 경우 익명 계정으로 전환됩니다.
    /// </summary>
    /// <param name="idToken"></param>
    /// <returns></returns>
    //async Task UnlinkGooglePlayGamesAsync(string idToken)
    //{
    //    try
    //    {
    //        await AuthenticationService.Instance.UnlinkGooglePlayGamesAsync(idToken);
    //        Debug.Log("Unlink is successful.");
    //    }
    //    catch (AuthenticationException ex)
    //    {
    //        // Compare error code to AuthenticationErrorCodes
    //        // Notify the player with the proper error message
    //        Debug.LogException(ex);
    //    }
    //    catch (RequestFailedException ex)
    //    {
    //        // Compare error code to CommonErrorCodes
    //        // Notify the player with the proper error message
    //        Debug.LogException(ex);
    //    }
    //}
    #endregion
}
