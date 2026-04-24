using UnityEngine;
using UnityEngine.Events;
using System.Collections;

public class ExperienceManager : MonoBehaviour
{
    // フェーズの名前も日本語（英語）のハイブリッドにしてわかりやすくしました
    public enum 進行フェーズ
    {
        待機_Setup = 0,
        フェーズ1_導入_Intro = 1,
        フェーズ2_接触_Contact = 2,
        フェーズ3_破棄_Throw = 3,
        フェーズ4_夢中遊行_Sleepwalk = 4,
        フェーズ5_終焉_Ending = 5
    }

    [Header("▼ 現在のステータス（プレイ中に確認・変更可能）")]
    public 進行フェーズ 現在のフェーズ = 進行フェーズ.待機_Setup;

    [Space(10)]
    [Header("▼ 開発用デバッグ機能")]
    public bool 途中からテストする = false;
    public 進行フェーズ テスト開始フェーズ = 進行フェーズ.待機_Setup;

    [Space(10)]
    [Header("▼ 疑似・後ろ倒れギミック（フェーズ3用）")]
    [Tooltip("OVRCameraRigの親オブジェクト（XR Originなど）を指定してください")]
    public Transform プレイヤーのXR_Origin; 
    [Tooltip("マイナスの値を入れると上を向く（後ろに倒れる）動きになります")]
    public float 後ろに倒れる角度 = -25f;
    [Tooltip("この秒数かけて倒れます。短いほど衝撃的です")]
    public float 倒れるまでの秒数 = 0.15f;

    [Space(10)]
    [Header("▼ 各フェーズ開始時に実行する演出リスト（＋ボタンで追加）")]
    public UnityEvent 待機フェーズ開始時の設定;
    public UnityEvent フェーズ1_導入_開始時の設定;
    public UnityEvent フェーズ2_接触_開始時の設定;
    public UnityEvent フェーズ3_破棄_開始時の設定;
    public UnityEvent フェーズ4_夢中遊行_開始時の設定;
    public UnityEvent フェーズ5_終焉_開始時の設定;

    void Start()
    {
        // 途中からテストする設定なら、そのフェーズから開始
        現在のフェーズ = 途中からテストする ? テスト開始フェーズ : 進行フェーズ.待機_Setup;
        フェーズを実行する();
    }

    // 外部（OSCや当たり判定）から呼ばれる「次へ進む」ボタン
    public void 次のフェーズへ進む()
    {
        if (現在のフェーズ == 進行フェーズ.フェーズ5_終焉_Ending) return; 
        現在のフェーズ++;
        フェーズを実行する();
    }

    [ContextMenu("★ 強制的に『現在のフェーズ』を実行（テスト用）")]
    public void 強制実行ボタン()
    {
        フェーズを実行する();
    }

    void フェーズを実行する()
    {
        Debug.Log($"<color=cyan>[進行管理]</color> {現在のフェーズ} が開始されました");

        switch (現在のフェーズ)
        {
            case 進行フェーズ.待機_Setup:
                待機フェーズ開始時の設定.Invoke();
                break;

            case 進行フェーズ.フェーズ1_導入_Intro:
                フェーズ1_導入_開始時の設定.Invoke();
                break;

            case 進行フェーズ.フェーズ2_接触_Contact:
                フェーズ2_接触_開始時の設定.Invoke();
                break;

            case 進行フェーズ.フェーズ3_破棄_Throw:
                フェーズ3_破棄_開始時の設定.Invoke();
                StartCoroutine(後ろ倒れギミックを実行());
                StartCoroutine(指定秒数待って次へ(10f)); 
                break;

            case 進行フェーズ.フェーズ4_夢中遊行_Sleepwalk:
                フェーズ4_夢中遊行_開始時の設定.Invoke();
                StartCoroutine(指定秒数待って次へ(45f)); 
                break;

            case 進行フェーズ.フェーズ5_終焉_Ending:
                フェーズ5_終焉_開始時の設定.Invoke();
                break;
        }
    }

    IEnumerator 指定秒数待って次へ(float 待機秒数)
    {
        yield return new WaitForSeconds(待機秒数);
        次のフェーズへ進む();
    }

    IEnumerator 後ろ倒れギミックを実行()
    {
        if (プレイヤーのXR_Origin == null) yield break;

        Quaternion 初期角度 = プレイヤーのXR_Origin.rotation;
        Vector3 初期位置 = プレイヤーのXR_Origin.position;

        Quaternion 目標角度 = 初期角度 * Quaternion.Euler(後ろに倒れる角度, 0, 0);
        Vector3 目標位置 = 初期位置 + new Vector3(0, -0.1f, -0.1f);

        float 経過時間 = 0f;

        while (経過時間 < 倒れるまでの秒数)
        {
            プレイヤーのXR_Origin.rotation = Quaternion.Lerp(初期角度, 目標角度, 経過時間 / 倒れるまでの秒数);
            プレイヤーのXR_Origin.position = Vector3.Lerp(初期位置, 目標位置, 経過時間 / 倒れるまでの秒数);
            経過時間 += Time.deltaTime;
            yield return null;
        }

        プレイヤーのXR_Origin.rotation = 目標角度;
        プレイヤーのXR_Origin.position = 目標位置;
    }
}