using UnityEngine;
using uOSC;

[RequireComponent(typeof(uOscServer))]
[RequireComponent(typeof(uOscClient))]
public class MultiDeviceController : MonoBehaviour
{
    [Header("▼ ここにGameManagerオブジェクトを登録")]
    public ExperienceManager gameManager;

    [Space(10)]
    [Header("▼ 現在の脳の握力（テスト・確認用）")]
    [Tooltip("デバイスから送られてくる現在の握力（0.0〜1.0）")]
    public float currentBrainGrip = 0f;

    private uOscClient oscClient;

    void Start()
    {
        var oscServer = GetComponent<uOscServer>();
        oscServer.onDataReceived.AddListener(OnDataReceived);
        
        oscClient = GetComponent<uOscClient>();
    }

    void OnDataReceived(Message message)
    {
        if (message.address == "/brain/grip")
        {
            currentBrainGrip = (float)message.values[0];
            // ここでノイズ演出の更新などを呼び出せます
        }
        else if (message.address == "/brain/throw")
        {
            // フェーズ2の時だけ「投げた」判定を許可する
            if (gameManager.currentPhase == ExperienceManager.Phase.Contact)
            {
                Debug.Log("<color=red>脳デバイスの投下を検知！フェーズ3へ移行します。</color>");
                gameManager.GoToNextPhase(); 
            }
        }
    }

    // --- 【送信】UnityEventの「＋」ボタンから呼び出せる命令 ---
    
    public void SendHeadShockSignal()
    {
        oscClient.Send("/head/shock", 1);
        Debug.Log("頭部デバイスへ『衝撃』信号を送信しました。");
    }

    public void SendBrainPulseSignal()
    {
        oscClient.Send("/brain/pulse", 1);
        Debug.Log("脳デバイスへ『脈動』信号を送信しました。");
    }
}