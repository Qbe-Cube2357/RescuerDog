using System;
using UnityEngine;

[Serializable]
public sealed class PhaseSettings
{
    [Header("基本設定")]
    public string phaseName = "フェーズ1";

    [Min(1f)]
    public float timeLimit = 30f;

    [Header("おもちゃ")]
    [Min(1)]
    public int minToyCount = 4;

    [Min(1)]
    public int maxToyCount = 5;

    public bool refillToysWhenAllDelivered;

    [Header("宝")]
    [Min(0)]
    public int minTreasureCount;

    [Min(0)]
    public int maxTreasureCount;

    [Header("風船付き宝")]
    [Tooltip("お宝総数のうち、風船付きにする最小数")]
    [Min(0)]
    public int minBalloonTreasureCount;

    [Tooltip("お宝総数のうち、風船付きにする最大数")]
    [Min(0)]
    public int maxBalloonTreasureCount;

    public bool refillTreasuresWhenAllDelivered;

    [Header("カメラ")]
    public bool spawnCamera;

    [Header("飼い主")]
    [Min(1)]
    public int ownerCount = 1;

    [Header("鳥")]
    public bool birdAttackEnabled;
}