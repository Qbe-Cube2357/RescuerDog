using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager_pre : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("シーン設定")]
    public string clearSceneName = "GAMECLEAR";
    public string overSceneName = "GAMEOVER";

    [Header("プレイヤー管理")]
    public GameObject mainPlayer;

    [Header("敵の管理")]
    public GameObject[] toyPrefab;
    public GameObject mobPrefab;
    public GameObject bossPrefab;
    public Transform[] spawnPoints;

    [Header("ステージ状態の管理")]
    private float gameTimer = 0f;
    private bool bossSpawned = false;
    private bool isGameEnding = false;
    private bool isMainPlayerDead = false;

    [Header("モブ生成制限")]
    public int initialMobCount = 10; // 最初に出現させる数

    private void Start()
    {
        // 開始時にモブをまとめて生成
        Spawntoy();
        // SpawnInitialMobs();
    }

    private void Update()
    {
        if (isGameEnding) return;

        gameTimer += Time.deltaTime;

        // 1分経過後にボス出現
        if (gameTimer >= 60f && !bossSpawned)
        {
            SpawnBoss();
        }

        // クリア判定: ボス出現済み かつ 時間が1分経ってる
        if (bossSpawned && gameTimer > 62f)
        {

        }
    }


    // --- 生成ロジック ---
    void SpawnInitialMobs()
    {
        if (spawnPoints.Length == 0) return;

        for (int i = 0; i < initialMobCount; i++)
        {
            SpawnMob();
        }
    }

    void Spawntoy()
    {
        for (int i = 0; i < toyPrefab.Length; i++)
        {
            // ランダムな地点から生成
            Transform sp = spawnPoints[Random.Range(0, spawnPoints.Length)];
            Instantiate(toyPrefab[i], sp.position, sp.rotation);
        }
    }

    void SpawnMob()
    {
        // ランダムな地点から生成
        Transform sp = spawnPoints[Random.Range(0, spawnPoints.Length)];
        Instantiate(mobPrefab, sp.position, sp.rotation);
    }

    void SpawnBoss()
    {
        bossSpawned = true;
        Debug.Log("BOSS APPEARED!");
        // ランダムな位置に
        Transform sp = spawnPoints[Random.Range(0, spawnPoints.Length)];
        // ボス（プレハブ）をスポーン
        Instantiate(bossPrefab, sp.position, sp.rotation);
    }

    void CheckGameOver(bool isMainPlayerDead)
    {
        // メイン死、またはプレイヤーリストが空（全員死亡）
        if (isMainPlayerDead)
        {
            EndGame(overSceneName);
        }
    }

    void EndGame(string sceneName)
    {
        if (isGameEnding) return;
        isGameEnding = true;
        SceneManager.LoadScene(sceneName);
    }


}