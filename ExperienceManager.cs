using UnityEngine;
using System.Collections;

/// <summary>
/// 脳髄XR体験 総合司令塔クラス
/// 全フェーズの進行と、ESP・VRデバイス間のデータフローを統括する
/// </summary>
public class ExperienceManager : MonoBehaviour
{
    // フェーズ定義
    public enum Phase
    {
        Wait,           // 待機状態
        Phase1_Intro,   // 【導入：剥離】アテンド・正木教授出現
        Phase2_Contact, // 【接触：濁流】脳髄論・頭部デバイスへのフィードバック
        Phase3_Throw,   // 【絶頂：破棄】脳を投げる・ブラックアウト・物理ショック
        Phase4_Erosion, // 【変容：夢中遊行】SR切り替え・代替現実・操作不能
        Phase5_Ending   // 【終焉：還流】HMD着脱・現実の肖像画・エンディング
    }

    [Header("Current State")]
    public Phase currentPhase = Phase.Wait;

    [Header("Settings")]
    public float brainThrowVelocityThreshold = 5.0f; // 脳を投げたか判定する速度の閾値

    // --- 各種外部スクリプトの参照（適宜アタッチしてください） ---
    // public ESPNetworkManager espManager; // ESPとの通信用（OSCやSerial）
    // public VisualEffectManager vfxManager; // ホワイトアウト/ノイズ制御など
    // public AudioManager audioManager; // バイノーラル音声・幻聴制御
    // public OVRCameraRig cameraRig; // Questコントローラーの位置・速度取得用

    private void Start()
    {
        // デバッグ用：起動時にPhase1から開始
        ChangePhase(Phase.Phase1_Intro);
    }

    private void Update()
    {
        // 常に監視すべき処理（フェーズごとの毎フレーム処理）
        switch (currentPhase)
        {
            case Phase.Phase2_Contact:
                UpdateContactPhase();
                break;
            case Phase.Phase3_Throw:
                UpdateThrowPhase();
                break;
            case Phase.Phase5_Ending:
                UpdateEndingPhase();
                break;
        }

        // デバッグ用：スペースキーで強制的に次のフェーズへ
        if (Input.GetKeyDown(KeyCode.Space))
        {
            GoToNextPhase();
        }
    }

    /// <summary>
    /// フェーズの切り替えと初期化処理
    /// </summary>
    public void ChangePhase(Phase newPhase)
    {
        currentPhase = newPhase;
        Debug.Log($"[ExperienceManager] フェーズ移行: {currentPhase}");

        StopAllCoroutines(); // 前フェーズのコルーチンをキャンセル

        switch (currentPhase)
        {
            case Phase.Phase1_Intro:
                StartCoroutine(Routine_Phase1_Intro());
                break;
            case Phase.Phase2_Contact:
                StartCoroutine(Routine_Phase2_Contact());
                break;
            case Phase.Phase3_Throw:
                StartCoroutine(Routine_Phase3_Throw());
                break;
            case Phase.Phase4_Erosion:
                StartCoroutine(Routine_Phase4_Erosion());
                break;
            case Phase.Phase5_Ending:
                StartCoroutine(Routine_Phase5_Ending());
                break;
        }
    }

    private void GoToNextPhase()
    {
        if ((int)currentPhase < System.Enum.GetNames(typeof(Phase)).Length - 1)
        {
            ChangePhase((Phase)((int)currentPhase + 1));
        }
    }

    // =========================================================
    // 各フェーズのコルーチン（時間経過ベースの演出）
    // =========================================================

    private IEnumerator Routine_Phase1_Intro()
    {
        // 0:00~ パススルー開始、案内役退場
        Debug.Log("案内役が退出、正木教授の登場準備");
        
        // 頭部のバイブレーター作動（ESPへ送信）
        // espManager.SendHeadsetVibration(true);
        // audioManager.PlayBoneSawSound();
        
        yield return new WaitForSeconds(10.0f); // 10秒待機

        // 正木教授出現・爆語り開始
        // vfxManager.ShowProfessorMasaki();
        // audioManager.PlayProfessorSpeech();
        
        yield return new WaitForSeconds(10.0f); // 語りの後、次のフェーズへ促す
        
        ChangePhase(Phase.Phase2_Contact);
    }

    private IEnumerator Routine_Phase2_Contact()
    {
        // 脳デバイスに触れるのを待つフェーズ
        Debug.Log("脳デバイスへの接触待機...");
        yield break; 
        // UpdateContactPhase() で触覚ループを処理し、条件を満たしたらPhase3へ
    }

    private IEnumerator Routine_Phase3_Throw()
    {
        // 脳が投げられた瞬間の処理
        Debug.Log("脳デバイス投棄！物理ショック発動");

        // 1. ESPへミラー跳ね上げ命令送信
        // espManager.TriggerMirrorFlip();

        // 2. 視界ホワイトアウト → ブラックアウト
        // vfxManager.TriggerWhiteoutThenBlackout();

        // 3. すべての音を消失させる
        // audioManager.StopAllSounds();

        yield return new WaitForSeconds(5.0f); // 暗闇の中での潜伏時間

        // SR映像へ切り替えなどの準備
        ChangePhase(Phase.Phase4_Erosion);
    }

    private IEnumerator Routine_Phase4_Erosion()
    {
        Debug.Log("代替現実 / 夢中遊行フェーズ開始");

        // 視界を戻す（録画されたSR映像の再生 or コントローラー入力無視での自動歩行開始）
        // vfxManager.PlaySRVideo();
        // audioManager.PlayMultipleVoices("大丈夫ですか");

        yield return new WaitForSeconds(25.0f); // 演出時間

        ChangePhase(Phase.Phase5_Ending);
    }

    private IEnumerator Routine_Phase5_Ending()
    {
        Debug.Log("エンディング待機。HMDが外されるのを待つ...");
        yield break;
        // UpdateEndingPhase() でHMD着脱を検知して完了
    }

    // =========================================================
    // 各フェーズの毎フレーム処理（センサ入力・フィードバック）
    // =========================================================

    private void UpdateContactPhase()
    {
        // 【脳デバイスの圧力センサ -> 頭部デバイスのサーボへ同期】
        
        // float[] pressures = espManager.GetBrainPressures();
        // float averagePressure = CalculateAverage(pressures);

        // 握る力に応じて映像ノイズと音響を激化
        // vfxManager.SetNoiseIntensity(averagePressure);
        // audioManager.SetHeartbeatIntensity(averagePressure);

        // 頭部のサーボへ押し込み圧力を送信
        // espManager.SendHeadsetServos(pressures);

        // ※もし脳が一定の力以上で握られ続けたら、強制的にPhase3へ移行するタイマーを仕込んでも良い
    }

    private void UpdateThrowPhase()
    {
        // 【投げるアクションの検知】
        // Questコントローラー（脳デバイス内蔵）の加速度・速度を取得して投擲を検知

        /*
        Vector3 brainVelocity = OVRInput.GetLocalControllerVelocity(OVRInput.Controller.RHand); // 例
        if (brainVelocity.magnitude > brainThrowVelocityThreshold)
        {
            ChangePhase(Phase.Phase3_Throw);
        }
        */
    }

    private void UpdateEndingPhase()
    {
        // 【HMD着脱検知】
        // HMDを外した（近接センサーがオフになった）瞬間を検知する
        // OVRPlugin.userPresent などを使用

        /*
        if (!OVRPlugin.userPresent) 
        {
            // 最後の「剥き出しの正木の声」を外部スピーカーから鳴らす
            // espManager.PlayExternalSpeaker();
            Debug.Log("体験終了");
            enabled = false; // Managerの停止
        }
        */
    }
}