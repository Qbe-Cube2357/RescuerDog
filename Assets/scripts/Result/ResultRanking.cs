using UnityEngine;
using System.IO;
using System.Collections.Generic;
using TMPro;

public class ResultRanking : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI FirstText, SecondText, ThredText;
    int loadedFirst, loadedSecond, loadedThred;
    int first, second, thred;
    string filePath;

    void Start()
    {
        filePath = Application.dataPath + "/multi_scores.txt";

        if (File.Exists(filePath))
        {
            // 1. ファイルを行ごとに読み込んで配列にする
            // lines[0] には1行目、lines[1] には2行目...が入る
            string[] lines = File.ReadAllLines(filePath);

            // 2. それぞれの行（文字列）を数値（int型）に変換する
            loadedFirst = int.Parse(lines[0]);
            loadedSecond = int.Parse(lines[1]);
            loadedThred = int.Parse(lines[2]);

            // 3. 読み込んだ数値を画面やログで確認
            Debug.Log($"読み込み成功！ 1: {loadedFirst}, 2: {loadedSecond}, 3: {loadedThred}");
        }
        else
        {
            Debug.LogWarning("ファイルが見つかりません");
        }

        //並び替え
        if(loadedFirst < GameManager.currentScore)
        {
            first = GameManager.currentScore;
            second = loadedFirst;
            thred = loadedSecond;
        }
        else if (loadedSecond < GameManager.currentScore)
        {
            first = loadedFirst;
            second = GameManager.currentScore;
            thred = loadedSecond;
        }
        else if(loadedThred < GameManager.currentScore)
        {
            first = loadedFirst;
            second = loadedSecond;
            thred = GameManager.currentScore;
        }
        else
        {
            first = loadedFirst;
            second = loadedSecond;
            thred = loadedThred;
        }

        FirstText.text = $"{first}";
        SecondText.text = $"{second}";
        ThredText.text = $"{thred}";

        //書き込み
        filePath = Application.dataPath + "/multi_scores.txt";

        // 2. テキストに書き込むために、数値を文字列の配列（またはリスト）に変換する
        List<string> saveLines = new List<string>();
        saveLines.Add(first.ToString());
        saveLines.Add(second.ToString());
        saveLines.Add(thred.ToString());

        // 3. まとめて書き込み（各データが自動で改行されて保存されます）
        File.WriteAllLines(filePath, saveLines);

        Debug.Log("複数データの保存完了: " + filePath);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            first = 0;
            second = 0;
            thred = 0;

            //書き込み
            filePath = Application.dataPath + "/multi_scores.txt";

            // 2. テキストに書き込むために、数値を文字列の配列（またはリスト）に変換する
            List<string> saveLines = new List<string>();
            saveLines.Add(first.ToString());
            saveLines.Add(second.ToString());
            saveLines.Add(thred.ToString());

            // 3. まとめて書き込み（各データが自動で改行されて保存されます）
            File.WriteAllLines(filePath, saveLines);

            if (File.Exists(filePath))
            {
                // 1. ファイルを行ごとに読み込んで配列にする
                // lines[0] には1行目、lines[1] には2行目...が入る
                string[] lines = File.ReadAllLines(filePath);

                // 2. それぞれの行（文字列）を数値（int型）に変換する
                loadedFirst = int.Parse(lines[0]);
                loadedSecond = int.Parse(lines[1]);
                loadedThred = int.Parse(lines[2]);

                // 3. 読み込んだ数値を画面やログで確認
                Debug.Log($"読み込み成功！ 1: {loadedFirst}, 2: {loadedSecond}, 3: {loadedThred}");

                FirstText.text = $"{first}";
                SecondText.text = $"{second}";
                ThredText.text = $"{thred}";
            }
        }
    }
}
