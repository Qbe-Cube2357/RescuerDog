using System;
using System.Collections.Generic;
using UnityEngine;

public enum TutorialStepType
{
    Dialogue,
    Movement,
    Collect,
    Delivery,
    Showcase,
    BirdEvasion,
    LoadScene
}

public enum TutorialMovementType
{
    Forward,
    Backward,
    SpinLeft,
    SpinRight
}

public enum TutorialCharacterSide
{
    Left,
    Right
}

[Serializable]
public sealed class TutorialDialoguePage
{
    [TextArea(3, 8)]
    public string text;

    public Sprite characterSprite;
    public Sprite bubbleSprite;

    public bool showCharacter = true;
    public TutorialCharacterSide characterSide;
}

[Serializable]
public sealed class TutorialStepData
{
    [Header("基本")]
    public string stepName;
    public TutorialStepType stepType;

    [Header("会話・紹介ページ")]
    public List<TutorialDialoguePage> pages =
        new List<TutorialDialoguePage>();

    [Header("練習UI")]
    public string practiceTitle;

    [TextArea(3, 8)]
    public string practiceInstruction;

    [Header("練習時の表示画像")]
    [Tooltip("ONの場合、この手順専用の画像設定を使用する")]
    public bool overridePracticeVisual;

    public Sprite practiceBubbleSprite;
    public Sprite practiceCharacterSprite;

    public bool showPracticeCharacter = true;

    public TutorialCharacterSide practiceCharacterSide =
        TutorialCharacterSide.Left;

    [Header("移動練習")]
    public TutorialMovementType movementType;

    [Min(0.1f)]
    public float requiredDistance = 2f;

    [Min(1f)]
    public float requiredAngle = 90f;

    [Range(0.01f, 1f)]
    public float inputThreshold = 0.35f;

    [Range(0f, 0.99f)]
    public float neutralThreshold = 0.15f;

    [Min(0f)]
    public float minimumInputHoldTime = 0.15f;

    [Header("回収練習")]
    public GameObject[] collectiblePrefabs;
    public Transform[] collectibleSpawnPoints;

    [Min(1)]
    public int requiredCollectCount = 2;

    [Header("納品練習")]
    [Min(1)]
    public int requiredDeliveryCount = 2;

    [Header("紹介")]
    public TutorialShowcaseRig showcasePrefab;
    public Transform showcaseSpawnPoint;

    [Header("鳥回避")]
    [Min(1)]
    public int requiredEvasionSuccesses = 3;

    [Min(0f)]
    public float birdLockOnHeight = 2f;

    [Header("シーン遷移")]
    public string destinationSceneName = "MenuScene";
}