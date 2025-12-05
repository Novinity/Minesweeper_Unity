using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class GridZoom : MonoBehaviour
{
    public static GridZoom instance {get; private set;}

    private InputSystem_Actions inputActions;

    public float scrollSpeed = 0.1f;
    public float minScale = 1f;

    public float maxScale = 3f;

    public float moveSpeed = 4f;

    public RectTransform rectTransform;

    Vector2 prevMousePos;
    private bool moving = false;

    void Awake()
    {
        instance = this;
    }

    void OnEnable()
    {
        if (inputActions != null) inputActions.Enable();
    }

    void OnDisable()
    {
        if (inputActions != null) inputActions.Disable();
    }

    void Start()
    {
        inputActions = new InputSystem_Actions();
        inputActions.Player.Scroll.performed += OnScrollPerformed;

        inputActions.Player.MiddleClick.performed += delegate
        {
            moving = true;
        };
        inputActions.Player.MiddleClick.canceled += delegate
        {
            prevMousePos = new Vector2();
            moving = false;
        };

        inputActions.Enable();
    }

    void Update()
    {
        if (!moving || rectTransform.localScale == Vector3.one * minScale) return;
        if (prevMousePos != Vector2.zero)
        {
            Vector2 delta = inputActions.Player.MousePosition.ReadValue<Vector2>() - prevMousePos;
            rectTransform.position += new Vector3(delta.x, delta.y) * moveSpeed * Time.deltaTime;
            prevMousePos = Vector2.zero;
        }
        prevMousePos = inputActions.Player.MousePosition.ReadValue<Vector2>();
    }

    private void OnScrollPerformed(InputAction.CallbackContext context)
    {
        float delta = context.ReadValue<float>();

        if (Mathf.Approximately(delta, 0f)) return;

        float oldScale = rectTransform.localScale.x;
        float newScale = Mathf.Clamp(oldScale + delta * scrollSpeed, minScale, maxScale);
        float scaleFactor = newScale / oldScale;

        if (Mathf.Approximately(scaleFactor, 1f))
            return;

        Vector2 mouseScreenPos = inputActions.Player.MousePosition.ReadValue<Vector2>() - new Vector2(Screen.width / 2, Screen.height / 2);

        Camera cam = Camera.main;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform,
            mouseScreenPos,
            cam,
            out Vector2 localPoint
        );

        // World position of that local point BEFORE scaling
        Vector3 worldPosBefore = rectTransform.TransformPoint(mouseScreenPos);

        // Apply scale
        rectTransform.localScale = Vector3.one * newScale;

        // World position of the SAME local point AFTER scaling
        Vector3 worldPosAfter = rectTransform.TransformPoint(mouseScreenPos);

        // Shift rect so the point under the cursor stays fixed
        Vector3 worldDelta = worldPosBefore - worldPosAfter;
        rectTransform.position += worldDelta;

        if (newScale == minScale) ResetZoom();
    }

    public void ResetZoom()
    {
        rectTransform.localPosition = Vector3.zero;
        rectTransform.localScale = Vector3.one * minScale;
    }
}
