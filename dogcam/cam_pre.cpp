/*
 g++ -std=c++11 cam.cpp `pkg-config --cflags --libs opencv4`
 */
#include <iostream>           //入出力関連ヘッダ
#include <opencv2/opencv.hpp> //OpenCV関連ヘッダ
#include <string>

int col = 0;
cv::Rect face;

// main関数
int main(int argc, char *argv[])
{
    // OpenCV初期設定処理
    // カメラキャプチャの初期化
    cv::VideoCapture capture = cv::VideoCapture(0);

    if (capture.isOpened() == 0)
    {
        // カメラが見つからないときはメッセージを表示して終了
        printf("Camera not found\n");
        exit(1);
    }

    cv::Mat sourceImage1 = cv::imread("DogEar1.jpg", cv::IMREAD_COLOR);
    cv::Mat sourceImage2 = cv::imread("DogMouth.jpg", cv::IMREAD_COLOR);
    cv::Mat sourceImage3 = cv::imread("deco.jpg", cv::IMREAD_COLOR);

    cv::Mat pic;
    int picNum = 0;

    std::string saveDir = "保存フォルダ";

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
    cv::namedWindow("Frame");
    cv::moveWindow("Frame", 0, 0);
    cv::namedWindow("Face");
    cv::moveWindow("Face", imageSize.width, 0);

    // ①正面顔検出器の読み込み
    faceClassifier.load("haarcascades/haarcascade_frontalface_default.xml");
    eyeClassifier.load("haarcascades/haarcascade_mcs_eyepair_small.xml");

    while (1)
    {
        // ビデオキャプチャから1フレーム画像取得
        capture >> originalImage;
        cv::resize(originalImage, frameImage, imageSize);

        // フレーム画像表示
        cv::imshow("Frame", frameImage);

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

        // ②検出情報を受け取るための配列を用意する
        std::vector<cv::Rect> faces, eyes;

        // ③画像中から検出対象の情報を取得する
        faceClassifier.detectMultiScale(frameImage, faces, 1.1, 3, 0, cv::Size(20, 20));
        eyeClassifier.detectMultiScale(frameImage, eyes, 1.1, 3, 0, cv::Size(10, 10));

        if (faces.size() != 0)
        {
            int maxsize = -1;
            // ④顔領域の検出
            for (int i = 0; i < faces.size(); i++)
            {
                // 検出情報から顔の位置情報を取得
                face = faces[i];
                // 大きさによるチェック。
                if (face.width * face.height < 5000 * 5000)
                {
                    continue; // 小さい矩形は採用しない
                }
                else
                {
                    printf("%d\n", face.width * face.height);

                    if (maxsize < face.width * face.height)
                    {
                        maxsize = face.width * face.height;

                        face.x -= face.width * 0.1;
                        face.y -= face.height * 0.4;
                        face.width += face.width * 0.2;
                        face.height += face.height * 0.5;
                    }
                }

                // ⑤画像の加工
                // change_face_color(frameImage, hsvImage, face);

                // 取得した顔の位置情報に基づき、矩形描画を行う
                // cv::rectangle(frameImage,cv::Point(face.x, face.y),cv::Point(face.x + face.width, face.y + face.height),CV_RGB(255, 0, 0),3, cv::LINE_AA);
            }
        }
        else
        {
            printf("no\n");
        }

        int cx = 0, cy = 0;

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

        // deco
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

        // 眼領域の検出
        //        for (int i = 0; i < eyes.size(); i++) {
        //            // 検出情報から顔の位置情報を取得
        //            cv::Rect eye = eyes[i];
        //            // 取得した顔の位置情報に基づき、矩形描画を行う
        //            cv::rectangle(frameImage,cv::Point(eye.x, eye.y),cv::Point(eye.x + eye.width, eye.y + eye.height),CV_RGB(0, 255, 0),3, cv::LINE_AA);
        //
        //            //change_face_color(frameImage, hsvImage, eye);
        //        }

        // 認識結果画像表示
        cv::imshow("Face", frameImage);
        cv::imshow("tmp1", tempImage1);
        cv::imshow("tmp2", tempImage2);
        cv::imshow("tmp3", tempImage3);

        pic = frameImage.clone();

        char key = cv::waitKey(10);
        if (key == 'q')
        {
            break;
        }
        else if (key == 'p')
        {
            std::string s = saveDir + "/" + std::to_string(picNum) + ".jpg";
            cv::imwrite(s, pic);
            picNum++;
            printf("%d保存完了\n", picNum);
        }
    }

    return 0;
}
