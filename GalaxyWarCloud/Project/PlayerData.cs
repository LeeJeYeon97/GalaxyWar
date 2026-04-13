using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Project;

public class PlayerData
{
    [JsonProperty("displayName")]
    public string? DisplayName { get; set; }

    [JsonProperty("experience")]
    public int Experience { get; set; }

    [JsonProperty("maxSurviveTime")]
    public int MaxSurviveTime { get; set; }

    [JsonProperty("maxScore")]
    public int MaxScore { get; set; }
}
public class PlayerEconomyData
{
    [JsonProperty("currencies")]
    public Dictionary<string, int> Currencies { get; set; } = new Dictionary<string, int>();

    [JsonProperty("itemInventory")]
    public Dictionary<string, int> ItemInventory { get; set; } = new Dictionary<string, int>();

    //  3. [새로 추가!] 장비 아이템 상세 목록 (고유 일련번호와 세부 데이터를 모두 포함)
    [JsonProperty("equipmentlist")]
    public List<EquipmentItemData> EquipmentList { get; set; } = new List<EquipmentItemData>();
}

public class PlayerDataResponse
{
    [JsonProperty("playerData")]
    public PlayerData PlayerData { get; set; } = new PlayerData();

    [JsonProperty("playerEconomyData")]
    public PlayerEconomyData PlayerEconomyData { get; set; } = new PlayerEconomyData();

    [JsonProperty("isNewPlayer")]
    public bool IsNewPlayer { get; set; }
}

public class EquipmentItemData
{
    [JsonProperty("instanceid")]
    public string? InstanceId { get; set; } // 고유 ID (예: "A1B2C3...")
    [JsonProperty("itemkey")]
    public string? ItemKey { get; set; }    // 종류 (예: "TEST_EQUIP")
    [JsonProperty("level")]
    public int Level { get; set; }         // 강화 수치
    [JsonProperty("amount")]
    public int Amount { get; set; }        // 수량 (장비는 보통 1)

    // 필요하다면 다른 데이터도 추가 가능 (내구도 등)
}
