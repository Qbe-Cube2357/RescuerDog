//g++ m5.cpp -std=c++11 -framework OpenGL -framework GLUT -Wno-deprecated
#include <iostream>  //入出力関連ヘッダ
//#include <GLUT/glut.h> //OpenGL
#include <math.h>      //数学関数
#include <unistd.h>    //シリアル通信用
#include <fcntl.h>     //シリアル通信用
#include <termios.h>   //シリアル通信用

#define DEV_NAME_L "/dev/cu.M5StickC-L"
#define DEV_NAME_R "/dev/cu.M5StickC-R"
#define BAUD_RATE 9600
#define BUFF_SIZE 4096

void getSerialData();
void analyzeBuffer(int devID);
void initSerial();

// シリアル通信関係
//0:L
//1:R
int fd[2];                    // シリアルポート
char bufferAll[2][BUFF_SIZE]; // 蓄積バッファデータ
int bufferPoint[2] = {0,0};       // 蓄積バッファデータサイズ
double val[2];             // 受信データ
double valOld[2] = {0,0};           //古いデータ(確認用)


//int countL=0,countR=0;
int count=0;

int main(int argc, const char* argv[])
{
    initSerial();
    
        while(1){
            //readFromSerial(0); // L用
            //readFromSerial(1); // R用
        
            getSerialData();
            
            if(abs(val[0])<1.0){
                val[0] = 0.0;
                //countL=1;
            }
            if(abs(val[1])<1.0){
                val[1] = 0.0;
                //countR=1;
            }
                
            if(val[0]!=valOld[0] || val[1]!=valOld[1]){
                // テキストファイルへ保存
                FILE *fp = fopen("data.txt", "w"); // 追記モード
                if (fp) {
                    
                    
                    fprintf(fp, "%f,%f\n", val[0],val[1]);
                    fflush(fp); //強制書き出し
                    fclose(fp);
                }
                
                std::cout << "Saved: " << val[0] << "," << val[1] << std::endl;
            }
            valOld[0] = val[0];
            valOld[1] = val[1];
        }
        
        //count=0;
    
    return 0;
}

// シリアルポート初期化
void initSerial()
{
    struct termios tio;

    // シリアルポートオープン
    fd[0] = open(DEV_NAME_L, O_RDWR | O_NONBLOCK);
    fd[1] = open(DEV_NAME_R, O_RDWR | O_NONBLOCK);
    if (fd[0] < 0)
    { // オープン失敗
        printf("ERROR on device open\n");
        exit(1);
    }
    else if (fd[1] < 0)
    { // オープン失敗
        printf("ERROR on device open\n");
        exit(1);
    }
    printf("init serial port\n");

    // 初期設定
    memset(&tio, 0, sizeof(tio));   //tioという構造体メモリ領域を全てクリア(初期化)
    tio.c_cflag = CS8 | CLOCAL | CREAD; //文字サイズを「8ビット」に設定・モデム制御信号を無視にする・受信を有効にする
    tio.c_cc[VTIME] = 100;  //タイムアウト時間設定
    
    // ボーレート設定
    cfsetispeed(&tio, BAUD_RATE);   //受信速度を9600にする
    cfsetospeed(&tio, BAUD_RATE);   //送信速度を9600にする
    
    // デバイス設定実施
    for(int i=0; i<2; i++){
        tcsetattr(fd[i], TCSANOW, &tio);   //設定してきた「tio」の内容をfdに適用
    }
    
    std::cout << "Serial port initialized." << std::endl;   //初期設定が完了したことをターミナルに表示
}

// bufferAll[]分析
void analyzeBuffer(int devID)
{
    // どんなデータが届いているか確認用
    std::cout << "Received line with bufferPoint: " << bufferPoint << std::endl;
    
    char inData[2][BUFF_SIZE];  //格納用2次元配列。2つの要素まで保存可能。それぞれ「BUFF_SIZE」分の文字の長さを持つ
    int dataID = 0; // 「何番目の数値を処理しているか」カウンタ
    int j = 0;      // 「何文字目を書き込んでいるか」ポインタ
    

    for (int i = 0; i < bufferPoint[devID]; i++)   //受信した前データを異文字ずつ読み込み
    {
        inData[dataID][j] = bufferAll[devID][i]; // bufferAll[]の値をinData[][j]に1文字ずつ格納

        if (inData[dataID][j] == ',')   // カンマが来たら次の文字列へ
        {
            inData[dataID][j] = '\0'; // 文字列inData[dataID]完成(null文字を最後につける)
            dataID++;                 // 次の数値データ（文字列）に移動
            if (dataID == 6)
                break; // 6を超えないように
            j = 0;     // inDataの先頭から書き込むため
        }
        else if (inData[dataID][j] == '\n') // 改行が来たら次の文字列へ
        {
            inData[dataID][j] = '\0'; // 文字列inData[dataID]完成(null文字を最後につける)
            dataID++;                 // 次の数値データ（文字列）に移動
            break;                    // 全データの取得完了
        }
        else    // データの途中
        {
            j++; // 次の文字に移動
        }
    }

//    if (dataID >= 1)    // データが一つ以上取り出せたか確認
//    {
//        for(int i=0; i<dataID; i++){
//            // 文字列を小数に変換
//            val[i] = atof(inData[i]);
//        }
//    }
    
    val[devID] = atof(inData[0]);
    
    
    
    //count++;
}

// シリアルポートからデータ取得
void getSerialData()
{
    //std::cout << "!" << std::endl;  
    // シリアルポートからの入力用
    char buffer[BUFF_SIZE]; //一時格納用配列
    // buffer[]に受信データ格納
    int len[2];
    for(int i=0; i<2; i++){
        len[i] = read(fd[i], buffer, BUFF_SIZE);    //fdから最大BUFF_SIZE分データ読み込み・読み込んだ数字をlenに代入
        
        // bufferAll[]に蓄積
        for (int j = 0; j < len[i]; j++)    //読み込んだ文字数分1文字ずつループ処理
        {
            bufferAll[i][bufferPoint[i]] = buffer[j]; // bufferAllの末尾に文字を追加
            bufferPoint[i]++;                      // 次の位置に移動
            
            if (buffer[j] == '\n')  // 読み込んだ文字が'\n'(改行)ならそのセットを終了
            {
                analyzeBuffer(i); // データの解析(１セット分)
                bufferPoint[i] = 0; // 格納用配列の頭へ
            }
        }
    }
}
