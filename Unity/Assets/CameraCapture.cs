using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Runtime.InteropServices;


public class CameraCapture : MonoBehaviour {

//	private String ForkExeCommand = "PopCameraTrack --childmode=1 --binarystdio=1";
//	private String	JobString = "subscribenewfeatures serial=0x1a11000005ac8510 asbinary=1 memfile=1";
	private String ForkExeCommand = "PopCapture --childmode=1 --binarystdio=1";
	private String	JobString = "subscribenewframe serial=face memfile=1";
	//private String	ForkExeCommand = "fork:pwd";
	private PopUnityChannel	mChannel = null;
	private Texture2D mTexture;
	public Material MaterialForTexture;
	static public List<String> mPopUnityDebugLog = new List<String>();

	static void GuiLog(String Log)
	{
		mPopUnityDebugLog.Add( Log );
		while (mPopUnityDebugLog.Count > 20)
			mPopUnityDebugLog.RemoveAt (0);
	}

	void Start()
	{
		PopUnity.Start();
		PopUnity.DebugDelegate += GuiLog;

		//	need to CREATE a texture. overwriting an existing one doesn't work...
		if (mTexture == null) {
			mTexture = new Texture2D (1280, 1024, TextureFormat.BGRA32, false);

			//	need a material...
			if (MaterialForTexture != null)
				MaterialForTexture.mainTexture = mTexture;
		}

		PopUnity.AssignJobHandler("re:getframe", ((Job) => this.OnGetFrameReply(Job)) );
		PopUnity.AssignJobHandler("newframe", ((Job) => this.OnGetFrameReply(Job)) );

		String ChannelAddress = ForkExeCommand;
		if (!ForkExeCommand.StartsWith ("fork:")) {
			ChannelAddress = "fork:" + Application.streamingAssetsPath + "/" + ForkExeCommand;
		}

		mChannel = new PopUnityChannel (ChannelAddress);
		mChannel.SendJob( JobString );
	}

	void Update () {
		PopUnity.Update ();
	}

	void OnGetFrameReply(PopJob Job)
	{
		Job.GetParam("default",mTexture);
	}

	Rect StepRect(Rect rect)
	{
		rect.y += rect.height + (rect.height*0.30f);
		return rect;
	}

	void OnGUI()
	{

		Rect rect = new Rect ( 20, 20, Screen.width-40, 100 );
		JobString = GUI.TextField (rect, JobString);
		rect = StepRect (rect);

		if (GUI.Button (rect, "run command")) {
			mChannel.SendJob (JobString);
		}
		rect = StepRect (rect);

		//	fill
		rect.height = Screen.height - rect.y;

		rect = new Rect (0, 0, Screen.width, Screen.height);
		String LogString = String.Join("\n", mPopUnityDebugLog.ToArray() );
			GUI.Label (rect, LogString );

		GUI.DrawTexture (rect, mTexture);
	}

	void OnPostRender()
	{
		GL.IssuePluginEvent (0);
	}
}
