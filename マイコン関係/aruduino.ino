#include <Wire.h>
#include <Adafruit_PWMServoDriver.h>
#include <WiFi.h>
#include <WiFiUdp.h>
#include <OSCMessage.h>

Adafruit_PWMServoDriver pwm = Adafruit_PWMServoDriver();
WiFiUDP Udp;

#define SERVOMIN  102
#define SERVO_90  300

// WiFi settings
const char* WIFI_SSID = "ivrc";
const char* WIFI_PASS = "ivrc1234";
const int LOCAL_PORT = 9000; // UnityのOSC送信先ポートに合わせる

// OSC address
const char* OSC_ADDR_TEST = "/test/value";

const bool invertServo[6] = {false, false, true, true, true, false};

int lastPulses[6] = {0, 0, 0, 0, 0, 0};
int lastValue = -1;

void setup() {
  Serial.begin(115200);

  // WiFi connect
  WiFi.mode(WIFI_STA);
  WiFi.begin(WIFI_SSID, WIFI_PASS);
  Serial.print("Connecting WiFi");
  const unsigned long connectTimeoutMs = 15000;
  unsigned long startMs = millis();
  while (WiFi.status() != WL_CONNECTED) {
    delay(300);
    Serial.print(".");
    if (millis() - startMs > connectTimeoutMs) {
      Serial.println();
      Serial.print("WiFi connect failed. Status=");
      Serial.println(WiFi.status());
      break;
    }
  }
  if (WiFi.status() == WL_CONNECTED) {
    Serial.println();
    Serial.println("WiFi connected");
    Serial.print("IP: ");
    Serial.println(WiFi.localIP());
  }

  // UDP start
  Udp.begin(LOCAL_PORT);
  Serial.print("OSC UDP Port: ");
  Serial.println(LOCAL_PORT);

  // I2C init
  Wire.begin();
  Wire.setClock(400000);

  pwm.begin();
  pwm.setOscillatorFrequency(27000000);
  pwm.setPWMFreq(50);

  Serial.println("OSC Receiver Ready");
}

void loop() {
  int packetSize = Udp.parsePacket();
  if (packetSize <= 0) return;

  OSCMessage msg;
  while (packetSize--) {
    msg.fill(Udp.read());
  }

  if (!msg.hasError()) {
    msg.dispatch(OSC_ADDR_TEST, onTestValue);
  } else {
    OSCErrorCode error = msg.getError();
    Serial.print("OSC Error: ");
    Serial.println(error);
  }
}

void onTestValue(OSCMessage &msg) {
  if (msg.size() == 0) return;

  int value = msg.getInt(0);
  value = constrain(value, 0, 100);

  if (value == lastValue) return;
  lastValue = value;

  for (int i = 0; i < 6; i++) {
    int pulse = invertServo[i]
      ? map(value, 0, 100, SERVO_90, SERVOMIN)
      : map(value, 0, 100, SERVOMIN, SERVO_90);

    if (abs(pulse - lastPulses[i]) > 2) {
      pwm.setPWM(i, 0, pulse);
      lastPulses[i] = pulse;
    }
  }
}