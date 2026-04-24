using UnityEngine;
using uOSC;

[RequireComponent(typeof(uOscServer))]
[RequireComponent(typeof(uOscClient))]
public class MultiDeviceController : MonoBehaviour
{
    [Header("▼ ここにGameManagerオブジェクトをドラッグ＆ドロップ")]
    public ExperienceManager 体験マネージャー;

    [Space(10)]
    [Header("▼ 現在の脳みその状態（テスト・確認用）")]
    [Tooltip("デバイスから送られてくる現在の握力（0.0〜1.0）が表示されます")]
    public float 現在の脳の握力 = 0f;

    private uOscClient 送信機;

    void Start()
    {
        var 受信機 = GetComponent<uOscServer>();
        受信機.onDataReceived.AddListener(データを受信した時);
        
        送信機 = GetComponent<uOscClient>();
    }

    // --- 【受信】脳みそデバイスからデータが届いた時の処理 ---
    void データを受信した時(Message メッセージ)
    {
        // 1. 握る強さ（0.0 ~ 1.0）を受信
        if (メッセージ.address == "/brain/grip")
        {
            現在の脳の握力 = (float)メッセージ.values[0];
            // 必要に応じて、ここでノイズを強める処理などを呼び出します
        }
        
        // 2. 「投げた！」というトリガーを受信
        else if (メッセージ.address == "/brain/throw")
        {
            // 現在が「フェーズ2（接触）」の時だけ反応する
            if (体験マネージャー.現在のフェーズ == ExperienceManager.進行フェーズ.フェーズ2_接触_Contact)
            {
                Debug.Log("<color=red>脳デバイスの投下を検知しました！</color>");
                体験マネージャー.次のフェーズへ進む(); // フェーズ3へ強制移行
            }
        }
    }

    // --- 【送信】Unityからデバイスへ信号を送る ---
    // ExperienceManager の設定画面から「＋」ボタンで追加して使います
    
    public void 頭部デバイスに衝撃信号を送信する()
    {
        送信機.Send("/head/shock", 1);
        Debug.Log("頭部デバイスへ振動開始信号を送信しました。");
    }

    public void 脳デバイスに脈動信号を送信する()
    {
        送信機.Send("/brain/pulse", 1);
        Debug.Log("脳デバイスへ脈動開始信号を送信しました。");
    }
}