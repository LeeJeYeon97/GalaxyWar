using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_AbilityListItem : UI_Base
{
    enum Images { Image_AbilityIcon }
    enum Texts { Text_AbilityLevel }

    public override void Init()
    {
        if (_init) return;

        Bind<Image>(typeof(Images));
        Bind<TMP_Text>(typeof(Texts));

        base.Init();
    }

    // 팝업에서 이 스킬 아이콘을 생성할 때 데이터를 넣어주는 함수
    public void SetInfo(Sprite iconSprite, int level)
    {
        Init(); // 방어막 찌르기!

        GetImage((int)Images.Image_AbilityIcon).sprite = iconSprite;
        GetTMP((int)Texts.Text_AbilityLevel).text = $"Lv.{level}";
    }
}
