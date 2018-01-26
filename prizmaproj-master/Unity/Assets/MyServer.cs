using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.Text;
using System;
public class MyServer : MonoBehaviour
{
    int connectionId;
    int maxConnections = 10;
    int reliableChannelID;
    int hostId;
    int port= 8888;
    byte error;
    public AnimationManager manager;
    int CurrentConID = -1;
    // Use this for initialization
    void Start()    
    {
        /*server = new NetworkServerSimple();
        if(server.Listen("127.0.0.1", 5656))
            GameObject.Find("DebugText").GetComponent<TextMesh>().text = "waiting for connection";
        server.RegisterHandler(MsgType.Connect, OnConnected);*/
        NetworkTransport.Init();
        ConnectionConfig config = new ConnectionConfig();
        reliableChannelID = config.AddChannel(QosType.ReliableSequenced);
        HostTopology topology = new HostTopology(config, maxConnections);
        hostId = NetworkTransport.AddHost(topology, port,null);
        Debug.Log("Socket Open! Host ID is " + hostId);
    }
    AnimationManager.RotDir ConvertToRotDir(string dir)
    {
        switch (dir)
        {
            case "Left":
                return AnimationManager.RotDir.Left;
            case "Right":
                return AnimationManager.RotDir.Right;
            case "Up":
                return AnimationManager.RotDir.Up;
            case "Down":
                return AnimationManager.RotDir.Down;
            case "Cw":
                return AnimationManager.RotDir.CW;
            case "Ccw":
                return AnimationManager.RotDir.CCW;
            default:
                return AnimationManager.RotDir.None;
        }

    }
    AnimationManager.LightDir ConvertToLightDir(string dir)
    {
        switch (dir)
        {
            case "Left":
                return AnimationManager.LightDir.Left;
            case "Right":
                return AnimationManager.LightDir.Right;
            case "Front":
                return AnimationManager.LightDir.Front;
            case "Back":
                return AnimationManager.LightDir.Back;
         
            default:
                return AnimationManager.LightDir.None;
        }

    }
    void HandleMsg(string msg)
    {
        string[] message = msg.Split('|');
        switch (message[0])
        {
            case "P":
                manager.TogglePresentationMode();
                break;
            case "SlidingSpeed":
                manager.ChangeSlidingSpeed(float.Parse(message[1]));
                break;
            case "AxisRotation":
                if (message[1] == "X")  manager.deltaXAngle = int.Parse(message[2]);
                if (message[1] == "Y") manager.deltaYAngle = int.Parse(message[2]);
                if (message[1] == "Z") manager.deltaZAngle = int.Parse(message[2]);
                break;
            case "Scale":
                if (message[1] == "Up") manager.ScaleModel(true);
                if (message[1] == "Down") manager.ScaleModel(false);
                break;
            case "Rotate":
                manager.RotCurrentModel(ConvertToRotDir(message[1]));
                break;
            case "Light":
                bool intensify = false;
                if (message[2] == "Lighten")
                    intensify = true;
                manager.ChangeLightIntensity(ConvertToLightDir(message[1]), intensify);
                break;
            case "Clear":
                manager.ClearCurrentModel();
                break;
            case "NextModel":
                manager.showNextAnim(true);
                break;
            case "PrevModel":
                manager.showNextAnim(false);
                break;
            case "ModelRequest":
                Debug.Log(name + " was requested");
                manager.ShowModel(message[1]);
                break;
        }
    }
     void Update()
    {
        int recHostID;
        int recConnectionID;
        int recChannelID;
        byte[] buffer = new byte[1024];
        int bufferSize = 1024;
        int dataSize;
        NetworkEventType recNetworkEvent = NetworkTransport.Receive(out recHostID, out recConnectionID, out recChannelID, buffer, bufferSize, out dataSize, out error);
        
        switch (recNetworkEvent)
        {
            case NetworkEventType.DataEvent:
                string msg = Encoding.Unicode.GetString(buffer,0,dataSize);
                //GameObject.Find("DebugText").GetComponent<TextMesh>().text = msg;
                Debug.Log("Received " + msg+"From conId: "+recConnectionID);
                HandleMsg(msg);
                SendMsg("server accepted", recConnectionID);
                Debug.Log("sent message");
                break;
            case NetworkEventType.ConnectEvent:
                Debug.Log("some1 with conID " + recConnectionID + " connected!");
                CurrentConID = recConnectionID;
                SendMsg("server connected", recConnectionID);
                SendInitData(recConnectionID);
                break;
            case NetworkEventType.DisconnectEvent:
                Debug.Log("some1 with conID " + recConnectionID + " disconnected!");
                CurrentConID = -1;
                break;
            default:
                break;
        }
    }
    public void SendMsg(string msg,int conId)
    {
            byte[] buffer = Encoding.Unicode.GetBytes(msg);
            NetworkTransport.Send(hostId, conId, reliableChannelID, buffer, sizeof(char) * msg.Length, out error);
        
    }
    public void SendBytes(byte[] data,int conId)
    {
        Debug.Log(data.Length);
        NetworkTransport.Send(hostId, conId, reliableChannelID, data, data.Length, out error);
    }
    void SendInitData(int conID)
    {
        SendMsg("InitStart",conID);
        SendMsg("Presentation|" + (manager.mode == AnimationManager.PresentationMode.Automatic ? "auto" : "manual"),conID);
        SendMsg("AxisRotation|X|" + manager.deltaXAngle,conID);
        SendMsg("AxisRotation|Y|" + manager.deltaYAngle, conID);
        SendMsg("AxisRotation|Z|" + manager.deltaZAngle, conID);
        SendMsg("SlidingSpeed|" + manager.repeatRateSec, conID);
        Dictionary<string, Texture2D> images = manager.GetImages();
        SendMsg("SendingImages|" + images.Count,conID);
        foreach (string imageName in images.Keys)
        {
            SendImage(images[imageName], conID, imageName);
        }
        SendMsg("FinishInit", conID);
    }
    void SendImage(Texture2D tex,int conID,string imageName)
    {
        byte[] data = tex.EncodeToPNG();
        SendMsg("ExpectImage|" + imageName + "|" + data.Length, conID);
        //send chunks of 1024 byte
        int chunkAmmount = (data.Length / 1024);
        for (int chunks = 0; chunks < chunkAmmount; ++chunks)
        {
            byte[] chunk = new byte[1024];
            for (int i = 0; i < 1024; i++)
                chunk[i] = data[chunks * 1024 + i];
            SendBytes(chunk, conID);
        }
        //send the remaining if it wasn't a multiply of 1024
        int remainingBytes = data.Length - chunkAmmount * 1024;
        byte[] remainChunk = new byte[remainingBytes];
        for (int i = 0; i < remainingBytes; i++)
        {
            remainChunk[i] = data[chunkAmmount * 1024 + i];
        }
        SendBytes(remainChunk, conID);
    }
    public void PresentationClientNotify()
    {
        SendMsg("Presentation|" + (manager.mode == AnimationManager.PresentationMode.Automatic ? "auto" : "manual"), CurrentConID);
    }
    // Update is called once per frame

}
