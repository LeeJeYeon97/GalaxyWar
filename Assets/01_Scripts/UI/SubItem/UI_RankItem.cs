using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_RankItem : UI_Base
{
    enum Texts
    {
        Text_Rank,
        Text_Name,
        Text_Score
    }
    enum Images
    {
        Image_Rank,
    }

    public Sprite[] rankIcon;
    public Sprite myRankPanel;
    public Sprite normalPanel;
    public Sprite _1stPanel;

    public Image Panel;

    public override void Init()
    {
        base.Init();

        Bind<TMP_Text>(typeof(Texts));
        Bind<Image>(typeof(Images));
    }

    public void SetInfo(int rank, string playerName, int score, bool isMine = false)
    {
        if (!_init) Init();

        // 1. 기본 세팅 초기화
        GetTMP((int)Texts.Text_Rank).gameObject.SetActive(false);
        GetImage((int)Images.Image_Rank).gameObject.SetActive(true); // GetTMP -> GetImage로 수정 완료

        // 2. 내 정보인지 체크하여 패널과 랭킹 텍스트 색상 변경
        if (isMine)
        {
            Panel.sprite = myRankPanel;
            GetTMP((int)Texts.Text_Rank).color = Color.yellow; // 내 카드면 노란색!
        }
        else
        {
            Panel.sprite = normalPanel;
            GetTMP((int)Texts.Text_Rank).color = Color.white; // 일반 카드면 원래 색상
        }

        // 3. 기록이 없는 경우 (신규 유저)
        if (rank <= 0)
        {
            GetTMP((int)Texts.Text_Rank).gameObject.SetActive(true);
            GetImage((int)Images.Image_Rank).gameObject.SetActive(false);

            GetTMP((int)Texts.Text_Rank).text = "-";
            GetTMP((int)Texts.Text_Score).text = "-";
        }
        else
        {
            // 4. 점수 및 랭킹 세팅
            GetTMP((int)Texts.Text_Score).text = $"{score} Stage";

            if (rank == 1)
            {
                GetImage((int)Images.Image_Rank).sprite = rankIcon[0];
                Panel.sprite = _1stPanel; // 1등 전용 패널
            }
            else if (rank == 2)
            {
                GetImage((int)Images.Image_Rank).sprite = rankIcon[1];
            }
            else if (rank == 3)
            {
                GetImage((int)Images.Image_Rank).sprite = rankIcon[2];
            }
            else
            {
                GetTMP((int)Texts.Text_Rank).gameObject.SetActive(true);
                GetImage((int)Images.Image_Rank).gameObject.SetActive(false);
                GetTMP((int)Texts.Text_Rank).text = rank.ToString();
            }
        }

        // 닉네임 세팅
        GetTMP((int)Texts.Text_Name).text = string.IsNullOrEmpty(playerName) ? "Unknown" : playerName;
    }
}