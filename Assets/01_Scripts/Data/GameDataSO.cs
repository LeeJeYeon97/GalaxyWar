using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct PhaseInfo
{
    public Define.PhaseType phaseType; // 어떤 페이즈인지
    public float startTime;           // 도달 시간 (초)
    public float meteorSpawnInterval;   // 메테오 스폰 주기
}

[CreateAssetMenu(fileName = "GameData", menuName = "ScriptableObjects/GameData")]
public class GameDataSO : ScriptableObject
{

    [Tooltip("게임 시작 전 카운트 다운 시간")]
    public int GameStartTime;

    [Header("Object Spawn Settings")]
    public float itemSpawnInterval; // 생성 간격

    public int reviveCount;

    [Header("GameTestMode")]
    public bool expZero;
    public bool playerGod;

    [Header("Phase Settings")]
    [Tooltip("시간이 낮은 순서대로 넣어주세요 (Phase1: 0초, Phase2: 90초...)")]
    public List<PhaseInfo> phases = new List<PhaseInfo>();
}