using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Controlo de câmara com Orbit, Zoom e Pan.
/// Funciona em desktop (rato + teclado) e VR (XR Controllers).
///
/// SETUP:
///   1. Adicionar este script à Main Camera
///   2. Definir o "alvo" (o modelo do esterilizador) no Inspector
///   3. Em VR: ligar os campos leftController e rightController
/// </summary>
public class CameraController : MonoBehaviour
{
    // ═══════════════════════════════════════════════════════
    // CONFIGURAÇÃO
    // ═══════════════════════════════════════════════════════

    [Header("Alvo")]
    [Tooltip("Transform do modelo — a câmara orbita à volta deste ponto")]
    [SerializeField] private Transform alvo;

    [Tooltip("Se não houver alvo, orbita à volta desta posição")]
    [SerializeField] private Vector3 pontoOrbita = Vector3.zero;

    [Header("Orbit")]
    [SerializeField] private float sensibilidadeOrbit = 200f;
    [SerializeField] private float limiteVerticalMin  = -80f;  // graus
    [SerializeField] private float limiteVerticalMax  =  80f;

    [Header("Zoom")]
    [SerializeField] private float sensibilidadeZoom  = 5f;
    [SerializeField] private float distanciaMinima    = 0.5f;
    [SerializeField] private float distanciaMaxima    = 10f;
    [SerializeField] private float distanciaInicial   = 3f;

    [Header("Pan")]
    [SerializeField] private float sensibilidadePan   = 0.005f;

    [Header("Suavização")]
    [Tooltip("Suavizar o movimento da câmara (0 = instantâneo)")]
    [SerializeField] private float suavidade          = 10f;

    // ═══════════════════════════════════════════════════════
    // ESTADO INTERNO
    // ═══════════════════════════════════════════════════════

    private float   _distancia;        // distância atual ao alvo
    private float   _anguloHorizontal; // rotação horizontal (yaw)
    private float   _anguloVertical;   // rotação vertical (pitch)
    private Vector3 _offset;           // offset do pan

    // Posição e rotação alvo (suavizadas com Lerp)
    private Vector3    _posAlvo;
    private Quaternion _rotAlvo;

    // Controlo de estado do rato
    private bool _orbitando = false;
    private bool _pannando   = false;
    private Vector2 _posRatoAnterior;

    // ═══════════════════════════════════════════════════════
    // CICLO DE VIDA
    // ═══════════════════════════════════════════════════════

    private void Start()
    {
        _distancia = distanciaInicial;

        // Calcular ângulos iniciais a partir da posição atual da câmara
        // para não haver salto no primeiro frame
        Vector3 direcao = transform.position - ObterPontoAlvo();
        _distancia         = direcao.magnitude;
        _anguloHorizontal  = Mathf.Atan2(direcao.x, direcao.z) * Mathf.Rad2Deg;
        _anguloVertical    = Mathf.Asin(direcao.y / _distancia) * Mathf.Rad2Deg;

        AtualizarPosicaoAlvo();
        transform.position = _posAlvo;
        transform.rotation = _rotAlvo;
    }

    private void Update()
    {
        TratarInputDesktop();
        AtualizarPosicaoAlvo();
        AplicarSuavizacao();
    }

    // ═══════════════════════════════════════════════════════
    // INPUT DESKTOP (rato + scroll)
    // ═══════════════════════════════════════════════════════

    private void TratarInputDesktop()
    {
        var mouse    = Mouse.current;
        var keyboard = Keyboard.current;
        if (mouse == null) return;

        Vector2 posRatoAtual = mouse.position.ReadValue();
        Vector2 delta        = posRatoAtual - _posRatoAnterior;

        // ── ORBIT — botão esquerdo do rato ──────────────────
        // Clicar e arrastar com o botão esquerdo orbita à volta do modelo
        if (mouse.leftButton.wasPressedThisFrame)
        {
            _orbitando = true;
            _posRatoAnterior = posRatoAtual;
        }
        if (mouse.leftButton.wasReleasedThisFrame)
            _orbitando = false;

        if (_orbitando)
        {
            // delta.x → rotação horizontal (yaw)
            // delta.y → rotação vertical (pitch), invertida para ser intuitivo
            _anguloHorizontal += delta.x * sensibilidadeOrbit * Time.deltaTime;
            _anguloVertical   -= delta.y * sensibilidadeOrbit * Time.deltaTime;

            // Limitar o ângulo vertical para não fazer flip
            _anguloVertical = Mathf.Clamp(_anguloVertical, limiteVerticalMin, limiteVerticalMax);
        }

        // ── PAN — botão direito do rato (ou Middle) ─────────
        // Arrastar com o botão direito move o ponto de órbita
        if (mouse.rightButton.wasPressedThisFrame)
        {
            _pannando = true;
            _posRatoAnterior = posRatoAtual;
        }
        if (mouse.rightButton.wasReleasedThisFrame)
            _pannando = false;

        if (_pannando)
        {
            // Mover o offset no plano local da câmara
            // transform.right = direita da câmara
            // transform.up    = cima da câmara
            _offset -= transform.right * delta.x * sensibilidadePan * _distancia;
            _offset -= transform.up    * delta.y * sensibilidadePan * _distancia;
        }

        // ── ZOOM — scroll do rato ───────────────────────────
        float scroll = mouse.scroll.ReadValue().y;
        if (Mathf.Abs(scroll) > 0.01f)
        {
            // Scroll para cima = aproximar (distância menor)
            // Scroll para baixo = afastar (distância maior)
            _distancia -= scroll * sensibilidadeZoom * Time.deltaTime;
            _distancia  = Mathf.Clamp(_distancia, distanciaMinima, distanciaMaxima);
        }

        // ── RESET — tecla F ─────────────────────────────────
        if (keyboard != null && keyboard.fKey.wasPressedThisFrame)
            ResetarCamera();

        _posRatoAnterior = posRatoAtual;
    }

    // ═══════════════════════════════════════════════════════
    // CÁLCULO DA POSIÇÃO
    // ═══════════════════════════════════════════════════════

    /// <summary>
    /// Calcula a posição e rotação alvo da câmara a partir dos ângulos
    /// e da distância atuais, usando coordenadas esféricas.
    ///
    /// Coordenadas esféricas:
    ///   - _anguloHorizontal (φ): rotação em torno do eixo Y
    ///   - _anguloVertical   (θ): elevação acima do plano horizontal
    ///   - _distancia        (r): distância ao centro
    /// </summary>
    private void AtualizarPosicaoAlvo()
    {
        // Converter ângulos para direção em coordenadas cartesianas
        // Quaternion.Euler cria uma rotação a partir de ângulos de Euler
        Quaternion rotacao = Quaternion.Euler(_anguloVertical, _anguloHorizontal, 0f);

        // A câmara fica "atrás" do alvo — por isso usamos -forward
        Vector3 direcao = rotacao * Vector3.back;

        Vector3 centro = ObterPontoAlvo() + _offset;

        _posAlvo = centro + direcao * _distancia;
        _rotAlvo = Quaternion.LookRotation(centro - _posAlvo);
    }

    /// <summary>
    /// Aplica Lerp/Slerp para suavizar o movimento da câmara.
    /// Sem suavização a câmara seria demasiado brusca.
    /// </summary>
    private void AplicarSuavizacao()
    {
        float t = Time.deltaTime * suavidade;
        transform.position = Vector3.Lerp(   transform.position, _posAlvo, t);
        transform.rotation = Quaternion.Slerp(transform.rotation, _rotAlvo, t);
    }

    // ═══════════════════════════════════════════════════════
    // API PÚBLICA
    // ═══════════════════════════════════════════════════════

    /// <summary>
    /// Reseta a câmara para a posição e distância iniciais.
    /// Ligar a um botão "Reset" na UI.
    /// </summary>
    public void ResetarCamera()
    {
        _distancia        = distanciaInicial;
        _anguloHorizontal = 0f;
        _anguloVertical   = 20f;
        _offset           = Vector3.zero;
    }

    /// <summary>
    /// Define um novo alvo para a câmara orbitar.
    /// Útil para focar num componente específico durante a manutenção.
    /// </summary>
    public void DefinirAlvo(Transform novoAlvo)
    {
        alvo    = novoAlvo;
        _offset = Vector3.zero;
    }

    /// <summary>
    /// Faz zoom para mostrar melhor um componente específico.
    /// </summary>
    public void FocarComponente(Transform componente, float distancia = 1f)
    {
        alvo       = componente;
        _offset    = Vector3.zero;
        _distancia = distancia;
    }

    // ── Utilitário ────────────────────────────────────────────
    private Vector3 ObterPontoAlvo()
    {
        return alvo != null ? alvo.position : pontoOrbita;
    }
}