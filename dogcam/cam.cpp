/*
 旧: g++ -std=c++11 cam.cpp `pkg-config --cflags --libs opencv4`
 新: g++ -std=c++11 cam.cpp $(pkg-config --cflags --libs opencv4) -pthread -o cam.out
 */
#include <iostream>           //入出力関連ヘッダ
#include <opencv2/opencv.hpp> //OpenCV関連ヘッダ
#include <string>
#include <atomic>
#include <mutex>
#include <queue>
#include <thread>
#include <chrono>

// int col = 0;
// cv::Rect face;

// ---『標準入力専用スレッド』---
namespace
{
    std::atomic<bool> stopRequested(false);
    std::mutex commandMutex;
    std::queue<std::string> commandQueue;

    void ReadCommands()
    {
        std::string command;

        while (std::getline(std::cin, command))
        {
            if (command == "QUIT")
            {
                stopRequested.store(true);
                return;
            }

            std::lock_guard<std::mutex> lock(commandMutex);
            commandQueue.push(command);
        }

        // Unity終了などで標準入力が閉じられた場合
        stopRequested.store(true);
    }

    bool TryTakeCommand(std::string &command)
    {
        std::lock_guard<std::mutex> lock(commandMutex);

        if (commandQueue.empty())
        {
            return false;
        }

        command = commandQueue.front();
        commandQueue.pop();
        return true;
    }

    void SendProtocolMessage(const std::string &message)
    {
        std::cout << message << std::endl;
    }
}

// ---『撮影命令を処理する関数』---
void HandleCommand(
    const std::string &command,
    const cv::Mat &processedFrame)
{
    const std::string capturePrefix = "CAPTURE|";

    if (command.rfind(capturePrefix, 0) != 0)
    {
        std::cerr << "Unknown command: " << command << std::endl;
        SendProtocolMessage("ERROR|UNKNOWN_COMMAND");
        return;
    }

    const std::string outputPath =
        command.substr(capturePrefix.length());

    if (outputPath.empty() || processedFrame.empty())
    {
        SendProtocolMessage("ERROR|SAVE_FAILED");
        return;
    }

    try
    {
        bool saved = cv::imwrite(outputPath, processedFrame);

        if (!saved)
        {
            SendProtocolMessage("ERROR|SAVE_FAILED");
            return;
        }

        SendProtocolMessage("SAVED|" + outputPath);
    }
    catch (const cv::Exception &exception)
    {
        std::cerr
            << "OpenCV save error: "
            << exception.what()
            << std::endl;

        SendProtocolMessage("ERROR|SAVE_FAILED");
    }
}

// main関数
int main(int argc, char *argv[])
{
    // OpenCV初期設定処理
    // カメラキャプチャの初期化
    cv::VideoCapture capture = cv::VideoCapture(0);

    // if (capture.isOpened() == 0)
    // {
    //     // カメラが見つからないときはメッセージを表示して終了
    //     printf("Camera not found\n");
    //     exit(1);
    // }
    if (!capture.isOpened())
    {
        // カメラが見つからないときはメッセージを表示して終了
        SendProtocolMessage("ERROR|CAMERA_NOT_FOUND");
        return 1;
    }

    cv::Mat sourceImage1 = cv::imread("DogEar1.jpg", cv::IMREAD_COLOR);
    cv::Mat sourceImage2 = cv::imread("DogMouth.jpg", cv::IMREAD_COLOR);
    cv::Mat sourceImage3 = cv::imread("deco.jpg", cv::IMREAD_COLOR);
    if (sourceImage1.empty() ||
        sourceImage2.empty() ||
        sourceImage3.empty())
    {
        std::cerr << "Decoration image could not be loaded." << std::endl;
        SendProtocolMessage("ERROR|ASSET_NOT_FOUND");
        return 2;
    }

    cv::Mat pic;
    // int picNum = 0;

    // std::string saveDir = "保存フォルダ";

    if (sourceImage1.data == 0 && sourceImage2.data == 0 && sourceImage3.data == 0)
    { // 画像ファイルが読み込めなかった場合
        printf("File not found\n");
        exit(0);
    }

    cv::Mat originalImage, frameImage, hsvImage, tempImage1, tempImage2, tempImage3, grayImage;
    cv::Size imageSize(720, 405);         // 画像サイズ
    cv::CascadeClassifier faceClassifier; // 顔認識用分類器
    cv::CascadeClassifier eyeClassifier;  // 顔認識用分類器

    // 3チャンネル画像"hsvImage"と"tempImage"の確保（ビデオと同サイズ）
    hsvImage = cv::Mat(imageSize, CV_8UC3);
    tempImage1 = cv::Mat(imageSize, CV_8UC3);
    tempImage2 = cv::Mat(imageSize, CV_8UC3);
    tempImage3 = cv::Mat(imageSize, CV_8UC3);

    // OpenCVウィンドウ生成
    // cv::namedWindow("Frame");
    // cv::moveWindow("Frame", 0, 0);
    // cv::namedWindow("Face");
    // cv::moveWindow("Face", imageSize.width, 0);

    // ①正面顔検出器の読み込み
    // faceClassifier.load("haarcascades/haarcascade_frontalface_default.xml");
    // eyeClassifier.load("haarcascades/haarcascade_mcs_eyepair_small.xml");
    if (!faceClassifier.load(
            "haarcascades/haarcascade_frontalface_default.xml"))
    {
        std::cerr << "Face cascade could not be loaded." << std::endl;
        SendProtocolMessage("ERROR|CASCADE_NOT_FOUND");
        return 3;
    }

    // Unityから標準入力を受け取るスレッドを開始
    std::thread commandThread(ReadCommands);

    bool readySent = false;
    bool frameErrorReported = false;

    while (!stopRequested.load())
    {
        // -----------------------------
        // 1. カメラからフレームを取得
        // -----------------------------
        if (!capture.read(originalImage) || originalImage.empty())
        {
            if (!frameErrorReported)
            {
                std::cerr
                    << "Failed to read camera frame."
                    << std::endl;

                SendProtocolMessage(
                    "ERROR|FRAME_READ_FAILED");

                frameErrorReported = true;
            }

            std::this_thread::sleep_for(
                std::chrono::milliseconds(100));

            continue;
        }

        // 読み取りが復旧したので、次のエラー通知を許可
        frameErrorReported = false;

        // -----------------------------
        // 2. 画像サイズを調整
        // -----------------------------
        cv::resize(
            originalImage,
            frameImage,
            imageSize);

        // -----------------------------
        // 3. 明るさ調整
        // -----------------------------
        cv::cvtColor(
            frameImage,
            frameImage,
            cv::COLOR_BGR2HSV);

        for (int y = 0; y < frameImage.rows; y++)
        {
            for (int x = 0; x < frameImage.cols; x++)
            {
                cv::Vec3b hsv =
                    frameImage.at<cv::Vec3b>(y, x);

                if (hsv[2] + 50 >= 255)
                {
                    hsv[2] = 255;
                }
                else
                {
                    hsv[2] += 50;
                }

                frameImage.at<cv::Vec3b>(y, x) = hsv;
            }
        }

        cv::cvtColor(frameImage, frameImage, cv::COLOR_HSV2BGR);

        // -----------------------------
        // 3. 明るさ調整
        // -----------------------------
        cv::cvtColor(frameImage, frameImage, cv::COLOR_BGR2HSV);
        for (int y = 0; y < frameImage.rows; y++)
        {
            for (int x = 0; x < frameImage.cols; x++)
            {
                cv::Vec3b hsv = frameImage.at<cv::Vec3b>(y, x);

                if (hsv[2] + 50 >= 255)
                {
                    hsv[2] = 255;
                }
                else
                {
                    hsv[2] += 50;
                }

                frameImage.at<cv::Vec3b>(y, x) = hsv;
            }
        }
        cv::cvtColor(frameImage, frameImage, cv::COLOR_HSV2BGR);

        // // ②検出情報を受け取るための配列を用意する
        // std::vector<cv::Rect> faces, eyes;

        // // ③画像中から検出対象の情報を取得する
        // faceClassifier.detectMultiScale(frameImage, faces, 1.1, 3, 0, cv::Size(20, 20));
        // eyeClassifier.detectMultiScale(frameImage, eyes, 1.1, 3, 0, cv::Size(10, 10));

        // if (faces.size() != 0)
        // {
        //     int maxsize = -1;
        //     // ④顔領域の検出
        //     for (int i = 0; i < faces.size(); i++)
        //     {
        //         // 検出情報から顔の位置情報を取得
        //         face = faces[i];
        //         // 大きさによるチェック。
        //         if (face.width * face.height < 5000 * 5000)
        //         {
        //             continue; // 小さい矩形は採用しない
        //         }
        //         else
        //         {
        //             printf("%d\n", face.width * face.height);

        //             if (maxsize < face.width * face.height)
        //             {
        //                 maxsize = face.width * face.height;

        //                 face.x -= face.width * 0.1;
        //                 face.y -= face.height * 0.4;
        //                 face.width += face.width * 0.2;
        //                 face.height += face.height * 0.5;
        //             }
        //         }

        //         // ⑤画像の加工
        //         // change_face_color(frameImage, hsvImage, face);

        //         // 取得した顔の位置情報に基づき、矩形描画を行う
        //         // cv::rectangle(frameImage,cv::Point(face.x, face.y),cv::Point(face.x + face.width, face.y + face.height),CV_RGB(255, 0, 0),3, cv::LINE_AA);
        //     }
        // }
        // else
        // {
        //     printf("no\n");
        // }

        // -----------------------------
        // 4. 顔検出
        // -----------------------------
        // ②検出情報を受け取るための配列を用意する
        std::vector<cv::Rect> faces, eyes;
        cv::Mat detectionGray;
        cv::cvtColor(frameImage, detectionGray, cv::COLOR_BGR2GRAY);
        cv::equalizeHist(detectionGray, detectionGray);

        faceClassifier.detectMultiScale(
            detectionGray,
            faces,
            1.1,
            3,
            0,
            cv::Size(20, 20));

        bool hasFace = false;
        cv::Rect face;
        int largestArea = 5000;

        for (const cv::Rect &candidate : faces)
        {
            int area = candidate.width * candidate.height;

            if (area <= largestArea)
            {
                continue;
            }

            largestArea = area;
            face = candidate;
            hasFace = true;
        }

        if (hasFace)
        {
            face.x -= static_cast<int>(face.width * 0.1f);
            face.y -= static_cast<int>(face.height * 0.4f);
            face.width += static_cast<int>(face.width * 0.2f);
            face.height += static_cast<int>(face.height * 0.5f);
        }

        int cx = 0, cy = 0;

        // -----------------------------
        // 5. 犬の耳・口を合成
        // -----------------------------
        if (hasFace)
        {
            // イヌみみ
            cv::Point2f original1[4], translate1[4];
            original1[0] = cv::Point2f(0, 0);                                 // A
            original1[1] = cv::Point2f(sourceImage1.cols, 0);                 // B
            original1[2] = cv::Point2f(sourceImage1.cols, sourceImage1.rows); // C
            original1[3] = cv::Point2f(0, sourceImage1.rows);                 // D

            translate1[0] = cv::Point2f(face.x - 30, face.y - 70);                                  // A'
            translate1[1] = cv::Point2f(face.x + face.width + 30, face.y - 70);                     // B'
            translate1[2] = cv::Point2f(face.x + face.width + 30, face.y + (face.height / 3) - 20); // C'
            translate1[3] = cv::Point2f(face.x - 30, face.y + (face.height / 3) - 20);              // D'
            cv::Mat persMat1 = cv::getPerspectiveTransform(original1, translate1);

            cv::warpPerspective(sourceImage1, tempImage1, persMat1, frameImage.size(),
                                cv::INTER_LINEAR, cv::BORDER_CONSTANT, cv::Scalar(255, 255, 255));

            cv::cvtColor(tempImage1, grayImage, cv::COLOR_BGR2GRAY);
            for (int y = 0; y < tempImage1.rows; y++)
            {
                for (int x = 0; x < tempImage1.cols; x++)
                {
                    unsigned char g = grayImage.at<unsigned char>(y, x);

                    if (g <= 245)
                    {
                        frameImage.at<cv::Vec3b>(y, x) = tempImage1.at<cv::Vec3b>(y, x);
                    }
                }
            }

            // イヌくち
            cv::Point2f original2[4], translate2[4];
            original2[0] = cv::Point2f(0, 0);                                 // A
            original2[1] = cv::Point2f(sourceImage2.cols, 0);                 // B
            original2[2] = cv::Point2f(sourceImage2.cols, sourceImage2.rows); // C
            original2[3] = cv::Point2f(0, sourceImage2.rows);                 // D

            translate2[0] = cv::Point2f(face.x + (face.width / 10), face.y + (face.height * 1 / 2) - 10);     // A'
            translate2[1] = cv::Point2f(face.x + (face.width * 9 / 10), face.y + (face.height * 1 / 2) - 10); // B'
            translate2[2] = cv::Point2f(face.x + (face.width * 9 / 10), face.y + face.height + 20);           // C'
            translate2[3] = cv::Point2f(face.x + (face.width / 10), face.y + face.height + 20);               // D'
            cv::Mat persMat2 = cv::getPerspectiveTransform(original2, translate2);

            cv::warpPerspective(sourceImage2, tempImage2, persMat2, frameImage.size(),
                                cv::INTER_LINEAR, cv::BORDER_CONSTANT, cv::Scalar(255, 255, 255));

            cv::cvtColor(tempImage2, grayImage, cv::COLOR_BGR2GRAY);
            for (int y = 0; y < tempImage2.rows; y++)
            {
                for (int x = 0; x < tempImage2.cols; x++)
                {
                    unsigned char g = grayImage.at<unsigned char>(y, x);

                    if (g <= 245)
                    {
                        frameImage.at<cv::Vec3b>(y, x) = tempImage2.at<cv::Vec3b>(y, x);
                    }
                }
            }
        }
        // -----------------------------
        // 6. deco画像を合成
        // -----------------------------
        cv::Point2f original3[4];
        original3[0] = cv::Point2f(0, 0);                                 // A
        original3[1] = cv::Point2f(sourceImage3.cols, 0);                 // B
        original3[2] = cv::Point2f(sourceImage3.cols, sourceImage3.rows); // C
        original3[3] = cv::Point2f(0, sourceImage3.rows);                 // D

        cv::Mat persMat3 = cv::getPerspectiveTransform(original3, original3);

        cv::warpPerspective(sourceImage3, tempImage3, persMat3, frameImage.size(),
                            cv::INTER_LINEAR, cv::BORDER_CONSTANT, cv::Scalar(255, 255, 255));

        cv::cvtColor(tempImage3, grayImage, cv::COLOR_BGR2GRAY);
        for (int y = 0; y < tempImage3.rows; y++)
        {
            for (int x = 0; x < tempImage3.cols; x++)
            {
                unsigned char g = grayImage.at<unsigned char>(y, x);

                if (g <= 250)
                {
                    frameImage.at<cv::Vec3b>(y, x) = tempImage3.at<cv::Vec3b>(y, x);
                }
            }
        }

        // 認識結果画像表示
        // cv::imshow("Face", frameImage);
        // cv::imshow("tmp1", tempImage1);
        // cv::imshow("tmp2", tempImage2);
        // cv::imshow("tmp3", tempImage3);

        // -----------------------------
        // 7. 最新の加工結果を保持
        // -----------------------------
        pic = frameImage.clone();

        // 最初の加工済みフレームが完成したら
        // UnityへREADYを一度だけ送る
        if (!readySent)
        {
            SendProtocolMessage("READY");
            readySent = true;
        }

        // char key = cv::waitKey(10);
        // if (key == 'q')
        // {
        //     break;
        // }
        // else if (key == 'p')
        // {
        //     std::string s = saveDir + "/" + std::to_string(picNum) + ".jpg";
        //     cv::imwrite(s, pic);
        //     picNum++;
        //     printf("%d保存完了\n", picNum);
        // }

        // -----------------------------
        // 8. Unityからの命令を処理
        // -----------------------------
        std::string command;

        while (TryTakeCommand(command))
        {
            HandleCommand(command, pic);
        }
    }

    // -----------------------------
    // 9. 終了処理
    // -----------------------------

    // QUITまたは標準入力切断時には
    // ReadCommandsスレッドも終了している
    if (commandThread.joinable())
    {
        commandThread.join();
    }

    capture.release();

    return 0;
}
