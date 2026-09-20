#include <Wire.h>
#include <Adafruit_GFX.h>
#include <Adafruit_SSD1306.h>
#include <Servo.h>

#define OLED_RESET -1
Adafruit_SSD1306 display(128, 64, &Wire, OLED_RESET);

Servo EntryGateServo;
Servo ExitGateServo;

#define entryGateSensor  2
#define exitGateSensor  3
bool entryGateOpen = false;
bool exitGateOpen = false;
// Helper to center two lines of text
void drawCenteredText(const String &line1, const String &line2 = "", int textSize = 1, int delayTime = 1000) {
  display.clearDisplay();
  display.setTextSize(textSize);
  display.setTextColor(WHITE);

  int16_t x1, y1;
  uint16_t w, h;

  if (line1 != "") {
    display.getTextBounds(line1, 0, 0, &x1, &y1, &w, &h);
    display.setCursor((128 - w) / 2, 10);
    display.println(line1);
  }

  if (line2 != "") {
    display.getTextBounds(line2, 0, 0, &x1, &y1, &w, &h);
    display.setCursor((128 - w) / 2, 30);
    display.println(line2);
  }

  display.display();
  delay(delayTime);
}

// Final countdown screen
void showCountdown() {
  drawCenteredText("Starting", "System...", 1, 600);

  for (int i = 3; i >= 1; i--) {
    display.clearDisplay();
    display.setTextSize(2);
    display.setTextColor(WHITE);

    String countText = String(i);
    int16_t x1, y1;
    uint16_t w, h;

    display.getTextBounds(countText, 0, 0, &x1, &y1, &w, &h);
    display.setCursor((128 - w) / 2, (64 - h) / 2);
    display.println(countText);
    display.display();
    delay(500);
  }

  drawCenteredText("System", "Started!", 1, 1000);
}

void startupScreen() {
  drawCenteredText("Smart Parking", "System", 1, 1000);
  drawCenteredText("Hazara University", "Mansehra", 1, 1000);

  drawCenteredText("Syeda Aleesha", "Batool", 1, 800);
  drawCenteredText("Roll: 302-11074", "BSSE VII B", 1, 600);
  drawCenteredText("Semester:", "Fall 2024", 1, 600);

  drawCenteredText("Laiba", "", 1, 800);
  drawCenteredText("Roll: 302-211059", "BSSE VII B", 1, 600);
  drawCenteredText("Semester:", "Fall 2024", 1, 600);

  drawCenteredText("Roobina Khan", "", 1, 800);
  drawCenteredText("Roll: 302-211060", "BSSE VII B", 1, 600);
  drawCenteredText("Semester:", "Fall 2024", 1, 600);

  showCountdown(); // Final system start countdown
}

void setup() {
  Serial.begin(9600);
  pinMode(entryGateSensor,INPUT);
  pinMode(exitGateSensor,INPUT);
  EntryGateServo.attach(A1);
  ExitGateServo.attach(A2);
  if (!display.begin(SSD1306_SWITCHCAPVCC, 0x3C)) {
    Serial.println(F("SSD1306 allocation failed"));
    for (;;);
  }

  display.clearDisplay();
  //startupScreen(); // Show only once
  closeEntryGate();
  delay(100);
  closeExitGate();
}

void loop() {
  int entryGateSensorRead = digitalRead(entryGateSensor);
  if(entryGateSensorRead == LOW)
  {
    entryGateOpen = true;
    openEntryGate();
  }
  else
  {
    if(entryGateOpen == true)
    {
     delay(5000);
     entryGateOpen = false;
     closeEntryGate();
    }
  }

   int exitGateSensorRead = digitalRead(exitGateSensor);
  Serial.println(exitGateSensorRead);
  if(exitGateSensorRead == LOW)
  {
    if(exitGateOpen == false)
    openExitGate();
    exitGateOpen = true;
    
  }
  else
  {
    if(exitGateOpen == true)
    {
     delay(5000);
     exitGateOpen = false;
     closeExitGate();
    }
  }
  //openEntryGate();
  //openExitGate();
}

void closeEntryGate()
 {
  EntryGateServo.write(0);    // Close position
}

void openEntryGate() {
  EntryGateServo.write(90);   // Open position (adjust if needed)               // Wait 5 seconds

}

void closeExitGate() {
  ExitGateServo.write(0);     // Close position

}
// Method to open and close the exit gate
void openExitGate() {
  ExitGateServo.write(100);    // Open position (adjust if needed)
}
