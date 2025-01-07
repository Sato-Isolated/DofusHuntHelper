#include "Keyboard.h"

void setup() {
  // Initialiser le port série
  Serial.begin(9600);
  // Initialiser le contrôle du clavier
  Keyboard.begin();

  // Indiquer que l'Arduino est prêt
  Serial.println("READY");
}

void loop() {
  // Vérifier s'il y a des données série entrantes
  if (Serial.available() > 0) {
    // Lire la donnée entrante
    char incomingByte = Serial.read();

    switch (incomingByte) {
      case '1':
        // Envoyer la touche Entrée
        Keyboard.press(KEY_RETURN);
        delay(100);
        Keyboard.release(KEY_RETURN);
        // Confirmer l'action
        Serial.println("ENTER_SENT");
        break;

      case 'P':
        // Répondre au ping
        Serial.println("PONG");
        break;

      default:
        // Réponse par défaut pour les commandes inconnues
        Serial.println("UNKNOWN_COMMAND");
        break;
    }
  }
}
