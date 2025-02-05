# Dofus Hunt Helper
![image](https://github.com/user-attachments/assets/4becace4-91bc-497f-be4b-32e52cd3586b)

<details>
<summary>🇫🇷 Version Française</summary>

## Description
Dofus Hunt Helper est une application Windows en C# qui automatise les déplacements dans le jeu Dofus lors des chasses au trésor. L'application permet de simplifier l'étape où vous sélectionnez un indice sur un site tel que [DofusDB](https://dofusdb.fr/fr/tools/treasure-hunt) : elle déplace automatiquement la souris vers le chat du jeu, copie la commande `/travel` et appuie deux fois sur la touche Entrée pour exécuter la commande dans le jeu.

Bien que DofusDB soit un exemple courant, l'application fonctionne avec tout site ou outil qui fournit une commande `/travel`.

---

## Fonctionnalités
- Connexion à un Arduino via le port série (optionnel)
- Capture des coordonnées de clic souris
- Automatisation de la saisie des commandes `/travel`
- Gestion multi-écrans
- Fonctionnement sans Arduino en utilisant uniquement des interactions clavier/souris

---

## Prérequis
- Visual Studio
- .NET 8
- [Arduino](https://www.arduino.cc/) compatible (exemple utilisé : Arduino Uno R4 WiFi) si vous souhaitez utiliser la version avec Arduino

---

## Installation
1. Clonez le dépôt :
   ```bash
   git clone https://github.com/sato-isolated/DofusHuntHelper.git
   ```
2. Ouvrez le projet dans Visual Studio.
3. Compilez et exécutez.

---

## Utilisation
1. **Avec Arduino** : Connectez l'Arduino et cliquez sur "Start".
2. **Sans Arduino** : Lancez simplement l'application et utilisez les fonctionnalités avec le clavier et la souris.
3. Capturez les coordonnées avec "Capture".
4. Copiez une commande `/travel` dans le presse-papier depuis un site ou outil de chasse au trésor.
5. L'application déplacera automatiquement la souris vers le chat de Dofus, collera la commande, et effectuera deux pressions sur Entrée pour valider la commande.

---

## Contribution
Les contributions sont les bienvenues. Vous pouvez ouvrir une issue pour signaler un bug ou soumettre une pull request pour proposer des modifications.

---

## Licence
Ce projet est sous licence MIT. Vous pouvez l'utiliser, le modifier et le redistribuer selon les termes de la licence.

---

## Roadmap
- 📚 **Tentative d'automatisation via OCR** : Expérimentation pour extraire les indices de chasse directement depuis l'écran.
- 🛠️ **Utilisation sans Arduino** (terminé) : Ajout d'une option pour fonctionner sans connexion Arduino, en utilisant uniquement des interactions clavier/souris.
- 💻 **Amélioration de l'interface graphique** : Créer une interface plus intuitive et conviviale.
- 🔌 **Gestion automatique du port série** : Détecter et configurer automatiquement le port série utilisé par l'Arduino.

---

## Soutenir le projet
Si vous souhaitez soutenir ce projet, vous pouvez faire un don en kamas sur le serveur **Dakal 6** à **Twisted-Fail**

</details>

<details>
<summary>🇬🇧 English Version</summary>

## Description
Dofus Hunt Helper is a Windows application written in C# that automates movements in the game Dofus during treasure hunts. The application simplifies the process of selecting a clue on a site like [DofusDB](https://dofusdb.fr/en/tools/treasure-hunt) by automatically moving the mouse to the game's chat, pasting the `/travel` command, and pressing Enter twice to execute the command in the game.

Although DofusDB is a common example, the application works with any site or tool that provides a `/travel` command.

---

## Features
- Arduino connection via serial port (optional)
- Mouse click coordinate capture
- Automation of `/travel` command input
- Multi-screen support
- Works without Arduino using keyboard/mouse interactions only

---

## Prerequisites
- Visual Studio
- .NET 8
- Compatible [Arduino](https://www.arduino.cc/) (example used: Arduino Uno R4 WiFi) if using the Arduino version

---

## Installation
1. Clone the repository:
   ```bash
   git clone https://github.com/sato-isolated/DofusHuntHelper.git
   ```
2. Open the project in Visual Studio.
3. Build and run the application.

---

## Usage
1. **With Arduino**: Connect the Arduino and click "Start".
2. **Without Arduino**: Simply launch the application and use the features with the keyboard and mouse.
3. Capture the coordinates with "Capture".
4. Copy a `/travel` command to the clipboard from any treasure hunt site or tool.
5. The application will automatically move the mouse to the Dofus chat, paste the command, and press Enter twice to execute it.

---

## Contribution
Contributions are welcome. Feel free to open an issue to report a bug or submit a pull request to suggest changes.

---

## License
This project is licensed under the MIT License. You are free to use, modify, and distribute it under the terms of the license.

---

## Roadmap
- 📚 **Attempt at OCR-based automation** : Experiment to extract treasure hunt clues directly from the screen.
- 🛠️ **Usage without Arduino** (completed) : Added an option to operate without an Arduino connection, using only keyboard/mouse interactions.
- 💻 **Improving the graphical interface** : Create a more intuitive and user-friendly interface.
- 🔌 **Automatic serial port management** : Detect and automatically configure the serial port used by the Arduino.

## Support the project
If you wish to support this project, you can make a donation in Kamas on the **Dakal 6** server at **Twisted-Fail**.
</details>
