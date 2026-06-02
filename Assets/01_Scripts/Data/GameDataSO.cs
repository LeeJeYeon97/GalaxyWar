using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "GameData", menuName = "ScriptableObjects/GameData")]
public class GameDataSO : ScriptableObject
{

    [Tooltip("게임 시작 전 카운트 다운 시간")]
    public int GameStartTime;

    [Header("Object Spawn Settings")]
    public float itemSpawnInterval; // 생성 간격

    public int reviveCount;

    [Header("다시뽑기 골드값")]
    public int rerollGoldCost;

    [Header("GameTestMode")]
    public bool expZero;
    public bool playerGod;
    public bool noAttack;

    [Header("게임 종료시간")]
    public float gameclearTime;

    [Header("카메라 사이즈")]
    public float gamePlayeSize;
    public float burstModeSize;
    public float bossStageSize;
}