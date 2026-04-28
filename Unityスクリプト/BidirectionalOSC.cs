using UnityEngine;
using extOSC;

public class BidirectionalOSC : MonoBehaviour
{
    [Header("OSC Settings")]
    public OSCReceiver Receiver;
    public OSCTransmitter Transmitter;

    void Start()
    {
        // 【受信の設定】ESP32からのデータを受け取る準備
        Receiver.Bind("/esp32/sensor", OnReceiveSensorData);
    }

    // 【受信処理】ESP32からデータが届いた時に呼ばれる
    private void OnReceiveSensorData(OSCMessage message)
    {
        int sensorValue = message.Values[0].IntValue;
        Debug.Log("ESP32からのセンサー値: " + sensorValue);
    }

    void Update()
    {
        // 【送信処理】スペースキーでESP32のLEDをON/OFF
        if (Input.GetKeyDown(KeyCode.Space))
        {
            SendLedCommand(1);
        }
        else if (Input.GetKeyUp(KeyCode.Space))
        {
            SendLedCommand(0);
        }
    }

    // 【送信用の関数】
    private void SendLedCommand(int state)
    {
        var message = new OSCMessage("/unity/led");
        message.AddValue(OSCValue.Int(state));
        Transmitter.Send(message);
    }
}