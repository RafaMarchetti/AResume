/*============================================================================== 
 * Copyright (c) 2012-2014 Qualcomm Connected Experiences, Inc. All Rights Reserved. 
 * ==============================================================================*/

using UnityEngine;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using Vuforia;

/// <summary>
/// UI Event Handler class that handles events generated by user-tap actions
/// over the UI Options Menu
/// </summary>
public class VirtualButtonsUIEventHandler : ISampleAppUIEventHandler {
    
    #region PUBLIC_MEMBER_VARIABLES
    public override event System.Action CloseView;
    public override event System.Action GoToAboutPage;
    public static bool offTargetTrackingIsEnabled = false;
    public Material mVirtualButtonMaterial;
    #endregion PUBLIC_MEMBER_VARIABLES
    
    #region PRIVATE_MEMBER_VARIABLES
    private VirtualButtonsUIView mView;
    
    private ImageTargetBehaviour mAResume = null;

    // Dictionary for storing virtual button positions.
    private Dictionary<string, Vector3> mVBPositionDict =
        new Dictionary<string, Vector3>();

    // Dictionary for storing virtual button scale values.
    private Dictionary<string, Vector3> mVBScaleDict =
        new Dictionary<string, Vector3>();
    #endregion PRIVATE_MEMBER_VARIABLES
    
    #region PUBLIC_MEMBER_PROPERTIES
    public VirtualButtonsUIView View
    {
        get {
            if(mView == null){
                mView = new VirtualButtonsUIView();
                mView.LoadView();
            }
            return mView;
        }
    }
    #endregion PUBLIC_MEMBER_PROPERTIES
    
    #region PUBLIC_METHODS
    public override void UpdateView (bool tf)
    {
        this.View.UpdateUI(tf);
    }
    
    public override  void Bind()
    {
        this.View.mCloseButton.TappedOn             += OnTappedOnCloseButton;
        this.View.mButtonTwo.TappedOn      += OnTappedOnButtonTwo;
        this.View.mButtonFour.TappedOn     += OnTappedOnButtonFour;
        this.View.mButtonThree.TappedOn        += OnTappedOnButtonThree;
        this.View.mButtonOne.TappedOn       += OnTappedOnButtonOne;
        this.View.mAboutButton.TappedOn             += OnTappedOnAboutButton;

        // register Vuforia started callback
        VuforiaAbstractBehaviour vuforiaBehaviour = (VuforiaAbstractBehaviour)FindObjectOfType(typeof(VuforiaAbstractBehaviour));
        if (vuforiaBehaviour)
        {
            vuforiaBehaviour.RegisterVuforiaStartedCallback(EnableContinuousAutoFocus);
            vuforiaBehaviour.RegisterOnPauseCallback(OnPause);
        }
        
         // Find the Wood image target.
        mAResume = GameObject.Find("AResume").GetComponent<ImageTargetBehaviour>();

        // Add a mesh for each virtual button on the Wood target.
        VirtualButtonBehaviour[] vbs =
                mAResume.gameObject.GetComponentsInChildren<VirtualButtonBehaviour>();
        foreach (VirtualButtonBehaviour vb in vbs)
        {
            CreateVBMesh(vb);
            // Also store the position and scale for later.
            mVBPositionDict.Add(vb.VirtualButtonName, vb.transform.localPosition);
            mVBScaleDict.Add(vb.VirtualButtonName, vb.transform.localScale);
        }
        
    }
    
    public override  void UnBind()
    { 
        this.View.mCloseButton.TappedOn             -= OnTappedOnCloseButton;
        this.View.mButtonTwo.TappedOn      -= OnTappedOnButtonTwo;
		this.View.mButtonFour.TappedOn     -= OnTappedOnButtonFour;
		this.View.mButtonThree.TappedOn        -= OnTappedOnButtonThree;
		this.View.mButtonOne.TappedOn       -= OnTappedOnButtonOne;
        this.View.mAboutButton.TappedOn             -= OnTappedOnAboutButton;

        // unregister Vuforia started callback
        VuforiaAbstractBehaviour vuforiaBehaviour = (VuforiaAbstractBehaviour)FindObjectOfType(typeof(VuforiaAbstractBehaviour));
        if (vuforiaBehaviour)
        {
            vuforiaBehaviour.UnregisterVuforiaStartedCallback(EnableContinuousAutoFocus);
            vuforiaBehaviour.UnregisterOnPauseCallback(OnPause);
        }

        mVBPositionDict.Clear();
        mVBScaleDict.Clear();
        this.View.UnLoadView();
        mView = null;
    }
    
    //SingleTap Gestures are captured by AppManager and calls this method for TapToFocus
    public override  void TriggerAutoFocus()
    {
        StartCoroutine(TriggerAutoFocusAndEnableContinuousFocusIfSet());
    }
    
    #endregion PUBLIC_METHODS
    
    #region PRIVATE_METHODS
    
    /// <summary>
    /// Activating trigger autofocus mode unsets continuous focus mode (if was previously enabled from the UI Options Menu)
    /// So, we wait for a second and turn continuous focus back on (if options menu shows as enabled)
    /// </returns>
    private IEnumerator TriggerAutoFocusAndEnableContinuousFocusIfSet()
    {
        //triggers a single autofocus operation 
        if (CameraDevice.Instance.SetFocusMode(CameraDevice.FocusMode.FOCUS_MODE_TRIGGERAUTO)) {
              this.View.FocusMode = CameraDevice.FocusMode.FOCUS_MODE_TRIGGERAUTO;
        }
        
        yield return new WaitForSeconds(1.0f);
         
        //continuous focus mode is turned back on 
        if (CameraDevice.Instance.SetFocusMode(CameraDevice.FocusMode.FOCUS_MODE_CONTINUOUSAUTO)) {
          this.View.FocusMode = CameraDevice.FocusMode.FOCUS_MODE_CONTINUOUSAUTO;
        }
        
        
        Debug.Log (this.View.FocusMode);
        
    }

    private void OnPause(bool pause)
    {
        if (!pause)
        {
            // set to continous autofocus
            CameraDevice.Instance.SetFocusMode(CameraDevice.FocusMode.FOCUS_MODE_CONTINUOUSAUTO);
        }
    }
    
    private void OnTappedOnAboutButton(bool tf)
    {
        if(this.GoToAboutPage != null)
        {
            this.GoToAboutPage();
        }
    }
    
    //We want autofocus to be enabled when the app starts
    private void EnableContinuousAutoFocus()
    {
        if (CameraDevice.Instance.SetFocusMode(CameraDevice.FocusMode.FOCUS_MODE_CONTINUOUSAUTO))
        {
            this.View.FocusMode = CameraDevice.FocusMode.FOCUS_MODE_CONTINUOUSAUTO;
        }
    }
    
    
    private void ResetCameraFacingToBack()
    {
        CameraDevice.Instance.Stop();
        CameraDevice.Instance.Init(CameraDevice.CameraDirection.CAMERA_BACK);
        CameraDevice.Instance.Start();
    }
    
    private bool ChangeCameraDirection(CameraDevice.CameraDirection direction)
    {
        bool directionSupported = false;
        CameraDevice.Instance.Stop();
        CameraDevice.Instance.Deinit();
        if(CameraDevice.Instance.Init(direction)) {
            directionSupported = true;
        }
        CameraDevice.Instance.Start();
        
        return directionSupported;
    }
    
    private void OnTappedOnButtonTwo(bool tf)
    {
        ToggleVirtualButton("button2");
        OnTappedToClose();
    }
    
	private void OnTappedOnButtonOne(bool tf)
    {
        ToggleVirtualButton("button1");
        OnTappedToClose();
    }
    
	private void OnTappedOnButtonThree(bool tf)
    {
        ToggleVirtualButton("button3");
        OnTappedToClose();
    }
    
	private void OnTappedOnButtonFour(bool tf)
    {
        ToggleVirtualButton("button4");
        OnTappedToClose();
    }
    
    private void OnTappedToClose()
    {
        if(this.CloseView != null)
        {
            this.CloseView();
        }
    }
    
    private void OnTappedOnCloseButton()
    {
        OnTappedToClose();
    }
    
     /// <summary>
    /// Create a mesh outline for the virtual button.
    /// </summary>
    private void CreateVBMesh(VirtualButtonBehaviour vb)
    {
        GameObject vbObject = vb.gameObject;

        MeshFilter meshFilter = vbObject.GetComponent<MeshFilter>();
        if (!meshFilter)
        {
            meshFilter = vbObject.AddComponent<MeshFilter>();
        }

        // Setup vertex positions.
        Vector3 p0 = new Vector3(-0.25f, 0, -0.25f);
        Vector3 p1 = new Vector3(-0.25f, 0, 0.25f);
        Vector3 p2 = new Vector3(0.25f, 0, -0.25f);
        Vector3 p3 = new Vector3(0.25f, 0, 0.25f);

        Mesh mesh = new Mesh();
        mesh.vertices = new Vector3[] { p0, p1, p2, p3 };
        mesh.triangles = new int[]  {
                                        0,1,2,
                                        2,1,3
                                    };

        // Add UV coordinates.
        mesh.uv = new Vector2[]{
                new Vector2(0,0),
                new Vector2(1,0),
                new Vector2(0,1),
                new Vector2(1,1)
                };

        // Add empty normals array.
        mesh.normals = new Vector3[mesh.vertices.Length];

        // Automatically calculate normals.
        mesh.RecalculateNormals();
        mesh.name = "VBPlane";

        meshFilter.sharedMesh = mesh;

        MeshRenderer meshRenderer = vbObject.GetComponent<MeshRenderer>();
        if (!meshRenderer)
        {
            meshRenderer = vbObject.AddComponent<MeshRenderer>();
        }

        meshRenderer.sharedMaterial = mVirtualButtonMaterial;
    }

    
    /// <summary>
    /// Create or destroy the virtual button with the given name.
    /// </summary>
    private void ToggleVirtualButton(string name)
    {
        if (mAResume.ImageTarget != null)
        {
            // Get the virtual button if it exists.
            VirtualButton vb = mAResume.ImageTarget.GetVirtualButtonByName(name);

            if (vb != null)
            {
                // Destroy the virtual button if it exists.
                mAResume.DestroyVirtualButton(name);
            }
            else
            {
                // Get the position and scale originally used for this virtual button.
                Vector3 position, scale;
                if (mVBPositionDict.TryGetValue(name, out position) &&
                    mVBScaleDict.TryGetValue(name, out scale))
                {
                    // Deactivate the dataset before creating the virtual button.
                    ObjectTracker objectTracker = TrackerManager.Instance.GetTracker<ObjectTracker>();
                    DataSet dataSet = objectTracker.GetActiveDataSets().First();
                    objectTracker.DeactivateDataSet(dataSet);

                    // Create the virtual button.
                    VirtualButtonBehaviour vbb = mAResume.CreateVirtualButton(name,
                                                                                      new Vector2(position.x, position.z),
                                                                                      new Vector2(scale.x, scale.z)) as VirtualButtonBehaviour;
                    if (vbb != null)
                    {
                        // Register the button with the event handler on the Wood target.
                        vbb.RegisterEventHandler(mAResume.GetComponent<VirtualButtonEventHandler>());

                        // Add a mesh to outline the button.
                        CreateVBMesh(vbb);

                        // If the Wood target isn't currently tracked hide the button.
                        if (mAResume.CurrentStatus == TrackableBehaviour.Status.NOT_FOUND)
                        {
                            vbb.GetComponent<Renderer>().enabled = false;
                        }
                    }

                    // Reactivate the dataset.
                    objectTracker.ActivateDataSet(dataSet);
                }
            }
        }
    }
    
    #endregion PRIVATE_METHODS
}
