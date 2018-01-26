using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.Net;
using System.Net.Sockets;
public class AnimationManager : MonoBehaviour {
	public float repeatRateSec;
	public string objectsTag;
    public Camera camLeft;
    public Camera camRight;
    GameObject currentAnim;
    Quaternion savedRot;
    Vector3 savedPos;
    Dictionary<string, Texture2D> texArr;
    Dictionary<string, GameObject> modelDict;
    List<GameObject> myArr;
    public GameObject anchor;
    public Image DebugImage;
    public float deltaXAngle=2;
    public float deltaYAngle = 2;
    public float deltaZAngle = 2;
    public float scalePercent=10;
    int myIter=0;
    public enum PresentationMode { Manual, Automatic }
    public enum RotDir {None, Left, Right, Up, Down, CW, CCW };
    public enum LightDir { None,Left,Right,Front,Back};
    public PresentationMode mode = PresentationMode.Automatic;
	// Use this for initialization
	void Start () {

        myArr = new List<GameObject>();
        texArr = new Dictionary<string, Texture2D>();
        modelDict = new Dictionary<string, GameObject>();
        
        foreach (GameObject o in Resources.LoadAll<GameObject>("Hologram_Repo"))
        {
            Debug.Log(o.name);
            #if UNITY_EDITOR
                Texture2D tex = UnityEditor.AssetPreview.GetAssetPreview(o);
                byte[] bytes = tex.EncodeToPNG();
                File.WriteAllBytes(Application.dataPath+"//Resources//" + o.name + ".png", bytes);
#endif
            Debug.Log(o.name);
            byte[] image=File.ReadAllBytes("Assets/Resources/" + o.name + ".png");
            Texture2D texture = new Texture2D(2, 2);
            texture.LoadImage(image);
            
            /* Sprite curSprite = DebugImage.GetComponent<Image>().sprite;
             DebugImage.GetComponent<Image>().sprite = Sprite.Create(texture, new Rect(0,0,tex.width,tex.height), new Vector2(0.5f,0.5f));
             */
            texArr.Add(o.name,texture);
            GameObject obj = Instantiate(o) as GameObject;
            obj.transform.Rotate(Vector3.right, 90);
            myArr.Add(obj);
            modelDict.Add(o.name, obj);
        }
        //hide all objects
        foreach (GameObject o in myArr){
            
            o.transform.position = anchor.transform.position;
            o.SetActive (false);
        }

        // flip cameras
        camLeft.projectionMatrix = camLeft.projectionMatrix * Matrix4x4.Scale(new Vector3(1, -1, 1));
        camRight.projectionMatrix = camRight.projectionMatrix * Matrix4x4.Scale(new Vector3(1, -1, 1));

        enableNextModel();
        InvokeRepeating("getNextModel", repeatRateSec, repeatRateSec) ;
	}
   
    void enableNextModel()
    {
        myArr[myIter].SetActive(true);		//set next animation object to true
        currentAnim = myArr[myIter];
        savedPos = currentAnim.transform.position;
        savedRot = currentAnim.transform.rotation;

    }
    void getNextModel()
    {
        showNextAnim(true);
    }
   public void showNextAnim(bool forward){
        
        myArr[myIter].transform.SetPositionAndRotation(savedPos, savedRot);

        myArr[myIter].SetActive (false);		//set current animation object to false
        if (forward)
            myIter++;
        else
            myIter--;
        myIter = (myIter+myArr.Count)% myArr.Count;
        enableNextModel();
		

    }
    public void ChangeLightIntensity(LightDir lightDir,bool intensify)
    {
        float factor = 1.1f;
        if (!intensify)
            factor = 0.9f;
        switch (lightDir)
        {
            case LightDir.Left:
                GameObject.Find("leftLight").GetComponent<Light>().intensity *=factor;
                break;
            case LightDir.Right:
                GameObject.Find("rightLight").GetComponent<Light>().intensity *= factor;
                break;
            case LightDir.Front:
                GameObject.Find("frontLight").GetComponent<Light>().intensity *= factor;
                break;
            case LightDir.Back:
                GameObject.Find("backLight").GetComponent<Light>().intensity *= factor;
                break;
        }

    }
    public void ChangeSlidingSpeed(float speed)
    {
        repeatRateSec = speed;
        if (mode == PresentationMode.Automatic)
        {
            CancelInvoke("getNextModel");
            InvokeRepeating("getNextModel", repeatRateSec, repeatRateSec);
        }
    }
    public void TogglePresentationMode()
    {
        if (mode == PresentationMode.Automatic)
        {
            CancelInvoke("getNextModel");
            mode = PresentationMode.Manual;
        }
        else
        {
            InvokeRepeating("getNextModel", repeatRateSec, repeatRateSec);
            mode = PresentationMode.Automatic;
        }
        GetComponent<MyServer>().PresentationClientNotify();

    }
    public void RotCurrentModel(RotDir rotDir)
    {
        Vector3 center = currentAnim.transform.position;
        //look for one of this renderers
        SkinnedMeshRenderer ren = currentAnim.GetComponentInChildren<SkinnedMeshRenderer>();
        if (ren != null)
        {
            center = ren.bounds.center;
        }
        else
        {
            MeshRenderer render= currentAnim.GetComponentInChildren<MeshRenderer>();
            center = render.bounds.center;
        }
        switch (rotDir)
        {
            case RotDir.CCW:
                currentAnim.transform.RotateAround(center, currentAnim.transform.forward, -deltaZAngle);
                break;
            case RotDir.CW:
                currentAnim.transform.RotateAround(center, currentAnim.transform.forward, deltaZAngle);
                break;
            case RotDir.Left:
                currentAnim.transform.RotateAround(center, currentAnim.transform.up, deltaYAngle);
                break;
            case RotDir.Right:
                currentAnim.transform.RotateAround(center, currentAnim.transform.up, -deltaYAngle);
                break;
            case RotDir.Up:
                currentAnim.transform.RotateAround(center, currentAnim.transform.right, -deltaXAngle);
                break;
            case RotDir.Down:
                currentAnim.transform.RotateAround(center, currentAnim.transform.right, deltaXAngle);
                break;
        }
    }
    public void ScaleModel(bool Up)
    {
        if (Up)
            currentAnim.transform.localScale *= (100 + scalePercent) / 100.0f;
        else
            currentAnim.transform.localScale *= (100 - scalePercent) / 100.0f;
    }
    public void ClearCurrentModel()
    {
        currentAnim.transform.SetPositionAndRotation(savedPos, savedRot);
    }
    public Dictionary<string,Texture2D> GetImages()
    {
        return texArr;
    }
    public void ShowModel(string name)
    {
        if (mode == PresentationMode.Automatic)
        {
            CancelInvoke("getNextModel");
            InvokeRepeating("getNextModel", repeatRateSec, repeatRateSec);
        }
        myArr[myIter].transform.SetPositionAndRotation(savedPos, savedRot);

        myArr[myIter].SetActive(false);
        GameObject obj = modelDict[name];
        
        for(int i=0;i<myArr.Count;i++)
            if (obj == myArr[i]) {
                myIter = i;
                break;
            }
        enableNextModel();
       

    }
    // Update is called once per frame
    void Update()
    {
        
        if (Input.GetKey(KeyCode.Q))
        {

            RotCurrentModel(RotDir.CCW);
        }
        else if (Input.GetKey(KeyCode.E))
        {
            RotCurrentModel(RotDir.CW);
        }
        else if (Input.GetKey(KeyCode.W))
        {
            RotCurrentModel(RotDir.Up);
        }
        else if (Input.GetKey(KeyCode.S))
        {
            RotCurrentModel(RotDir.Down);
        }
        else if (Input.GetKey(KeyCode.D))
        {
            RotCurrentModel(RotDir.Right);
        }
        else if (Input.GetKey(KeyCode.A))
        {
            RotCurrentModel(RotDir.Left);
        }
        else if (Input.GetKeyUp(KeyCode.RightArrow))
        {
            if (mode == PresentationMode.Manual)
                showNextAnim(true);

        }
        else if (Input.GetKeyUp(KeyCode.LeftArrow))
        {
            if (mode == PresentationMode.Manual)
                showNextAnim(false);
        }
        else if (Input.GetKeyUp(KeyCode.P))
        {
            TogglePresentationMode();
        }
        else if (Input.GetKeyUp(KeyCode.C))
        {
            ClearCurrentModel();
        }else if (Input.GetKey(KeyCode.Plus) || Input.GetKey(KeyCode.KeypadPlus))
        {
            ScaleModel(true);
        }else if (Input.GetKey(KeyCode.Minus) || Input.GetKey(KeyCode.KeypadMinus))
        {
            ScaleModel(false);
        }
    }
}
