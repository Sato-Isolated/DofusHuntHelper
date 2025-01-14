#include "Keyboard.h"

void setup() {
  Serial.begin(9600);
  Keyboard.begin();
  Serial.println("READY");
}

void loop() {
  // Si on veut lire un "mot" complet, on pourrait accumuler
  // les caractères jusqu'à '\n'. Pour l'instant, on reste
  // sur un caractère unique.
  if (Serial.available() > 0) {
    char incomingByte = Serial.read();
    handleIncomingChar(incomingByte);
  }
}

void handleIncomingChar(char c) {
  switch (c) {
    case '1':
      sendEnter();
      break;

    case 'P':
      Serial.println("PONG");
      break;

    default:
      Serial.println("UNKNOWN_COMMAND");
      break;
  }
}

void sendEnter() {
  Keyboard.press(KEY_RETURN);
  delay(100);
  Keyboard.release(KEY_RETURN);
  Serial.println("ENTER_SENT");
}
