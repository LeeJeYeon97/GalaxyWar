using System.Threading.Tasks;
using TMPro;
using Unity.Services.Authentication;
using Unity.Services.Leaderboards.Models;
using UnityEngine;

public class UI_RankPanel : UI_Base
{
    enum GameObjects
    {
        Content,        // 스크롤뷰 안에 아이템들이 생성될 부모 Transform
        MyRankContainer // [추가] 하단에 내 카드를 고정시킬 전용 빈 게임오브젝트
    }

    enum Texts
    {
        Text_Loading // 데이터를 불러오는 동안 보여줄 텍스트 (옵션)
    }

    // 스크롤뷰 안에 생성할 랭킹 아이템 프리팹 (인스펙터에서 직접 할당하거나 ResourceMgr 이용)
    public GameObject RankItemPrefab;

    public override void Init()
    {
        base.Init();

        Bind<GameObject>(typeof(GameObjects));
        Bind<TMP_Text>(typeof(Texts));

        // 창이 켜질 때 자동으로 랭킹 데이터를 갱신합니다.
        RefreshUI();
    }

    private async void RefreshUI()
    {
        GetTMP((int)Texts.Text_Loading).gameObject.SetActive(true);

        Transform content = GetObject((int)GameObjects.Content).transform;
        Transform myRankContainer = GetObject((int)GameObjects.MyRankContainer).transform;

        // 기존에 생성되어 있던 리스트 삭제 (초기화)
        foreach (Transform child in content) 
            Managers.Resource.Destroy(child.gameObject);

        foreach (Transform child in myRankContainer)
            Managers.Resource.Destroy(child.gameObject);

        // 서버에서 데이터 긁어오기
        var topScores = await Managers.Leaderboard.GetTopScoresAsync(50);
        var myScore = await Managers.Leaderboard.GetMyScoreAsync();

        GetTMP((int)Texts.Text_Loading).gameObject.SetActive(false);

        // ==========================================
        // 1. 하단 고정 '내 랭킹 카드' 세팅
        // ==========================================
        GameObject myCard = Managers.Resource.Instantiate(RankItemPrefab, myRankContainer);

        //  [수정] 앵커를 Stretch / Stretch 로 세팅하여 부모 컨테이너에 꽉 채우기
        RectTransform myCardRect = myCard.GetComponent<RectTransform>();
        if (myCardRect != null)
        {
            myCardRect.anchorMin = new Vector2(0, 0); // 좌측 하단
            myCardRect.anchorMax = new Vector2(1, 1); // 우측 상단

            // Left, Bottom, Right, Top 여백을 모두 0으로 딱 붙임
            myCardRect.offsetMin = Vector2.zero;
            myCardRect.offsetMax = Vector2.zero;

            // 간혹 Instantiate 시 스케일이 틀어지는 것을 방지
            myCardRect.localScale = Vector3.one;
        }

        UI_RankItem myItem = myCard.GetComponent<UI_RankItem>();

        if (myScore != null)
        {
            // UGS 랭킹은 0등부터 시작하므로 보여줄 때는 +1을 해줍니다!
            myItem.SetInfo(myScore.Rank + 1, myScore.PlayerName, (int)myScore.Score, true);
        }
        else
        {
            // 기록이 아예 없는 유저일 경우 (-1을 넘겨서 '-' 로 표시되게 합니다)
            string myName = AuthenticationService.Instance.PlayerName;
            if (string.IsNullOrEmpty(myName)) myName = "나"; // 이름이 세팅되기 전 방어 로직

            myItem.SetInfo(-1, myName, -1, true);
        }

        // ==========================================
        // 2. 전체 랭킹 스크롤뷰 세팅
        // ==========================================
        if (topScores != null && topScores.Results != null)
        {
            foreach (var entry in topScores.Results)
            {
                GameObject go = Managers.Resource.Instantiate(RankItemPrefab, content);
                UI_RankItem rankItem = go.GetComponent<UI_RankItem>();

                if (rankItem != null)
                {
                    // 전체 랭킹 리스트 안에서도 '내 닉네임(ID)'이 있다면 노란색으로 빛나게 처리!
                    bool isMine = (entry.PlayerId == AuthenticationService.Instance.PlayerId);

                    rankItem.SetInfo(entry.Rank + 1, entry.PlayerName, (int)entry.Score, isMine);
                }
            }
        }
        else
        {
            Debug.LogWarning("리더보드에 아직 아무도 점수를 등록하지 않았습니다.");
        }
    }
}