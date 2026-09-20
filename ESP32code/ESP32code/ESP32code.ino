#include <WiFi.h>
#include <WiFiProvisioner.h>
#include <AsyncTCP.h>
#include <ESPAsyncWebServer.h>
#include <esp32cam.h>
#include <HTTPClient.h>
#include <PubSubClient.h>
#include <esp_task_wdt.h>
#include <ESP32Servo.h>

#define CAMERA_MODEL_AI_THINKER
Servo EntryGateServo;
Servo ExitGateServo;
WiFiProvisioner provisioner;
WiFiClient espClient;
PubSubClient mqttClient(espClient);
#define IRGateSensor 12
#define IRExitGateSensor 13
bool wifiConnected = false;

unsigned long lastSent = 0;
const unsigned long interval = 5000;

esp32cam::Resolution initialResolution;
const char* mqttUsername = "admin";
const char* mqttPassword = "admin123";
const int mqttPort = 1883;
const int port = 7209;
const char* uri = "/api/scan-qr";
char server[64] = "192.168.100.7";
char mqttServer[64] = "192.168.100.7";

// Function declarations
void mqttCallback(char* topic, byte* payload, unsigned int length);
void mqttReconnect();
void handleMqttLoop(void* parameter);
void handleCameraTask(void* parameter);

bool initCamera() 
{
  using namespace esp32cam;
{
   // Set correct pins for AI Thinker module
  const esp32cam::Pins cameraPins = esp32cam::pins::AiThinker;
 initialResolution = Resolution::find(1024, 768);
  // Configure camera
  esp32cam::Config cfg;
  cfg.setPins(cameraPins);
  //cfg.setBufferCount(1);
  cfg.setResolution(initialResolution);
  cfg.setBufferCount(8);
  cfg.setJpeg(96);
  int retryCount = 5;
while (!esp32cam::Camera.begin(cfg)) {
  Serial.println("Camera init failed");
  delay(1000);
  retryCount--;
  if (retryCount <= 0) {
    Serial.println("Camera init failed after multiple attempts. Restarting...");
    ESP.restart();
  }
}

  Serial.println("Camera ready");
  return true;
}
}

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

// MQTT callback when message is received
void mqttCallback(char* topic, byte* payload, unsigned int length) {
  Serial.print("Message received on topic: ");
  Serial.println(topic);
  Serial.print("Message: ");
  String message;
  for (int i = 0; i < length; i++) {
    message += (char)payload[i];
  }
  Serial.println(message);
   if (String(topic) == "Gate" && message == "EntryGateOpen") {
    openEntryGate();
    Serial.println("Entry Gate Opened");
    delay(5000);
    closeEntryGate();
  }
  if (String(topic) == "Gate" && message == "ExitGateOpen") {
    openExitGate();
    Serial.println("Exit Gate Opened");
    delay(5000);
    closeExitGate();
  }

  // TODO: Parse message and perform action
}

// MQTT connection management
// MQTT connection management
void mqttReconnect() {
  while (!mqttClient.connected()) {
    Serial.print("Connecting to MQTT...");
    // Use credentials here
    if (mqttClient.connect("ESP32Client", mqttUsername, mqttPassword)) {
      Serial.println("connected");
      mqttClient.subscribe("CarEntry");
      mqttClient.subscribe("CarExit");
      mqttClient.subscribe("Gate");
      mqttClient.subscribe("Sensors");
    } else {
      Serial.print("Failed. State=");
      Serial.println(mqttClient.state());
      delay(2000);
    }
  }
}

// Core 0: MQTT Loop Task
void handleMqttLoop(void* parameter) {
  mqttClient.setServer(mqttServer, mqttPort);
  mqttClient.setCallback(mqttCallback);

  for (;;) {
    if (!mqttClient.connected()) {
      mqttReconnect();
    }
    mqttClient.loop();
    vTaskDelay(10);  // Avoid watchdog trigger
  }
}

// Core 1: Image Sending Task
void handleCameraTask(void* parameter) {
  for (;;) {
    int entryGateSensorRead = digitalRead(IRGateSensor);
    if (wifiConnected && entryGateSensorRead == LOW) 
    {
      sendImage();
      // Example MQTT publish
      mqttClient.publish("CarEntry", "Image Captured and Sent");
      delay(5000);
    }
    vTaskDelay(10);
  }
}

void setup() {
  Serial.begin(115200);

  EntryGateServo.setPeriodHertz(50);    // standard 50 hz servo
	EntryGateServo.attach(15, 500, 2400); // attaches the servo on pin 18 to the servo object

  ExitGateServo.setPeriodHertz(50);    // standard 50 hz servo
	ExitGateServo.attach(14, 500, 2400); // attaches the servo on pin 18 to the servo object
  
  initCamera();
  // Remove the current task from watchdog monitoring
  pinMode(IRGateSensor,INPUT);
  provisioner.getConfig().AP_NAME = "SmartParkingEntryGate";
  provisioner.getConfig().SHOW_INPUT_FIELD = true;
  provisioner.getConfig().SHOW_RESET_FIELD = false;
  provisioner.getConfig().INPUT_LENGTH = 15;
  provisioner.getConfig().INPUT_TEXT ="Server Address";

  provisioner.onSuccess([](const char *ssid, const char *password, const char *input) {
    Serial.printf("Connected to SSID: %s\n", ssid);
    if (input) {
      strncpy(server, input, sizeof(server));
      strncpy(mqttServer, input, sizeof(mqttServer));
    }
    Serial.print("IP Address: ");
    Serial.println(WiFi.localIP());
    wifiConnected = true;
  });

  provisioner.startProvisioning();

  

  // Create FreeRTOS tasks pinned to cores
  xTaskCreatePinnedToCore(handleMqttLoop, "MQTT Task", 4096 , NULL, 1, NULL, 0);   // Core 0
  xTaskCreatePinnedToCore(handleCameraTask, "Camera Task", 4096, NULL, 1, NULL, 1); // Core 1
}
void closeEntryGate()
 {
  EntryGateServo.write(0);    // Close position
}

void openEntryGate() {
  EntryGateServo.write(90);   // Open position (adjust if needed)               // Wait 5 seconds

}


void closeExitGate()
 {
  ExitGateServo.write(0);    // Close position
}

void openExitGate() {
  ExitGateServo.write(90);   // Open position (adjust if needed)               // Wait 5 seconds

}

void loop() {
  // Empty loop. All tasks handled by FreeRTOS.
  
   delay(10);
}
