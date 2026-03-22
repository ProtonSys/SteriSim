using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Trigger de passo de manutenção para VR.
/// Colocar nos GameObjects interativos do modelo 3D (ex: resistência, válvula).
/// Quando o utilizador interage com o objeto no passo correto, avança o procedimento.
///
/// CORREÇÕES:
///   - Substituído MaintenanceSequence (não existia) por MaintenanceManager
///   - Trocado selectEntered (grab) por activated (toque/press) — mais adequado
///   - Adicionado feedback visual e validação de procedimento ativo
///
/// SETUP:
///   1. Adicionar este script ao GameObject do componente interativo
///   2. Garantir que tem Collider e XRSimpleInteractable
///   3. Definir codigoProcedimento e indicePassoNecessario no Inspector
/// </summary>
[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(XRSimpleInteractable))]
public class MaintenanceStepTrigger : MonoBehaviour
{
    [Header("Procedimento")]
    [Tooltip("Código do procedimento ao qual este trigger pertence (ex: MANT-02)")]
    [SerializeField] private string codigoProcedimento;

    [Tooltip("Índice do passo que este objeto representa (começa em 0)")]
    [SerializeField] private int indicePassoNecessario = 0;

    [Header("Feedback Visual")]
    [Tooltip("Highlighter deste componente — para destaque quando é o passo ativo")]
    [SerializeField] private ComponentHighlighter highlighter;

    [Tooltip("Material/cor quando a interação é bem-sucedida")]
    [SerializeField] private Color corSucesso = new Color(0.2f, 0.9f, 0.3f);

    [Tooltip("Material/cor quando a interação é feita no passo errado")]
    [SerializeField] private Color corErro    = new Color(0.9f, 0.2f, 0.2f);

    [Tooltip("Duração do flash de feedback (segundos)")]
    [SerializeField] private float duracaoFlash = 0.5f;

    // ── Internos ──────────────────────────────────────────────
    private XRSimpleInteractable _interactable;
    private Renderer             _renderer;
    private Color                _corOriginal;
    private bool                 _jaInteragido = false; // evitar duplo trigger

    // ── Ciclo de vida ─────────────────────────────────────────

    private void Awake()
    {
        _interactable = GetComponent<XRSimpleInteractable>();
        _renderer     = GetComponent<Renderer>();

        if (_renderer != null)
            _corOriginal = _renderer.material.color;
    }

    private void OnEnable()
    {
        // CORREÇÃO: usar 'activated' em vez de 'selectEntered'.
        //
        // selectEntered → dispara quando o utilizador AGARRA o objeto (grab)
        // activated     → dispara quando o utilizador pressiona/toca (mais natural
        //                 para interações de manutenção como "tocar na resistência")
        if (_interactable != null)
            _interactable.activated.AddListener(AoInteragir);

        // Subscrever o passo ativo para saber quando destacar este objeto
        if (MaintenanceManager.Instance != null)
            MaintenanceManager.Instance.OnPassoAtivado += AoPassoAtivado;
    }

    private void OnDisable()
    {
        if (_interactable != null)
            _interactable.activated.RemoveListener(AoInteragir);

        if (MaintenanceManager.Instance != null)
            MaintenanceManager.Instance.OnPassoAtivado -= AoPassoAtivado;
    }

    // ── Lógica ────────────────────────────────────────────────

    /// <summary>
    /// Chamado quando o utilizador interage com o objeto em VR.
    /// Verifica se é o procedimento e passo corretos antes de avançar.
    /// </summary>
    private void AoInteragir(ActivateEventArgs args)
    {
        // Ignorar se já foi interagido neste passo
        if (_jaInteragido) return;

        // Verificar se há um procedimento ativo
        if (!MaintenanceManager.Instance.EmAndamento)
        {
            Debug.Log("[MaintenanceStepTrigger] Nenhum procedimento ativo.");
            MostrarFlash(corErro);
            return;
        }

        var procAtual = MaintenanceManager.Instance.ProcedimentoAtual;

        // Verificar se é o procedimento correto
        if (procAtual.codigoProcedimento != codigoProcedimento)
        {
            Debug.Log($"[MaintenanceStepTrigger] Procedimento errado. " +
                      $"Esperado: {codigoProcedimento}, Ativo: {procAtual.codigoProcedimento}");
            MostrarFlash(corErro);
            return;
        }

        // Verificar se é o passo correto
        if (MaintenanceManager.Instance.IndicePassoAtual != indicePassoNecessario)
        {
            Debug.Log($"[MaintenanceStepTrigger] Passo errado. " +
                      $"Esperado: {indicePassoNecessario}, " +
                      $"Atual: {MaintenanceManager.Instance.IndicePassoAtual}");
            MostrarFlash(corErro);
            return;
        }

        // Tudo correto — avançar o procedimento
        _jaInteragido = true;
        MostrarFlash(corSucesso);
        MaintenanceManager.Instance.ConcluirPassoAtual();

        Debug.Log($"[MaintenanceStepTrigger] Passo {indicePassoNecessario} " +
                  $"concluído via interação VR.");
    }

    /// <summary>
    /// Chamado quando o passo ativo muda.
    /// Ativa/desativa o destaque consoante se é o passo deste trigger.
    /// </summary>
    private void AoPassoAtivado(PastoManutencao passo, int indice)
    {
        bool estePassoEstaAtivo = indice == indicePassoNecessario
            && MaintenanceManager.Instance.ProcedimentoAtual?.codigoProcedimento
               == codigoProcedimento;

        // Resetar flag quando este passo fica ativo novamente
        if (estePassoEstaAtivo)
            _jaInteragido = false;

        // Ativar/desativar destaque visual
        if (highlighter != null)
        {
            if (estePassoEstaAtivo)
                highlighter.Ativar();
            else
                highlighter.Desativar();
        }
    }

    /// <summary>
    /// Mostra um flash de cor no renderer para dar feedback imediato ao utilizador.
    /// Usa uma Coroutine para voltar à cor original após a duração definida.
    /// </summary>
    private void MostrarFlash(Color cor)
    {
        if (_renderer == null) return;
        StartCoroutine(CorotinaFlash(cor));
    }

    private System.Collections.IEnumerator CorotinaFlash(Color cor)
    {
        // Aplicar cor de feedback
        _renderer.material.color = cor;

        // Aguardar a duração do flash
        yield return new WaitForSeconds(duracaoFlash);

        // Restaurar cor original
        _renderer.material.color = _corOriginal;
    }
}
