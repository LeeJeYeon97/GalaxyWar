using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UI_UpgradePanel : UI_Base
{
    //  4개의 Content를 각각 바인딩할 수 있게 Enum을 수정합니다.
    enum GameObjects
    {
        Content_Hp,
        Content_Damage,
        //Content_Speed,
        //Content_Defense
    }

    private List<UpgradeDataSO> upgradeDataList = new List<UpgradeDataSO>();
    public GameObject nodePrefab;

    public override void Init()
    {
        base.Init();
        Bind<GameObject>(typeof(GameObjects));

        SortUpgradeData();
        RefreshAllNodes();
    }

    private void SortUpgradeData()
    {
        if (Managers.Data.UpgradeDataDict == null || Managers.Data.UpgradeDataDict.Count == 0) return;
        upgradeDataList = new List<UpgradeDataSO>(Managers.Data.UpgradeDataDict.Values);
    }
    public void UpdateNodesUI()
    {
        // 패널 안에 생성되어 있는 모든 노드들을 찾아서 UI만 싹 새로고침 합니다.
        UI_UpgradeNode[] nodes = GetComponentsInChildren<UI_UpgradeNode>();
        foreach (var node in nodes)
        {
            node.RefreshUI();
        }
    }
    public void RefreshAllNodes()
    {
        // 1. 4개의 Content 트랜스폼을 모두 가져옵니다.
        Transform contentHp = GetObject((int)GameObjects.Content_Hp).transform;
        Transform contentDmg = GetObject((int)GameObjects.Content_Damage).transform;
        //Transform contentSpd = GetObject((int)GameObjects.Content_Speed).transform;
        //Transform contentDef = GetObject((int)GameObjects.Content_Defense).transform;

        // 2. 모든 Content의 기존 자식들을 싹 지워줍니다.
        Transform[] allContents = { contentHp, contentDmg };
        foreach (var content in allContents)
        {
            foreach (Transform child in content)
                Managers.Resource.Destroy(child.gameObject);
        }

        // 3. 데이터를 순회하며 각자의 Content에 생성합니다.
        foreach (var data in upgradeDataList)
        {
            //  Enum 값에 따라 어느 스크롤 뷰(Content)에 들어갈지 타겟을 정합니다.
            Transform targetContent = null;
            switch (data.type)
            {
                case Define.UpgradeType.HP: targetContent = contentHp; break;
                case Define.UpgradeType.Damage: targetContent = contentDmg; break;
                // case Define.UpgradeType.Speed: targetContent = contentSpd; break;
                // case Define.UpgradeType.Defense: targetContent = contentDef; break;
            }

            UI_UpgradeNode prevNode = null;

            // 해당 능력치의 최대 레벨만큼 타겟 Content 안에 세로로 쌓습니다.
            for (int lv = 0; lv < data.MaxLevel; lv++)
            {
                GameObject go = Managers.Resource.Instantiate(nodePrefab, targetContent);
                UI_UpgradeNode node = go.GetComponent<UI_UpgradeNode>();

                int targetLevel = lv + 1;
                node.SetInfo(data, targetLevel, prevNode);

                prevNode = node;
            }

            //  [핵심] 노드 생성이 끝나면, 유니티에게 "지금 당장 UI 크기 계산해!"라고 명령합니다.
            Canvas.ForceUpdateCanvases();

            ScrollRect[] scrollRects = GetComponentsInChildren<ScrollRect>();
            foreach (var scroll in scrollRects)
            {
                // 이제 Content의 크기가 완벽히 정렬된 상태이므로 칼같이 바닥으로 내려갑니다.
                scroll.verticalNormalizedPosition = 0f;
            }
        }
    }
}