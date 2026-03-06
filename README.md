# Localization Tools for Unity

A powerful, open-source Editor extension for Unity that automates and streamlines the localization process. It integrates seamlessly with Unity's native Localization package, providing bulk translation and Text-to-Speech (TTS) generation directly within the Editor using external AI APIs.

---

## ⚠️ Disclaimer & API Costs
**Please read the [LICENSE](LICENSE.md) file carefully.**
This tool integrates with third-party APIs (ElevenLabs, Google Gemini, Z.ai). You are solely responsible for compliance with their Terms of Service and for any financial costs (credits/tokens usage) incurred through this tool. The author(s) takes no responsibility for excessive API usage due to bugs, unintended behavior, or user error. 

**Use at your own risk and monitor your API dashboards regularly.**

---

## 📦 Installation via Unity Package Manager (Recommended)

You can install this package directly into your Unity project using the Unity Package Manager (UPM).

1. Open your Unity project.
2. Go to **Window -> Package Manager**.
3. Click the **+** (plus) icon in the top-left corner and select **"Add package from git URL..."**.
4. Paste the following URL:
   `https://github.com/donsledzion/unity-elevenlabs-integration.git?path=Assets/LocalizationTools`
5. Click **Add**. Unity will download the package and automatically resolve required dependencies (it will install the Unity Localization package if you do not have it).

> **Note on Prerequisites:** This tool is built on top of Unity's official Localization system. If you install via the Git URL above, UPM will handle downloading the `com.unity.localization` dependency for you automatically.

---

## 🛠️ Setup & Configuration

Before using the tools, you must configure your API providers.

1. In Unity, go to **Edit -> Project Settings -> AI Localization**.
2. Provide your API keys and configuration details for the AI providers you intend to use (ElevenLabs, Gemini, Z.ai).

---

## 🚀 Features

The package provides two main utilities accessible via the top menu bar under **Tools -> Localization**.

### 1. Multitranslator
Automates the translation of text entries across all locales defined in your project.

*   **How it works:**
    *   Select your target String Table (the one containing your base language texts).
    *   The tool reads the entries and uses the configured translation provider (Gemini or Z.ai) to translate the values.
    *   It automatically populates the translations into all other available locales within your Unity project.

### 2. Multi-TTS Converter
Generates voiceovers (audio files) for your localized texts using ElevenLabs and automatically links them within Unity's Localization system.

*   **How it works:**
    *   **Source Table:** Select the String Table containing the texts you want to voice.
    *   **Target Table:** Select the Asset Table where the generated AudioClips should be referenced.
    *   **Output Directory:** Specify the folder within your project where the generated `.wav` files will be saved.
*   **Voice Selection:**
    *   After fetching the connection to ElevenLabs, the tool allows you to assign a specific, dedicated voice for each of your project's languages.
    *   *Note on Free Accounts:* If you are using a free tier of ElevenLabs, the API only permits the use of their official, built-in voices (even if custom voices are available to you in their web dashboard).
*   **Automatic Linking:** 
    Upon successful TTS conversion:
    1.  Audio files are saved to the target directory. The filenames perfectly match the entry keys from your Source String Table.
    2.  New entries are automatically created in your Target Asset Table with identical keys.
    3.  The newly generated AudioClips are automatically assigned as references to those Asset Table entries, making them immediately ready to use in your UI/Audio systems.

---

## 📄 License
This project is licensed under the MIT License. See the [LICENSE](LICENSE.md) file for more details.
