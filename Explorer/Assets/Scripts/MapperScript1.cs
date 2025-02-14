using UnityEngine;
using System.IO;
using System.Collections;
using System.Collections.Generic;

public class MapperScript1 : MonoBehaviour
{
    
    private Camera mapperCamera;
    private GameObject freeCamera;
    // private GameObject avatar;
    
    private bool showButton = true;
    private string screenshotPath;


    private bool isMapping = false;

    private List<string> dirtyCoords = new List<string>();
    private UnityEngine.Vector3 currentPosition;
    private int currentX = 0, currentY = 0;

    public float flyingHeight = 120f;

    private float parcelSize = 16f;


    void Start()
    {
        Debug.Log("Mapper script is ready!");
        
        // Find and store the MapperCamera and avatar reference
        // mapperCamera = GameObject.Find("MapperCamera").GetComponent<Camera>();
        // avatar = GameObject.Find("CharacterObject(Clone)");
        Invoke("FindFreeCamera", 20f);
        
        // if (mapperCamera == null)
        // {
        //     Debug.LogError("MapperCamera not found!");
        // }

        // Get desktop path and create map folder
        string desktopPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop);
        screenshotPath = Path.Combine(desktopPath, "map");
        
        // Create directory if it doesn't exist
        if (!Directory.Exists(screenshotPath))
        {
            Directory.CreateDirectory(screenshotPath);
        }
        
        UnityEngine.Debug.Log("Screenshots will be saved to: " + screenshotPath);

        getCoordsFromFile();
    }

    void OnGUI()
    {
        if (showButton)
        {
            if (GUI.Button(new Rect(10, 10, 150, 50), "Start Mapping"))
            {
                Debug.Log("START MAPPING!");
                isMapping = true;

                FindFreeCamera();

                FindAndDeactivate("Satellite View");
                FindAndDeactivate("REMOTE_ENTITIES");

                //Start Mapping
                GoToNextParcel();
            }
        }
    }
    
    void GoToNextParcel()
    {
        //Hide the button
        showButton = false;
        //Grab first position from array
        string[] splittedCoords = dirtyCoords[0].Split(',');
        //Convert to currentX and currentY positions
        currentX = int.Parse(splittedCoords[0]);
        currentY = int.Parse(splittedCoords[1]);
        currentPosition = new Vector3(currentX * parcelSize, flyingHeight, currentY * parcelSize);

        dirtyCoords.RemoveAt(0);

        if (Mathf.Abs(currentX) > 165f)
        {
            Debug.Log("Reached end of the world at " + currentPosition);
            Application.Quit();
            return;
        }

        string fullScreenshotPath = GetCurrentScreenshotPath("map");
        // check if screenshot was already taken and skip if so
        if (System.IO.File.Exists(fullScreenshotPath))
        {
            Debug.Log("Screenshot already exists for coordinate (" + currentX + "," + currentY + "), skipping...");
            Invoke("GoToNextParcel", 0.01f);
            return;
        }

        Debug.Log("now moving to position: (" + currentX + ", " + currentY + ")");

        // avatar = GameObject.Find("CharacterObject(Clone)");
        // freeCamera = GameObject.Find("FreeCamera");

        // if (freeCamera == null)
        // {
        //     Debug.LogError("freeCamera not found!");
        // }
        // else
        // {
        //     Debug.LogError("freeCamera successfully found!");
        // }

        // move mapperCamera to current position
        UnityEngine.Vector3 targetPosition = new UnityEngine.Vector3(parcelSize / 2, 0, parcelSize / 2) + currentPosition;
        // UnityEngine.Vector3 delta = mapperCamera.transform.position - targetPosition;
        UnityEngine.Vector3 delta = freeCamera.transform.position - targetPosition;
        if (delta.magnitude > 0.1f)
        {
            // avatar.transform.position = targetPosition;
            // mapperCamera.transform.position = targetPosition;
            freeCamera.transform.position = targetPosition;
            StartCoroutine(PanCamera());
        }
        waitStartTime = Time.time;
        Invoke("WaitForScreenshot", 2f);
    }
  
    private void getCoordsFromFile()
    {
        string txtPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop) + "/coords.txt";
        var sr = new StreamReader(txtPath);  //Open the file
        var fileContents = sr.ReadToEnd();  //Copy the file contents to a variable
        sr.Close();  //Close the file

        char[] delimiterChars = {';', '\n' };

        string[] estatesArr = fileContents.Split(delimiterChars) ; //estatesArr[0] ej. "-150,20;-150,25"
        Debug.Log("Screenshots to take:");
        Debug.Log(estatesArr.Length);
        foreach (string estateCoordsString in estatesArr)
        {
            var splitted = estateCoordsString.Split(";"[0]);
            foreach (string coordinate in splitted)
            {
                dirtyCoords.Add(coordinate);
            }
        }

    }

    private string GetCurrentScreenshotPath(string destDirectory)
    {
        string desktopPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop);
        string name = currentX + "," + currentY + ".png";
        string fullScreenshotPath = Path.Combine(desktopPath, destDirectory, name);
        return fullScreenshotPath;
    }

    private float waitStartTime = 0f;
    public float waitTimeout = 60f;
    public float waitBeforeScreenshot = 20f;

    void WaitForScreenshot()
    {
        string fullScreenshotPath = GetCurrentScreenshotPath("map");
        // check if screenshot was already taken
        if (System.IO.File.Exists(fullScreenshotPath))
        {
            Debug.Log("Screenshot already exists for coordinate (" + currentX + "," + currentY + ")");
            GoToNextParcel();
            return;
        }

        // check if waited too long (timeout)
        float waitedTime = Time.time - waitStartTime;
        bool timeoutExpired = waitedTime > waitTimeout;

        bool allScenesLoaded = false;
        // check if all scenes are loaded
        // ParcelScene[] scenes = FindObjectsOfType<ParcelScene>();
        // foreach (ParcelScene scene in scenes)
        // {
        //     bool importantScene = true;

        //     if (!importantScene)
        //     {
        //         // Debug.Log("Scene " + scene.gameObject.name + " is not important, skipping...");
        //         continue;
        //     }
        //     // Debug.Log("Scene " + scene.gameObject.name + " is important, checking if loaded...");

        //     if (!scene.gameObject.name.Contains("ready!"))
        //     {
        //         Debug.Log(scene.gameObject.name + " is not loaded, waiting...");
        //         allScenesLoaded = false;
        //     }
        // }

        if (allScenesLoaded || timeoutExpired)
        {
            if (allScenesLoaded)
            {
                Debug.Log("all scenes are loaded, preparing to take screenshot at (" + currentX + "," + currentY + ")");
            }
            if (timeoutExpired && !allScenesLoaded)
            {
                Debug.Log("Timeout waiting for screenshot at coordinate (" + currentX + "," + currentY + ")");
            }
            StartCoroutine(TakeScreenshotsAndGoToNextParcel());
        }
        else
        {
            Debug.Log("waiting for screenshot at (" + currentX + "," + currentY + ") for " + waitedTime + " seconds");
            Invoke("WaitForScreenshot", 5f);
        }

    }

    private IEnumerator TakeScreenshotsAndGoToNextParcel()
    {
        yield return new WaitForSeconds(waitBeforeScreenshot);
        yield return StartCoroutine(TakeScreenshot("day"));
        // DisableLightSources();
        // yield return StartCoroutine(TakeScreenshot("night"));
        yield return new WaitForSeconds(2f);
        GoToNextParcel();
        // EnableLightSources();
    }

    //Remove screenshot mode!!!!!
    private IEnumerator TakeScreenshot(string mode)
    {
        string screenshotMode = mode == "day" ? "map" : (mode == "night" ? "map-night" : "");
        string fullScreenshotPath = GetCurrentScreenshotPath(screenshotMode);
        UnityEngine.Debug.Log("Taking " + mode + " screenshot at (" + currentX + "," + currentY + ") now!");
        ScreenCapture.CaptureScreenshot(fullScreenshotPath);
        yield return null;
    }

    //Find FreeCamera
    void FindFreeCamera()
    {
        freeCamera = GameObject.Find("FreeCamera");

        if (freeCamera != null)
        {
            Debug.Log("freeCamera successfully found!");
        }
        else
        {
            Debug.LogError("freeCamera not found!");
        }
    }

    void FindAndDeactivate(string objectName)
    {
        GameObject obj = GameObject.Find(objectName);
        if (obj != null)
        {
            obj.SetActive(false);
            Debug.Log(objectName + " deactivated!");
        }
        else
        {
            Debug.LogWarning(objectName + " not found!");
        }
    }

    IEnumerator PanCamera()
    {
        freeCamera.transform.rotation = Quaternion.Euler(85f, 0f, 0f);
        yield return new WaitForSeconds(0.5f);
        freeCamera.transform.rotation = Quaternion.Euler(80f, 0f, 0f);
        yield return new WaitForSeconds(0.5f);
        freeCamera.transform.rotation = Quaternion.Euler(95f, 0f, 0f);
        yield return new WaitForSeconds(0.5f);
        freeCamera.transform.rotation = Quaternion.Euler(100f, 0f, 0f);
        yield return new WaitForSeconds(0.5f);
        freeCamera.transform.rotation = Quaternion.Euler(95f, 0f, 0f);
        yield return new WaitForSeconds(0.5f);
        freeCamera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
    }
}