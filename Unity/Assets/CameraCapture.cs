using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Runtime.InteropServices;


public class CameraCapture : MonoBehaviour {

	private String CameraSerial = "face";

	private PopUnityChannel	mTrackChannel = null;
	private String TrackCommand = "PopCameraTrack --childmode=1 --binarystdio=1 --MatchStepX=18 --MatchStepY=15 ";

	private PopUnityChannel	mCaptureChannel = null;
	private String CaptureCommand = "PopCapture --childmode=1 --binarystdio=1";

	private Texture2D mTexture;
	public Material MaterialForTexture;
	static public List<String> mPopUnityDebugLog = new List<String>();
	public List<TFeatureMatch> mFeatures;

	private int Height = 1;
	private int Width = 1;

	static void GuiLog(String Log)
	{
		mPopUnityDebugLog.Add( Log );
		while (mPopUnityDebugLog.Count > 20)
			mPopUnityDebugLog.RemoveAt (0);
	}

	void Start()
	{
		PopUnity.Start();
//		PopUnity.DebugDelegate += GuiLog;

		//	need to CREATE a texture. overwriting an existing one doesn't work...
		if (mTexture == null) {
			mTexture = new Texture2D (1280, 1024, TextureFormat.BGRA32, false);

			//	need a material...
			if (MaterialForTexture != null)
				MaterialForTexture.mainTexture = mTexture;
		}

		PopUnity.AssignJobHandler("re:getframe", ((Job) => this.OnGetFrameReply(Job)) );
		PopUnity.AssignJobHandler("newframe", ((Job) => this.OnGetFrameReply(Job)) );
		PopUnity.AssignJobHandler("newfeatures", ((Job) => this.OnNewFeatures(Job)) );

	}

	bool HasStartedTracking()
	{
		return (mCaptureChannel != null) && (mTrackChannel != null);
	}

	void StartTracking()
	{
		String TrackJobString = "subscribenewfeatures serial=" + CameraSerial + " asbinary=1 memfile=1";
		String CaptureJobString = "subscribenewframe serial=" + CameraSerial + " asbinary=1 memfile=1";

		String TrackChannelAddress = TrackCommand + " --serial=" + CameraSerial;

		if (!TrackCommand.StartsWith ("fork:")) {
			TrackChannelAddress = "fork:" + Application.streamingAssetsPath + "/" + TrackChannelAddress + " --forkpath=" + Application.streamingAssetsPath + "/";
		}
		mTrackChannel = new PopUnityChannel (TrackChannelAddress);
		mTrackChannel.SendJob( TrackJobString );
		
		
		
		String CaptureChannelAddress = CaptureCommand;
		if (!TrackCommand.StartsWith ("fork:")) {
			CaptureChannelAddress = "fork:" + Application.streamingAssetsPath + "/" + CaptureCommand + " --forkpath=" + Application.streamingAssetsPath + "/";
		}
		mCaptureChannel = new PopUnityChannel (CaptureChannelAddress);
		mCaptureChannel.SendJob( CaptureJobString );
	}

	void Update () {
		PopUnity.Update ();
	}

	void OnGetFrameReply(PopJob Job)
	{
		Job.GetParam("default",mTexture,SoyPixelsFormat.Invalid,true);
		Job.GetParamPixelsWidthHeight ("default", out Width, out Height);
	}

	void OnNewFeatures(PopJob Job)
	{
		Debug.Log ("new features!");
		if (mFeatures == null)
			mFeatures = new List<TFeatureMatch> ();
		Job.GetParamArray ("default", "tfeaturematch", ref mFeatures, 1000 );
		Job.GetParam("image",mTexture);
	}

	Rect StepRect(Rect rect)
	{
		rect.y += rect.height + (rect.height*0.30f);
		return rect;
	}

	private static Texture2D gRectTexture;
	private static GUIStyle gRectStyle;
	
	// Note that this function is only meant to be called from OnGUI() functions.
	public static void GUIDrawRect( Rect position, Color color )
	{
		if( gRectTexture == null )
		{
			gRectTexture = new Texture2D( 1, 1 );
		}
		
		if( gRectStyle == null )
		{
			gRectStyle = new GUIStyle();
		}
		
		gRectTexture.SetPixel( 0, 0, color );
		gRectTexture.Apply();
		
		gRectStyle.normal.background = gRectTexture;
		
		GUI.Box( position, GUIContent.none, gRectStyle );
		
		
	}


	void OnGUI()
	{
		if (!HasStartedTracking ()) {
			Rect rect = new Rect (20, 20, Screen.width - 40, 100);
			CameraSerial = GUI.TextField (rect, CameraSerial);
			rect = StepRect (rect);

			if (GUI.Button (rect, "start with camera " + CameraSerial)) {
				StartTracking ();
			}
			rect = StepRect (rect);

			return;
		}


		{
			Rect rect = new Rect (0, 0, Screen.width, Screen.height);
			String LogString = String.Join ("\n", mPopUnityDebugLog.ToArray ());
			GUI.Label (rect, LogString);
		}

		if (mTexture != null) {
			Rect rect = new Rect (0, 0, Screen.width, Screen.height);
			GUI.DrawTexture (rect, mTexture);
		}

		if (mFeatures != null) {
			int RectSize = 10;
			//	draw features
			Color Colour = new Color (1.0f, 0.0f, 0.0f, 0.7f);
			foreach (TFeatureMatch Feature in mFeatures) {
				Colour.r = 1.0f - Feature.mScore;
				Colour.g = Feature.mScore;

				//	normalise x/y
				float x = (Feature.mCoord_x) / (float)Width;
				float y = (Height - Feature.mCoord_y) / (float)Height;
				//	scale xy to screen
				Rect FeatureRect = new Rect (x * Screen.width, y * Screen.height, RectSize, RectSize);
				GUIDrawRect (FeatureRect, Colour);
			}

		}
	}

	void OnPostRender()
	{
		GL.IssuePluginEvent (0);
	}
}
