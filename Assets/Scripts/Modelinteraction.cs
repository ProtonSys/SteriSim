using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Move, roda e faz zoom ao modelo 3D com o rato.
/// Colocar este script no GameObject raiz do modelo (SteamSterilizer).
///
/// Controlos:
///   Botão esquerdo + arrastar → rodar
///   Botão direito  + arrastar → mover (pan)
///   Scroll                    → zoom (escala)
/// </summary>
public class ModelInteraction : MonoBehaviour
{
    [Header("Rotação")]
    [SerializeField] private float sensibilidadeRotacao = 0.3f;

    [Header("Pan")]
    [SerializeField] private float sensibilidadePan = 0.005f;

    [Header("Zoom")]
    [SerializeField] private float sensibilidadeZoom = 0.1f;
    [SerializeField] private float escalaMinima      = 0.2f;
    [SerializeField] private float escalaMaxima      = 3f;

    [Header("Suavização")]
    [SerializeField] private float suavidade = 10f;

    // Estado alvo (suavizado)
    private Vector3    _posAlvo;
    private Quaternion _rotAlvo;
    private Vector3    _escalaAlvo;

    // Estado do rato
    private Vector2 _posAnterior;
    private bool    _aRodar = false;
    private bool    _aPan   = false;

    private void Start()
    {
        _posAlvo   = transform.position;
        _rotAlvo   = transform.rotation;
        _escalaAlvo = transform.localScale;
    }

    private void Update()
    {
        TratarInput();
        AplicarSuavizacao();
    }

    private void TratarInput()
    {
        var mouse = Mouse.current;
        if (mouse == null) return;

        Vector2 posAtual = mouse.position.ReadValue();
        Vector2 delta    = posAtual - _posAnterior;

        // ── Rotação — botão esquerdo ──────────────────────────
        if (mouse.leftButton.wasPressedThisFrame)
        {
            _aRodar    = true;
            _posAnterior = posAtual;
        }
        if (mouse.leftButton.wasReleasedThisFrame)
            _aRodar = false;

        if (_aRodar)
        {
            // Rodar em torno do eixo Y (horizontal) e X (vertical)
            float rotY =  delta.x * sensibilidadeRotacao;
            float rotX = -delta.y * sensibilidadeRotacao;

            // Aplicar rotação no espaço do mundo para não fazer flip
            _rotAlvo = Quaternion.AngleAxis(rotY, Vector3.up)
                     * Quaternion.AngleAxis(rotX, transform.right)
                     * _rotAlvo;
        }

        // ── Pan — botão direito ───────────────────────────────
        if (mouse.rightButton.wasPressedThisFrame)
        {
            _aPan      = true;
            _posAnterior = posAtual;
        }
        if (mouse.rightButton.wasReleasedThisFrame)
            _aPan = false;

        if (_aPan)
        {
            // Mover no plano XY local do modelo
            Vector3 movimento = new Vector3(
                -delta.x * sensibilidadePan,
                -delta.y * sensibilidadePan,
                0f
            );
            _posAlvo += transform.TransformDirection(movimento);
        }

        // ── Zoom — scroll ─────────────────────────────────────
        float scroll = mouse.scroll.ReadValue().y;
        if (Mathf.Abs(scroll) > 0.01f)
        {
            float novaEscala = _escalaAlvo.x + scroll * sensibilidadeZoom;
            novaEscala = Mathf.Clamp(novaEscala, escalaMinima, escalaMaxima);
            _escalaAlvo = Vector3.one * novaEscala;
        }

        _posAnterior = posAtual;
    }

    private void AplicarSuavizacao()
    {
        float t = Time.deltaTime * suavidade;
        transform.position   = Vector3.Lerp(   transform.position,   _posAlvo,   t);
        transform.rotation   = Quaternion.Slerp(transform.rotation,   _rotAlvo,   t);
        transform.localScale = Vector3.Lerp(   transform.localScale, _escalaAlvo, t);
    }

    /// <summary>Reset à posição, rotação e escala iniciais.</summary>
    public void Reset()
    {
        _posAlvo    = Vector3.zero;
        _rotAlvo    = Quaternion.identity;
        _escalaAlvo = Vector3.one;
    }
}