#include <WiFi.h>
#include <esp32cam.h>
#include <PubSubClient.h>
#include <ESP32Servo.h>

#include <HTTPClient.h>
#include <AsyncTCP.h>
#include <ESPAsyncWebServer.h>
//#include "esp_bt.h"

const int port = 7209;
char server[64] = "192.168.100.7";
// ------ Config ------
const char* ssid = "DigiSync Technologies";
const char* password = "ultimate";
const char* mqttServer = "192.168.100.7";
const int mqttPort = 1883;
const char* mqttUser = "admin";
const char* mqttPass = "admin123";
const char* postUri = "/api/scan-qrexit";
const int postPort = 7209;
const char* uri = "/api/scan-qrexit";

#define BikeParking1 2
#define BikeParking2 14
#define BikeParking3 13


int lastBikeParking1Value = 1;
int lastBikeParking2Value = 1;
int lastBikeParking3Value = 1;

// ------ Pins ------
#define IR_EXIT_SENSOR 12
#define SERVO_PIN 15

// ------ Globals ------
WiFiClient espClient;
PubSubClient mqttClient(espClient);
Servo ExitGateServo;

bool wifiConnected = false;
esp32cam::Resolution initialResolution;

// ------ Camera Init ------
bool initCamera() {
  Serial.println("📷 Initializing camera...");
  using namespace esp32cam;
  Config cfg;
  cfg.setPins(pins::AiThinker);
  cfg.setResolution(Resolution::find(1024, 768));
  cfg.setBufferCount(2);
  cfg.setJpeg(80);

  bool ok = Camera.begin(cfg);
  if (ok) {
    Serial.println("✅ Camera initialized successfully.");
  } else {
    Serial.println("❌ Camera initialization failed.");
  }
  return ok;
}

// ------ Send Image ------
void sendImage() {
  if (!WiFi.isConnected()) return;

  auto frame = esp32cam::capture();
  if (!frame) {
    Serial.println("Failed to capture image");
    return;
  }

  Serial.print("Captured image size: ");
  Serial.println(frame->size());

  WiFiClient client;

  String boundary = "----WebKitFormBoundary7MA4YWxkTrZu0gW";
  String contentType = "multipart/form-data; boundary=" + boundary;

  String bodyStart = "--" + boundary + "\r\n";
  bodyStart += "Content-Disposition: form-data; name=\"image\"; filename=\"image.jpg\"\r\n";
  bodyStart += "Content-Type: image/jpeg\r\n\r\n";

  String bodyEnd = "\r\n--" + boundary + "--\r\n";

  int contentLength = bodyStart.length() + frame->size() + bodyEnd.length();

  Serial.println("Connecting to server...");

  if (!client.connect(server, port)) {
    Serial.println("Connection to server failed");
    return;
  }

  // Send HTTP headers
  client.print(String("POST ") + uri + " HTTP/1.1\r\n");
  client.print(String("Host: ") + server + "\r\n");
  client.print("Content-Type: " + contentType + "\r\n");
  client.print("Content-Length: " + String(contentLength) + "\r\n");
  client.print("Connection: close\r\n\r\n");

  // Send body
  client.print(bodyStart);
  client.write(frame->data(), frame->size());
  delay(10);  // Allow buffer flush
  client.print(bodyEnd);

  // Read response
  while (client.connected()) {
    while (client.available()) {
      String line = client.readStringUntil('\n');
      Serial.println(line);
    }
  }

  client.stop();
  Serial.println("Image sent and connection closed.");
}
// ------ MQTT ------
void mqttCallback(char* topic, byte* payload, unsigned int length) {
  Serial.print("📥 MQTT Message Received [");
  Serial.print(topic);
  Serial.print("]: ");
  String msg;
  for (int i = 0; i < length; ++i) {
    msg += (char)payload[i];
  }
  Serial.println(msg);

  if (String(topic) == "Gate" && msg == "ExitGateOpen") {
    Serial.println("🔓 Exit gate command received. Opening...");
    ExitGateServo.write(90);
    delay(5000);
    ExitGateServo.write(0);
    Serial.println("🔐 Gate closed.");
  }
}

void reconnectMQTT() {
  Serial.println("🔌 Reconnecting to MQTT broker...");
  while (!mqttClient.connected()) {
    Serial.print("🔁 Attempting MQTT connection...");
    if (mqttClient.connect("ESP32Client", mqttUser, mqttPass)) {
      Serial.println("✅ MQTT connected.");
      mqttClient.subscribe("Gate");
      Serial.println("📡 Subscribed to topic: Gate");
     publishInitialSensorStatus();  // Add this
    } else {
      Serial.print("❌ MQTT connection failed, rc=");
      Serial.print(mqttClient.state());
      Serial.println(" -> retrying in 1 sec...");
      delay(1000);
    }
  }
}

// ------ Tasks ------
void IRMonitorTask(void* param) {
  Serial.println("📌 IRMonitorTask started.");
  pinMode(IR_EXIT_SENSOR, INPUT);
  while (true) {
    int irValue = digitalRead(IR_EXIT_SENSOR);
    if (wifiConnected && irValue == LOW) {
      Serial.println("🚗 Vehicle detected at exit. Sending image...");
      sendImage();
      mqttClient.publish("CarExit", "Image Sent");
      Serial.println("📨 MQTT message published: CarExit -> Image Sent");
      delay(5000);
    }
    vTaskDelay(100 / portTICK_PERIOD_MS);
  }
}

void mqttTask(void* param) {
  Serial.println("📌 mqttTask started.");
  mqttClient.setServer(mqttServer, mqttPort);
  mqttClient.setCallback(mqttCallback);
  while (true) {
    if (!mqttClient.connected()) reconnectMQTT();
    mqttClient.loop();
    vTaskDelay(10 / portTICK_PERIOD_MS);
  }
}

void publishSensorStatus(const char* parkingName, int pinValue) {
  bool status = pinValue == 0;  // 0 = true, 1 = false
  String payload = "{";
  payload += "\"parkingName\":\"" + String(parkingName) + "\",";
  payload += "\"status\":" + String(status ? "true" : "false");
  payload += "}";

  mqttClient.publish("Sensors", payload.c_str());
  Serial.println("Published: " + payload);
}
void checkAndPublishSensorUpdates() {
  int current;


  current = digitalRead(BikeParking1);
  if (current != lastBikeParking1Value) {
    lastBikeParking1Value = current;
    publishSensorStatus("BikeParking1", current);
  }

  current = digitalRead(BikeParking2);
  if (current != lastBikeParking2Value) {
    lastBikeParking2Value = current;
    publishSensorStatus("BikeParking2", current);
  }

  current = digitalRead(BikeParking3);
  if (current != lastBikeParking3Value) {
    lastBikeParking3Value = current;
    publishSensorStatus("BikeParking3", current);
  }
}

void publishInitialSensorStatus() {

  publishSensorStatus("BikeParking1", digitalRead(BikeParking1));
  publishSensorStatus("BikeParking2", digitalRead(BikeParking2));
  publishSensorStatus("BikeParking3", digitalRead(BikeParking3));


  lastBikeParking1Value = digitalRead(BikeParking1);
  lastBikeParking2Value = digitalRead(BikeParking2);
  lastBikeParking3Value = digitalRead(BikeParking3);
}

// ------ Setup ------
void setup() {
  Serial.begin(115200);
  Serial.println("🚀 Booting ESP32-CAM...");


   // PSRAM check
  if (psramFound()) {
    Serial.println("✅ PSRAM is available");
    Serial.print("Free PSRAM: ");
    Serial.println(ESP.getFreePsram());
  } else {
    Serial.println("❌ PSRAM NOT available");
  }



  pinMode(BikeParking1,INPUT);
  pinMode(BikeParking2,INPUT);
  pinMode(BikeParking3,INPUT);

 
  lastBikeParking1Value = digitalRead(BikeParking1);
  lastBikeParking2Value = digitalRead(BikeParking2);
  lastBikeParking3Value = digitalRead(BikeParking3);
  
  // WiFi
  Serial.printf("📶 Connecting to WiFi: %s\n", ssid);
  WiFi.begin(ssid, password);
  for (int i = 0; i < 20 && WiFi.status() != WL_CONNECTED; i++) {
    delay(500);
    Serial.print(".");
  }
  wifiConnected = WiFi.status() == WL_CONNECTED;
  if (wifiConnected) {
    Serial.println("\n✅ WiFi connected!");
    Serial.print("🌐 IP Address: ");
    Serial.println(WiFi.localIP());
  } else {
    Serial.println("\n❌ WiFi connection failed!");
  }

  // Servo
  Serial.println("🔧 Setting up servo...");
  ExitGateServo.setPeriodHertz(50);    // standard 50 hz servo
	ExitGateServo.attach(15, 500, 2400); // attaches the servo on pin 18 to the servo object
  Serial.println("🔐 Gate set to CLOSED position.");

  // Camera
  if (!initCamera()) {
    Serial.println("⚠️ Restarting due to camera init failure...");
    ESP.restart();
  }

  // Start Tasks
  Serial.println("🧵 Starting FreeRTOS tasks...");
  xTaskCreatePinnedToCore(IRMonitorTask, "IR Task", 4096, NULL, 1, NULL, 1);
  xTaskCreatePinnedToCore(mqttTask, "MQTT Task", 4096, NULL, 1, NULL, 0);
  Serial.println("✅ Setup complete.");
}

void loop() {
  delay(1000); // Idle loop
  // Empty loop. All tasks handled by FreeRTOS.
  if (wifiConnected && mqttClient.connected()) {
    checkAndPublishSensorUpdates();
  }
   delay(10);
}
