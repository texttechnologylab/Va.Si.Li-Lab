

using Oculus.Interaction;
using UnityEngine;

/// <summary>
/// Overrides the radial gradient background opacity of RayInteractorCursorVisual
/// by subscribing to the same events and applying custom material properties after the base class updates.
/// Attach this component to the same GameObject as RayInteractorCursorVisual.
/// </summary>
[RequireComponent(typeof(RayInteractorCursorVisual))]
public class RayInteractorCursorVisualOverride : MonoBehaviour
{
    [SerializeField]
    private float _shaderRadialGradientBackgroundOpacityOverride = 0.005f;
    [SerializeField]
    private float _shaderOutlineWidthOverride = 0.2f;

    private int _shaderRadialGradientBackgroundOpacity = Shader.PropertyToID("_RadialGradientBackgroundOpacity");
    
    private int _shaderOutlineWidth = Shader.PropertyToID("_OutlineWidth");
    
    private RayInteractorCursorVisual _cursorVisual;
    private RayInteractor _rayInteractor;
    private Renderer _renderer;
    private bool _started = false;

    private void Start()
    {
        _cursorVisual = GetComponent<RayInteractorCursorVisual>();
        
        // Use reflection to access private fields from RayInteractorCursorVisual
        var rayInteractorField = typeof(RayInteractorCursorVisual).GetField("_rayInteractor", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        _rayInteractor = rayInteractorField?.GetValue(_cursorVisual) as RayInteractor;
        
        var rendererField = typeof(RayInteractorCursorVisual).GetField("_renderer", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        _renderer = rendererField?.GetValue(_cursorVisual) as Renderer;
        
        _started = true;
    }

    private void LateUpdate()
    {
        // Apply override every frame after all other updates
        ApplyOverride();
    }

    private void ApplyOverride()
    {
        if (_renderer != null && _renderer.enabled && _renderer.material != null)
        {
            _renderer.material.SetFloat(_shaderRadialGradientBackgroundOpacity, _shaderRadialGradientBackgroundOpacityOverride);
            _renderer.material.SetFloat(_shaderOutlineWidth, _shaderOutlineWidthOverride);
        }
    }
}