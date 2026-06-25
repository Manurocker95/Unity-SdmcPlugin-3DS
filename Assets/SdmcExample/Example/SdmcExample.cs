using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class SdmcExample : MonoBehaviour
{
    string lastStatus = "";
    string sequenceProgress = "";
    bool sequenceRunning;

    [SerializeField]
    private Texture2D ImageToSave;

    [SerializeField]
    private RawImage UIRawImage;

    private List<GameData> allGames = new List<GameData>();

    private GameData currentGame;

    private int nextSaveGameId = 1;

    private IEnumerator RunTestSequence()
    {
        if (sequenceRunning) { yield break; }
        sequenceRunning = true;

        var wait = new WaitForSeconds(0.25f);
        var longWait = new WaitForSeconds(2);
        var testPaths = new string[]
        {
                "Hello.txt",
                "Test/Saves/Hello2.txt",
                "Saves/Files/ImageTest.bin",
        };

        sequenceProgress += "Mounting sdmc: ";
        {
            var result = SdmcPlugin.SdmcMount();
            if (result == SdmcPlugin.SdmcResult.SDMC_SUCCESS)
            {
                sequenceProgress += "Ok\n";
            }
            else
            {
                sequenceProgress += "Fail: " + SdmcPlugin.GetErrorString(result) + "\n";
            }
            yield return wait;
        }

        sequenceProgress += "--- Simple file path ---\n";
        {
            var testPath = testPaths[0];
            var testString = "Hello World!";

            sequenceProgress += "Saving file: ";
            try
            {
                SdmcPlugin.WriteFile(testPath, Encoding.UTF8.GetBytes(testString));
                sequenceProgress += "Ok\n";
                sequenceProgress += "File saved to : " + Path.Combine(SdmcPlugin.BasePath, testPath) + "\n";
            }
            catch (Exception ex)
            {
                sequenceProgress += "Fail: " + ex + "\n";
            }
            yield return wait;

            sequenceProgress += "Checking file: ";
            try
            {
                var result = SdmcPlugin.FileExists(testPath);
                if (result == SdmcPlugin.SdmcResult.SDMC_SUCCESS)
                {
                    sequenceProgress += "Ok\n";
                }
                else
                {
                    sequenceProgress += "Fail: File does not exist\n";
                }
            }
            catch (Exception ex)
            {
                sequenceProgress += "Fail: " + ex + "\n";
            }
            yield return wait;

            sequenceProgress += "Reading file: ";
            try
            {
                var bytes = SdmcPlugin.ReadFile(testPath);
                string text = Encoding.UTF8.GetString(bytes);
                if (text == testString)
                {
                    sequenceProgress += "Ok\n";
                }
                else
                {
                    sequenceProgress += "Fail: '" + text + "' is not '" + testString + "'\n";
                }
            }
            catch (Exception ex)
            {
                sequenceProgress += "Fail: " + ex + "\n";
            }
            yield return wait;
        }

        yield return longWait;
        sequenceProgress = "";

        sequenceProgress += "--- Deep file path ---\n";
        {
            var testPath = testPaths[1];
            var testString = "Hello World 2!";

            sequenceProgress += "Saving file: ";
            try
            {
                SdmcPlugin.WriteFile(testPath, Encoding.UTF8.GetBytes(testString));
                sequenceProgress += "Ok\n";
                sequenceProgress += "File saved to : " + Path.Combine(SdmcPlugin.BasePath, testPath) + "\n";
            }
            catch (Exception ex)
            {
                sequenceProgress += "Fail: " + ex + "\n";
            }
            yield return wait;

            sequenceProgress += "Checking file: ";
            try
            {
                var result = SdmcPlugin.FileExists(testPath);
                if (result == SdmcPlugin.SdmcResult.SDMC_SUCCESS)
                {
                    sequenceProgress += "Ok\n";
                }
                else
                {
                    sequenceProgress += "Fail: File does not exist\n";
                }
            }
            catch (Exception ex)
            {
                sequenceProgress += "Fail: " + ex + "\n";
            }
            yield return wait;

            sequenceProgress += "Reading file: ";
            try
            {
                var bytes = SdmcPlugin.ReadFile(testPath);
                string text = Encoding.UTF8.GetString(bytes);
                if (text == testString)
                {
                    sequenceProgress += "Ok\n";
                }
                else
                {
                    sequenceProgress += "Fail: '" + text + "' is not '" + testString + "'\n";
                }
            }
            catch (Exception ex)
            {
                sequenceProgress += "Fail: " + ex + "\n";
            }
            yield return wait;
        }

        yield return longWait;
        sequenceProgress = "";

        sequenceProgress += "--- Texture ---\n";
        {
            var testPath = testPaths[2];
            UIRawImage.gameObject.SetActive(true);

            sequenceProgress += "Saving Image: ";
            try
            {
                var imageData = ImageToSave.GetRawTextureData();
                SdmcPlugin.WriteFile(testPath, imageData);
                sequenceProgress += "Ok\n";
                sequenceProgress += "Texture saved to : " + Path.Combine(SdmcPlugin.BasePath, testPath) + "\n";
            }
            catch (Exception ex)
            {
                sequenceProgress += "Fail: " + ex + "\n";
            }
            yield return wait;

            sequenceProgress += "Loading Image: ";
            try
            {
                var tex = new Texture2D(512, 512, TextureFormat.ETC_RGB4_3DS, false);
                var data = SdmcPlugin.ReadFile(testPath);
                tex.LoadRawTextureData(data);
                tex.Apply();
                UIRawImage.texture = tex;
                sequenceProgress += "Ok\n";
            }
            catch (Exception ex)
            {
                sequenceProgress += "Fail: " + ex + "\n";
            }
            yield return wait;
        }

        yield return longWait;

        sequenceProgress = "--- Hold Left Bumper or Q (Citra) to delete test files. Test ends in 5s ---\n";
        float countdown = 5;
        while (countdown > 0 && !UnityEngine.N3DS.GamePad.GetButtonHold(N3dsButton.L) && !UnityEngine.N3DS.GamePad.GetButtonHold(N3dsButton.R))
        {
            countdown -= Time.deltaTime;
            yield return 0;
        }

        if (countdown <= 0)
        {
            Debug.Log("Timed out");
            UIRawImage.gameObject.SetActive(false);
            Destroy(UIRawImage.texture);
            sequenceProgress = "";
            sequenceRunning = false;
            yield break;
        }

        Debug.Log("Interrupt");

        sequenceProgress = "--- Deleting ---\n";
        {
            yield return wait;
            foreach (var testPath in testPaths)
            {
                sequenceProgress += "Deleting file: " + Path.Combine(SdmcPlugin.BasePath, testPath);
                {
                    var result = SdmcPlugin.DeleteFile(testPath);
                    if (result == SdmcPlugin.SdmcResult.SDMC_SUCCESS)
                    {
                        sequenceProgress += " Ok\n";
                    }
                    else
                    {
                        sequenceProgress += " Fail: " + SdmcPlugin.GetErrorString(result) + "\n";
                    }
                    yield return wait;
                }

                sequenceProgress += "Deleting directory: " + SdmcPlugin.BasePath;
                {
                    var result = SdmcPlugin.DeleteDirectory("");
                    if (result == SdmcPlugin.SdmcResult.SDMC_SUCCESS)
                    {
                        sequenceProgress += " Ok\n";
                    }
                    else
                    {
                        sequenceProgress += " Fail: " + SdmcPlugin.GetErrorString(result) + "\n";
                    }
                    yield return wait;
                }
            }
        }

        yield return new WaitForSeconds(5);
        UIRawImage.gameObject.SetActive(false);
        Destroy(UIRawImage.texture);
        sequenceProgress = "";
        sequenceRunning = false;
    }

    [GUITarget(1)]
    void OnGUI()
    {
        int width = 320;
        int height = 240;

        int rectWidth = 60;
        float rectStep = (float)320 / 5;

        Rect rect = new Rect(0, 0, rectWidth, 25);

        if (!sequenceRunning)
        {
            // Top row.
            GUI.Label(new Rect(0, 0, Screen.width, 50), "Menu: " + lastStatus);
            rect.y += 50;

            try
            {
                if (GUI.Button(rect, "Mount"))
                {
                    Debug.Log(SdmcPlugin.SdmcMount());
                }
                rect.x += rectStep;

                if (GUI.Button(rect, "Unmount"))
                {
                    Debug.Log(SdmcPlugin.SdmcUnmount());
                }
                rect.x += rectStep;

                if (GUI.Button(rect, "Save"))
                {
                    SdmcPlugin.WriteObject("gamesSDMC.bin", allGames);
                }
                rect.x += rectStep;

                if (GUI.Button(rect, "Load"))
                {
                    allGames = SdmcPlugin.ReadObject<List<GameData>>("gamesSDMC.bin");
                }
                rect.x += rectStep;

                if (GUI.Button(rect, "Sequence"))
                {
                    StartCoroutine(RunTestSequence());
                }

                rect.x = 0;
                rect.y += 30;

                if (GUI.Button(rect, "Create"))
                {
                    string saveName = "game" + nextSaveGameId++;
                    GameData gameData = new GameData(saveName);
                    gameData.goldCoins = (int)(UnityEngine.Random.value * 1000.0f);
                    allGames.Add(gameData);
                }
            }
            catch (System.Exception ex)
            {
                lastStatus = ex.Message;
            }

            // List of games.
            rect.x = 5;
            rect.y += 30;
            rect.width = 100;
            GUI.Label(rect, "Games:");
            rect.y += 20;
            rect.width = 65;

            foreach (GameData gameData in allGames)
            {
                if (GUI.Button(rect, gameData.name))
                    currentGame = gameData;

                rect.x += 70;
            }

            // Current game details.
            if (currentGame != null)
            {
                rect.x = 5;
                rect.y += 30;
                rect.width = 100;
                GUI.Label(rect, "Current Game:");
                rect.y += 20;

                rect.width = 65;
                if (GUI.Button(rect, "Delete"))
                {
                    allGames.Remove(currentGame);
                    currentGame = null;
                }
                rect.x += 70;

                rect.width = 120;
                if (currentGame != null)
                    GUI.Label(rect, "Gold Coins: " + currentGame.goldCoins);
            }
        }
        else
        {
            GUI.Label(new Rect(0, 0, Screen.width, Screen.height), sequenceProgress);
        }
    }
}
