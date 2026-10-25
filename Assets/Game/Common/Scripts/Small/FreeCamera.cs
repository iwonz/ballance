#if UNITY_ANDROID && !UNITY_EDITOR
using Input = Ballance2.InputCompatibility.AndroidInput;
#endif
using System.Collections;
using RuntimeGizmos;
using RuntimeSceneGizmo;
using UnityEngine;
using UnityEngine.EventSystems;
using static Ballance2.Services.GameManager;

/*
 * Copyright (c) 2022 mengyu
 * 
 * Module:
 * FreeCamera.cs
 * 
 * Purpose:
 * Free camera with keyboard movement, left-button dragging, right-button rotation, and wheel zoom.
 * 
 * Author:
 * mengyu
 */

namespace Ballance2.Game
{
  /// <summary>
  /// Free camera with keyboard movement, left-button dragging, right-button rotation, and wheel zoom.
  /// </summary>
  public class FreeCamera : MonoBehaviour
  {
    [Tooltip("Enable wireframe rendering.")]
    /// <summary>
    /// Enable wireframe rendering.
    /// </summary>
    public bool Wireframe = false;
    [Tooltip("Enable this camera's AudioListener.")]
    /// <summary>
    /// Enable this camera's AudioListener.
    /// </summary>
    public bool Audio = true;
    [Tooltip("Enable fog rendering.")]
    /// <summary>
    /// Enable fog rendering.
    /// </summary>
    public bool Fog = true;
    [Tooltip("Enable skybox rendering.")]
    /// <summary>
    /// Enable skybox rendering.
    /// </summary>
    public bool SkyBox = true;
    [Tooltip("Current camera instance.")]
    /// <summary>
    /// Get the current camera instance.
    /// </summary>
    public static FreeCamera Instance;
    [Tooltip("Camera speed change event.")]
    /// <summary>
    /// Camera speed changed.
    /// </summary>
    public VoidDelegate onCamSpeedChanged;

    [Tooltip("Look cursor.")]
    public Texture2D cursorDragEyeTexture;
    [Tooltip("Look cursor while dragging.")]
    public Texture2D cursorDragEyeFreeTexture;
    [Tooltip("Drag cursor.")]
    public Texture2D cursorDragHandTexture;
    private AudioListener AudioListener;

    private bool _Wireframe = false;
    private bool _Audio = true;
    private bool _Fog = true;
    private bool _SkyBox = true;

    private void UpdateWireframe()
    {
      _Wireframe = Wireframe;
      mainCam.clearFlags = _Wireframe ? CameraClearFlags.SolidColor : CameraClearFlags.Skybox;
    }
    private void UpdateAudio()
    {
      _Audio = Audio;
      AudioListener.enabled = _Audio;
    }
    private void UpdateFog()
    {
      _Fog = Fog;
      RenderSettings.fog = _Fog;
    }
    private void UpdateSkyBox()
    {
      _SkyBox = SkyBox;
      if (!_Wireframe)
        mainCam.clearFlags = _SkyBox ? CameraClearFlags.Skybox : CameraClearFlags.SolidColor;
    }

    [Tooltip("Camera movement speed with the keyboard.")]
    /// <summary>
    /// Keyboard movement speed.
    /// </summary>
    public float cameraSpeed = 4f;
    [Tooltip("Horizontal camera movement speed with the mouse.")]
    /// <summary>
    /// Horizontal mouse movement speed.
    /// </summary>
    public float m_moveSensitivityX = 1f;
    [Tooltip("Vertical camera movement speed with the mouse.")]
    /// <summary>
    /// Vertical mouse movement speed.
    /// </summary>
    public float m_moveSensitivityY = 1f;
    [Tooltip("Camera zoom speed.")]
    /// <summary>
    /// Camera zoom speed.
    /// </summary>
    public float m_moveSensitivityZoom = 1f;
    //Roate camera
    [Tooltip("Horizontal camera movement speed.")]
    /// <summary>
    /// Horizontal camera movement speed.
    /// </summary>
    public float m_sensitivityX = 10f;
    [Tooltip("Vertical camera movement speed.")]
    /// <summary>
    /// Vertical camera movement speed.
    /// </summary>
    public float m_sensitivityY = 10f;
    // Horizontal camera rotation.
    [Tooltip("Minimum camera turn angle.")]
    /// <summary>
    /// Minimum camera rotation angle.
    /// </summary>
    public float m_minimumX = -360f;
    [Tooltip("Maximum camera turn angle.")]
    /// <summary>
    /// Maximum camera rotation angle.
    /// </summary>
    public float m_maximumX = 360f;
    // Vertical camera rotation, limited here to a maximum elevation of 45 degrees.
    [Tooltip("Minimum camera elevation angle.")]
    /// <summary>
    /// Minimum vertical camera angle.
    /// </summary>
    public float m_minimumY = -45f;
    [Tooltip("Maximum camera elevation angle.")]
    /// <summary>
    /// Maximum vertical camera angle.
    /// </summary>
    public float m_maximumY = 45f;

    private float m_rotationY = 0f;
    private Vector3 prevMousePos = Vector3.zero;
    private Vector3 moveCamTargetPos = Vector3.zero;
    private Vector3 moveCamTargetLookPos = Vector3.zero;
    private bool moveCam = false;
    private Vector3 currentVelocity = Vector3.zero;
    private Camera mainCam;
#if (UNITY_IPHONE || UNITY_ANDROID) && !UNITY_EDITOR
    private int lastDownTouchId = 0;
#endif

    private void Awake()
    {
      mainCam = GetComponent<Camera>();
    }
    private void Start()
    {
      Instance = this;
      AudioListener = GetComponent<AudioListener>();
    }
    private void Update()
    {
      if (Wireframe != _Wireframe) UpdateWireframe();
      if (Audio != _Audio) UpdateAudio();
      if (Fog != _Fog) UpdateFog();
      if (SkyBox != _SkyBox) UpdateSkyBox();


      // Do not drag through GUI controls.
      if (!EventSystem.current.IsPointerOverGameObject() && GUIUtility.hotControl == 0)
      {
        const bool mouseLeftMoveCamera = true;

        // Pan with the left or middle mouse button.
        //===============================

        if ((mouseLeftMoveCamera && Input.GetMouseButton(0)) || Input.GetMouseButton(2))
        {
          Vector3 p0 = transform.position;
          Vector3 p01 = p0 - transform.right * Input.GetAxisRaw("Mouse X") * m_moveSensitivityX * Time.timeScale;
          transform.position = p01 - transform.up * Input.GetAxisRaw("Mouse Y") * m_moveSensitivityY * Time.timeScale;

          Cursor.SetCursor(cursorDragHandTexture, Vector2.zero, CursorMode.Auto);
        }

        // Rotate with the right mouse button.
        //===============================

#if (UNITY_IPHONE || UNITY_ANDROID) && !UNITY_EDITOR
        // Rotate with two fingers on mobile.
        else if (Input.touchCount > 0 && lastDownTouchId >= 0)
        {
            for (int i = 0; i < Input.touches.Length; i++)
            {
                if (Input.touches[i].fingerId == lastDownTouchId && Input.touches[i].phase == TouchPhase.Moved)
                {
                    float m_rotationX = transform.localEulerAngles.y + Input.touches[i].deltaPosition.x * Time.deltaTime * m_sensitivityX;
                    m_rotationY += Input.touches[i].deltaPosition.y * Time.deltaTime * m_sensitivityY;
                    m_rotationY = Mathf.Clamp(m_rotationY, m_minimumY, m_maximumY);
                    transform.localEulerAngles = new Vector3(-m_rotationY, m_rotationX, 0);
                    break;
                }
            }
        }
        else lastDownTouchId = 0;
#else
        else if (Input.GetMouseButton(1))
        {
          float m_rotationX = transform.localEulerAngles.y + Input.GetAxis("Mouse X") * m_sensitivityX;
          m_rotationY += Input.GetAxis("Mouse Y") * m_sensitivityY;
          m_rotationY = Mathf.Clamp(m_rotationY, m_minimumY, m_maximumY);

          transform.localEulerAngles = new Vector3(-m_rotationY, m_rotationX, 0);

          Cursor.SetCursor(cursorDragEyeFreeTexture, Vector2.zero, CursorMode.Auto);
        }
        if (Input.GetMouseButtonUp(1) || Input.GetMouseButtonUp(0))
        {
          Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        }
#endif  
        // While the right mouse button is held.
        if (Input.GetMouseButton(1))
        {
          // Space raises the camera.
          if (Input.GetKey(KeyCode.Q))
          {
            transform.Translate(Vector3.up * cameraSpeed * Time.deltaTime, Space.Self);
          }
          if (Input.GetKey(KeyCode.E))
          {
            transform.Translate(Vector3.down * cameraSpeed * Time.deltaTime, Space.Self);
          }
          // W key.
          if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
          {
            this.gameObject.transform.Translate(new Vector3(0, 0, cameraSpeed * Time.deltaTime));
          }
          // S key.
          if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
          {
            this.gameObject.transform.Translate(new Vector3(0, 0, -cameraSpeed * Time.deltaTime));
          }
          // A key.
          if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
          {
            this.gameObject.transform.Translate(new Vector3(-(cameraSpeed) * Time.deltaTime, 0, 0));
          }
          // D key.
          if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
          {
            this.gameObject.transform.Translate(new Vector3((cameraSpeed) * Time.deltaTime, 0, 0));
          }

          // Increase movement speed.
          if (Input.GetAxis("Mouse ScrollWheel") < 0)
          {
            if (cameraSpeed < 1)
              cameraSpeed -= 0.01f;
            else if (cameraSpeed < 5)
              cameraSpeed -= 0.2f;
            else if (cameraSpeed < 10)
              cameraSpeed -= 2f;
            else if (cameraSpeed < 150)
              cameraSpeed -= 10f;
            else if (cameraSpeed < 500)
              cameraSpeed -= 50f;
            else 
              cameraSpeed -= 100f;

            onCamSpeedChanged?.Invoke();
          }
          // Decrease movement speed.
          if (Input.GetAxis("Mouse ScrollWheel") > 0)
          {
            if (cameraSpeed < 1)
              cameraSpeed += 0.01f;
            else if (cameraSpeed < 10)
              cameraSpeed += 0.2f;
            else if (cameraSpeed < 20)
              cameraSpeed += 2f;
            else if (cameraSpeed < 150)
              cameraSpeed += 10f;
            else if (cameraSpeed < 500)
              cameraSpeed += 50f;
            else cameraSpeed += 100f;

            onCamSpeedChanged?.Invoke();
          }
        }
        else
        {
          // Move backward to zoom out.
          if (Input.GetAxis("Mouse ScrollWheel") < 0)
          {
            this.gameObject.transform.Translate(new Vector3(0, 0, cameraSpeed * -m_moveSensitivityZoom * Time.deltaTime));
          }
          // Move forward to zoom in.
          if (Input.GetAxis("Mouse ScrollWheel") > 0)
          {
            this.gameObject.transform.Translate(new Vector3(0, 0, cameraSpeed * m_moveSensitivityZoom * Time.deltaTime));
          }

        }
      }
      
    }
    private void LateUpdate()
    {
      if (moveCam)
      {
        transform.position = Vector3.SmoothDamp(transform.position, moveCamTargetPos, ref currentVelocity, 0.2f);
        transform.LookAt(moveCamTargetLookPos);
      }
    }

    private void OnPreRender()
    {
      if (_Wireframe)
        GL.wireframe = true;
    }
    private void OnPostRender()
    {
      if (_Wireframe)
        GL.wireframe = false;
    }

  }
}