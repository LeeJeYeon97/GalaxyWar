using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_StageCard : UI_Base
{
    enum Texts
    {
        Text_Clear,
        Text_Lock,
        Text_Title,
    }
    enum Images
    {
        Image_level,
        Image_bg,
        Image_Lock,
        Image_Clear
    }

    public override void Init()
    {
        if (_init)
        {
            return;
        }

        base.Init();
        Bind<Image>(typeof(Images));
        Bind<TMP_Text>(typeof(Texts));
    }
    public void SetCard(int stageNum)
    {
        if(_init == false)
        {
            Init();
        }

        GetTMP((int)Texts.Text_Title).text = $"STAGE {stageNum:D2}";

        // 이미 클리어한 스테이지면
        if (stageNum <= Managers.Stage.clearStageLevel)
        {
            // 클리어 세팅 하기
            GetImage((int)Images.Image_Clear).gameObject.SetActive(true);
            GetImage((int)Images.Image_Lock).gameObject.SetActive(false);
            GetTMP((int)Texts.Text_Clear).gameObject.SetActive(true);

            GetImage((int)Images.Image_bg).gameObject.SetActive(true);
            GetTMP((int)Texts.Text_Lock).gameObject.SetActive(false);
        }
        // 지금 클리어해야하는 스테이지 다음 잠겨있는 스테이지면
        else if (stageNum >= Managers.Stage.clearStageLevel + 2)
        {
            // 잠금 아이콘 켜기
            GetImage((int)Images.Image_Clear).gameObject.SetActive(false);
            GetTMP((int)Texts.Text_Clear).gameObject.SetActive(false);

            GetImage((int)Images.Image_Lock).gameObject.SetActive(true);
            GetTMP((int)Texts.Text_Lock).gameObject.SetActive(true);

            GetImage((int)Images.Image_bg).gameObject.SetActive(true);
        }
        else
        {
            // 지금 클리어해야하는 스테이지면
            GetImage((int)Images.Image_Clear).gameObject.SetActive(false);
            GetTMP((int)Texts.Text_Clear).gameObject.SetActive(false);

            GetImage((int)Images.Image_Lock).gameObject.SetActive(false);
            GetTMP((int)Texts.Text_Lock).gameObject.SetActive(false);

            GetImage((int)Images.Image_bg).gameObject.SetActive(false);
        }
    }
}
