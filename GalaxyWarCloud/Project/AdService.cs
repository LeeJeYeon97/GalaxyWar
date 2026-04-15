using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Services.CloudCode.Apis;
using Unity.Services.CloudCode.Core;


namespace Project;

public class ParsedTokenData
{
    // DateTime.Ticks - avoids JSON serialization inconsistencies across libraries
    public long Timestamp { get; init; }
    public string InstanceId { get; init; } = "";
    public string InstanceName { get; init; } = "";
    public string AdNetwork { get; init; } = "";
    public string PlacementName { get; init; } = "";
    public string RewardName { get; init; } = "";
    public int RewardAmount { get; init; }
}
public class AdService
{
    private const int k_MaxTokenAgeMinutes = 30;
    private const int k_MaxFutureToleranceSeconds = 60;
    private const int k_MaxAdRewardAmount = 1000;

    // private const string k_LastAdTokenKey = "LAST_AD_TOKEN";

    private const string k_RecentAdTokensKey = "RECENT_AD_TOKENS_LIST";
    private const int k_MaxStoredTokens = 5; // 최근 영수증 5개까지 기억

    private const int k_MinimumAdIntervalSeconds = 10; // Minimum time between ads

    private readonly string[] k_AllowedRewardCurrencies =
    {
        ServerDefine.k_GoldCurrencyKey,
    };

    private readonly Dictionary<string, int> k_MaxRewardLimits = new()
    {
        { ServerDefine.k_GoldCurrencyKey, 1000 },
        // { ServerDefine.k_DiamondCurrencyKey, 50 } // 다이아몬드는 최대 50개만 허용 등
    };


    private readonly ILogger<AdService> _logger;
    private readonly PlayerEconomyService _playerEconomyService;
    private readonly PlayerDataService _playerDataService;

    public AdService(ILogger<AdService> logger,PlayerEconomyService playerEconomyService,PlayerDataService playerDataService)
    {
        _logger = logger;
        _playerEconomyService = playerEconomyService;
        _playerDataService = playerDataService;
    }

    [CloudCodeFunction("HandleGrantVideoAdReward")]
    public async Task<PlayerEconomyData> HandleGrantVideoAdReward(IExecutionContext context, IGameApiClient gameApiClient, string adToken)
    {
        try
        {
            // Parse the token into structured data
            // 영수증(JSON)을 읽을 수 있게 뜯어봅니다.
            ParsedTokenData tokenData = ParseToken(adToken);

            //영수증의 양식(시간, 재화 종류, 수량)이 맞는지 1차로 검사합니다.
            // Validate the token data structure and values
            ValidateTokenData(tokenData);

            // 개선 1: DB에서 리스트를 여기서 한 번만 읽습니다.
            List<string> recentTokens = await GetRecentTokens(context, gameApiClient);

            //읽어온 리스트를 검증 함수에 넘겨줍니다.
            ValidateTokenUsageLocal(tokenData.PlacementName, adToken, recentTokens);

            // Grant reward

            // 1. 플레이스먼트 이름이 "Game_Over" 인지 확인
            if (tokenData.PlacementName == "Game_Over")
            {
                //  2. Economy(지갑)는 건드리지 않고, PlayerData(Cloud Save)에 부활 자격 저장
                // "IS_REVIVE_PENDING"이라는 키를 true로 만듭니다.
                await _playerDataService.SaveData(context, gameApiClient, "IS_REVIVE_PENDING", true);

                _logger.LogInformation("부활 자격 부여 완료 (Game_Over)");
            }
            else
            {
                // 상점 광고 등은 기존처럼 골드 지급
                await _playerEconomyService.AddCurrency(context, gameApiClient, tokenData.RewardName, tokenData.RewardAmount);
            }

            //// [수정된 코드] 토큰에 적혀있는 바로 그 재화(RewardName)를 지급합니다!
            ////  개선 3: PlacementName에 따른 추가 로직 (필요 시)
            //// if (tokenData.PlacementName == "Special_Event") { ... }
            ////모든 검사가 통과되면 진짜 돈(재화)을 유저 지갑에 꽂아줍니다.
            //await _playerEconomyService.AddCurrency(context, gameApiClient, tokenData.RewardName, tokenData.RewardAmount);

            //이 영수증은 이제 사용 완료됨!" 이라고 DB에 도장을 찍어 저장합니다.
            // Store the token to prevent reuse
            // 읽어온 리스트를 저장 함수에도 넘겨서 중복 호출을 막습니다.
            await StoreTokenDirect(context, gameApiClient, adToken, recentTokens);

            _logger.LogInformation($"Successfully granted ad reward: {tokenData.RewardName} x{tokenData.RewardAmount} from {tokenData.AdNetwork}");

            return await _playerEconomyService.GetPlayerEconomyData(context, gameApiClient)
                ?? throw new InvalidOperationException("Failed to get player economy data");
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error granting reward");
            throw;
        }
    }

    private ParsedTokenData ParseToken(string adToken)
    {
        if (string.IsNullOrEmpty(adToken))
        {
            throw new UnauthorizedAccessException("Token is null or empty");
        }

        try
        {
            var tokenData = JsonConvert.DeserializeObject<ParsedTokenData>(adToken);

            if (tokenData == null)
            {
                throw new UnauthorizedAccessException("Failed to parse token");
            }

            return tokenData;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse token: {Token}", adToken);
            throw new UnauthorizedAccessException("Invalid token format");
        }
    }
    private void ValidateTokenData(ParsedTokenData data)
    {
        // Convert ticks to DateTime
        DateTime timestamp;
        try
        {
            timestamp = new DateTime(data.Timestamp, DateTimeKind.Utc);
        }
        catch (ArgumentOutOfRangeException)
        {
            throw new UnauthorizedAccessException("Invalid timestamp in token");
        }

        // Validate timestamp age
        DateTime now = DateTime.UtcNow;
        TimeSpan age = now - timestamp;

        // 시간이 지난 토큰인지 확인
        if (age.TotalMinutes > k_MaxTokenAgeMinutes)
        {
            // Make exception methods less specific in production, "Token invalid"
            throw new UnauthorizedAccessException("Token is too old");
        }
        // 미래에서 온 토큰인지 확인
        if (timestamp > now.AddSeconds(k_MaxFutureToleranceSeconds))
        {
            throw new UnauthorizedAccessException("Token timestamp is too far in the future");
        }

        if (string.IsNullOrEmpty(data.InstanceId))
        {
            throw new UnauthorizedAccessException("Instance ID cannot be empty");
        }

        // 허락된 재화가 맞는가
        if (!k_AllowedRewardCurrencies.Contains(data.RewardName))
        {
            throw new UnauthorizedAccessException($"Invalid reward name. Got '{data.RewardName}'");
        }

        if (data.RewardAmount <= 0)
        {
            throw new UnauthorizedAccessException($"Invalid reward amount. Must be positive, got {data.RewardAmount}");
        }

        if (data.RewardAmount > k_MaxAdRewardAmount)
        {
            throw new UnauthorizedAccessException($"Invalid reward amount. Expected under {k_MaxAdRewardAmount}, got {data.RewardAmount}");
        }

        int limit = k_MaxRewardLimits.GetValueOrDefault(data.RewardName, 0);
        if (data.RewardAmount > limit)
        {
            throw new UnauthorizedAccessException($"Reward amount {data.RewardAmount} exceeds limit for {data.RewardName}");
        }
    }

    // 도움 함수: DB에서 리스트만 깔끔하게 가져오기
    private async Task<List<string>> GetRecentTokens(IExecutionContext context, IGameApiClient gameApiClient)
    {
        var (success, tokensObj) = await _playerDataService.TryGetData(context, gameApiClient, k_RecentAdTokensKey);
        if (success && tokensObj != null)
        {
            return JsonConvert.DeserializeObject<List<string>>(tokensObj.ToString()) ?? new List<string>();
        }
        return new List<string>();
    }

    private async Task ValidateTokenUsage(IExecutionContext context, IGameApiClient gameApiClient, string adToken)
    {
        try
        {
            // 1. DB에서 최근 사용한 영수증 리스트 5개를 가져옵니다.
            var (success, tokensObj) = await _playerDataService.TryGetData(context, gameApiClient, k_RecentAdTokensKey);
            List<string> recentTokens = new List<string>();

            if (success && tokensObj != null)
            {
                try
                {
                    recentTokens = JsonConvert.DeserializeObject<List<string>>(tokensObj.ToString()) ?? new List<string>();
                }
                catch { }
            }

            // 2. [핑퐁 완벽 차단!] 내가 낸 영수증이 최근 5개 기록 안에 포함되어 있는지 검사합니다.
            if (recentTokens.Contains(adToken))
            {
                throw new UnauthorizedAccessException("Ad token is a duplicate of a recently used token. Reward denied.");
            }

            // 3. 쿨타임 검사를 위해 리스트에서 '가장 최근에 본(0번째)' 영수증을 꺼내서 던져줍니다.
            string? latestToken = recentTokens.Count > 0 ? recentTokens[0] : null;

            // 3. [쿨타임 검사] 이전 영수증에 적힌 시간과 지금 낸 영수증의 시간을 비교합니다.
            if (!HasSufficientAdIntervalElapsed(latestToken!))
            {
                // 광고 쿨타임(10초)도 안 지났는데 또 광고를 다 봤다고 요청했다면 매크로이므로 에러를 냅니다.
                throw new UnauthorizedAccessException("Ad rewarded too quickly. Reward denied.");
            }
        }
        // 4. [예외 필터링] C#의 고급 기능인 'when' 키워드를 사용한 예외 처리입니다.
        // 해석: 발생한 에러(ex)가 'UnauthorizedAccessException'이 "아닐 때만" 이 catch 문을 실행해라!
        catch (Exception ex) when (!(ex is UnauthorizedAccessException))
        {
            // DB가 잠깐 다운되었거나, 알 수 없는 서버 에러가 났을 때 이곳으로 들어옵니다.
            // 서버 에러 로그를 남깁니다. (해커를 추적하거나 버그를 고치기 위함)
            _logger.LogError(ex, "Error validating token usage");

            // 서버 에러가 났다고 해서 보상을 그냥 줘버리면 안 되므로(Fail-Secure 원칙), 강제로 보안 에러를 던져 보상 지급을 막습니다.
            throw new UnauthorizedAccessException("Unable to validate token usage");
        }
    }

    // 개선 1 적용: 이미 읽어온 리스트를 받아서 검사 (비동기 아님, 속도 매우 빠름)
    private void ValidateTokenUsageLocal(string placementName, string adToken, List<string> recentTokens)
    {
        if (recentTokens.Contains(adToken))
        {
            throw new UnauthorizedAccessException("Duplicate token detected.");
        }

        string? latestToken = recentTokens.Count > 0 ? recentTokens[0] : null;
        if (!HasSufficientAdIntervalElapsed(latestToken!))
        {
            throw new UnauthorizedAccessException("Ad interval too short.");
        }
    }

    private bool HasSufficientAdIntervalElapsed(string previousToken)
    {
        // 1. [뉴비 확인] 이전 영수증이 아예 없다면? (게임을 처음 깔았거나, 처음 광고를 보는 경우)
        if (string.IsNullOrEmpty(previousToken))
        {
            // No Previous token exists - timing is valid
            // 비교할 과거가 없으니 당연히 시간 쿨타임도 없습니다. 즉시 무사 통과(true) 시켜줍니다.
            return true;
        }

        try
        {
            // 2. [영수증 해독] DB에서 가져온 이전 영수증(JSON 문자열)을 C# 객체로 변환합니다.
            ParsedTokenData previousTokenData = ParseToken(previousToken);

            // 3. [시간 복원] 영수증에 적혀있던 'Ticks(아주 긴 숫자)'를 다시 현실 세계의 시간(DateTime)으로 복원합니다.
            // 이때 DateTimeKind.Utc를 명시하여 전 세계 어디서든 오차가 발생하지 않도록 기준을 꽉 잡아줍니다. (매우 훌륭함!)
            DateTime previousTimestamp = new DateTime(previousTokenData.Timestamp, DateTimeKind.Utc);

            // 4. [경과 시간 계산] 현재 서버의 절대 시간(UtcNow)에서 이전 광고를 본 시간을 뺍니다.
            TimeSpan timeBetweenAds = DateTime.UtcNow - previousTimestamp;

            // 5. [쿨타임 검사] 두 광고 사이의 시간 간격(TotalSeconds)이 우리가 정한 최소 쿨타임(예: 10초)보다 짧다면?
            if (timeBetweenAds.TotalSeconds < k_MinimumAdIntervalSeconds)
            {
                // 해커가 스피드핵을 썼거나 매크로로 패킷을 연달아 보낸 상황입니다.
                // 경고 로그를 남기고, 보상 지급을 단호하게 거절(false)합니다!
                _logger.LogWarning("Ad rewarded too quickly. Time since last ad: {TimeBetweenAds} seconds",
                    timeBetweenAds.TotalSeconds);
                return false;
            }

            // 아무 문제가 없다면 무사 통과!
            return true;
        }
        catch (Exception ex)
        {
            // 6. [예외 처리: Fail-Open 전략] 
            // 만약 과거 영수증을 분석하다가 에러가 났다면? (예: 게임 업데이트로 JSON 양식이 바뀌어서 옛날 영수증을 못 읽음)
            _logger.LogError(ex, "Error parsing previous token for timing validation. Allowing reward");

            // 에러가 났지만 일단 보상을 줍니다! (true)
            return true; // Fail-open strategy
        }
    }

    /// <summary>
    /// Stores the ad token and timestamp for future validation of ad rewards.
    /// </summary>
    /// <param name="adToken">The ad token to store</param>
    private async Task StoreLastAdToken(IExecutionContext context, IGameApiClient gameApiClient, string adToken)
    {
        // 1. 기존에 저장된 '최근 영수증 리스트'를 불러옵니다.
        var (success, tokensObj) = await _playerDataService.TryGetData(context, gameApiClient, k_RecentAdTokensKey);
        List<string> recentTokens = new List<string>();

        if (success && tokensObj != null)
        {
            try
            {
                // DB에 있던 JSON 배열을 C# List로 변환합니다.
                recentTokens = JsonConvert.DeserializeObject<List<string>>(tokensObj.ToString()) ?? new List<string>();
            }
            catch { /* 파싱 에러 시 빈 리스트로 새로 시작 */ }
        }

        // 2. 방금 쓴 따끈따끈한 새 영수증을 리스트의 맨 앞(0번 인덱스)에 끼워 넣습니다.
        recentTokens.Insert(0, adToken);

        // 3. 리스트가 너무 길어지면 안 되므로, 최근 5개(k_MaxStoredTokens)만 남기고 뒤에 있는 낡은 영수증은 자릅니다.
        if (recentTokens.Count > k_MaxStoredTokens)
        {
            recentTokens = recentTokens.Take(k_MaxStoredTokens).ToList();
        }

        // 4. 다시 JSON 텍스트로 압축해서 DB에 덮어씌웁니다.
        string jsonToSave = JsonConvert.SerializeObject(recentTokens);
        await _playerDataService.SaveData(context, gameApiClient, k_RecentAdTokensKey, jsonToSave);
    }

    // 개선 1 적용: 리스트를 새로 읽지 않고 받은 리스트를 수정해서 바로 저장
    private async Task StoreTokenDirect(IExecutionContext context, IGameApiClient gameApiClient, string adToken, List<string> recentTokens)
    {
        recentTokens.Insert(0, adToken);
        var finalTokens = recentTokens.Take(k_MaxStoredTokens).ToList();
        await _playerDataService.SaveData(context, gameApiClient, k_RecentAdTokensKey, JsonConvert.SerializeObject(finalTokens));
    }
}

