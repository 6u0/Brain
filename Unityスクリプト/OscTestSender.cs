using UnityEngine;
using extOSC;

public class OscTestSender : MonoBehaviour
{
    [Header("OSC")]
    [SerializeField] private OSCTransmitter transmitter;
    [SerializeField] private string address = "/test/value";

    [Header("Keys")]
    [SerializeField] private KeyCode keySendZero = KeyCode.A;
    [SerializeField] private KeyCode keySendHundred = KeyCode.B;

    void Update()
    {
        if (Input.GetKeyDown(keySendZero))
        {
            SendInt(0);
        }
        else if (Input.GetKeyDown(keySendHundred))
        {
            SendInt(100);
        }
    }

    private void SendInt(int value)
    {
        if (transmitter == null || string.IsNullOrWhiteSpace(address)) return;
        var message = new OSCMessage(address);
        message.AddValue(OSCValue.Int(value));
        transmitter.Send(message);
    }
}
