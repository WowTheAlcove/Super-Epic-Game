using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;

public class FileDataHandler
{
    private string dataDirPath = "";
    private string manualSavedDataFileName = "";
    private string autoSavedDataFileName = "";


    public FileDataHandler(string dataDirPath, string manualSaveFileName, string autoSaveFileName) {
        this.dataDirPath = dataDirPath;
        this.manualSavedDataFileName = manualSaveFileName;
        this.autoSavedDataFileName = autoSaveFileName;
    }

    public GameData Load(SaveFileType saveFileType) {
        string fullPathToSaveTo;
        switch (saveFileType) {
            case SaveFileType.Manual:
                fullPathToSaveTo = Path.Combine(dataDirPath, manualSavedDataFileName);
                break;
            case SaveFileType.Auto:
                fullPathToSaveTo = Path.Combine(dataDirPath, autoSavedDataFileName);
                break;
            default:
                Debug.LogError("SaveFileType is None or invalid, cannot save data.");
                return null;
        }
        GameData loadedData = null;
        if (File.Exists(fullPathToSaveTo)) {
            try {
                //loading in the serialized data from the file
                string dataToLoad = "";
                using (FileStream stream = new FileStream(fullPathToSaveTo, FileMode.Open)) {
                    using (StreamReader reader = new StreamReader(stream)) {
                        dataToLoad = reader.ReadToEnd();
                    }
                }

                //deserialize the data from the file into the C# GameData object
                loadedData = JsonUtility.FromJson<GameData>(dataToLoad);
            }
            catch (Exception e) {
                Debug.LogError("Error occurred when trying to load data from file: " +  fullPathToSaveTo + e.Message);
            }
        }

        return loadedData;
    }

    public void Save(GameData currentGameData, SaveFileType saveFileType) {
        string fullPathToSaveTo;
        switch (saveFileType){
            case SaveFileType.Manual:
                fullPathToSaveTo = Path.Combine(dataDirPath, manualSavedDataFileName);
                break;
            case SaveFileType.Auto:
                fullPathToSaveTo = Path.Combine(dataDirPath, autoSavedDataFileName);
                break;
            default:
                Debug.LogError("SaveFileType is None or invalid, cannot save data.");
                return;
        }
        
        try {
            //create the directory the file will be written to if it doesn't already exist
            Directory.CreateDirectory(Path.GetDirectoryName(fullPathToSaveTo));

            //serialize the game data object into a JSON string
            //true formats the JSON data
            string dataToStore = JsonUtility.ToJson(currentGameData, true);

            //write the data to the file
            using (FileStream stream = new FileStream(fullPathToSaveTo, FileMode.Create)) {
                using (StreamWriter writer = new StreamWriter(stream)) {
                    writer.Write(dataToStore);
                }
            }
        }
        catch (Exception e) {
            Debug.LogError("Error occurred when trying to save data to file: " + fullPathToSaveTo + e.Message);
        }
    }

}
public enum SaveFileType
{
    None,
    Manual,
    Auto
}
