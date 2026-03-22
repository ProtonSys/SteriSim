using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Inicializa e atualiza a barra de progresso do procedimento de manutenção.
/// Subscreve os eventos do MaintenanceManager para se atualizar automaticamente.
///
/// SETUP:
///   - Coloca este script no GameObject da barra de progresso
///   - ProgressBarFill deve ser uma Image com Image Type = Filled + Fill Method = Horizontal
///   - Ligar os campos no Inspector
/// </summary>
public class ProgressBarInitializer : MonoBehaviour
{
    [Header("Barra de Progresso")]
    [Tooltip("Image com Image Type = Filled e Fill Method = Horizontal")]
    [SerializeField] private Image progressBarFill;

    [Header("Textos (opcional)")]
    [SerializeField] private TMP_Text textProgresso;   // ex: "Passo 3 / 7"
    [SerializeField] private TMP_Text textPercentagem; // ex: "42%"

    [Header("Cores")]
    [SerializeField] private Color corInicio    = new Color(0.9f, 0.3f, 0.2f); // vermelho
    [SerializeField] private Color corMeio      = new Color(1f,   0.8f, 0.1f); // amarelo
    [SerializeField] private Color corFim       = new Color(0.2f, 0.8f, 0.3f); // verde

    [Header("Animação")]
    [Tooltip("Suavizar a transição da barra (0 = instantâneo)")]
    [SerializeField] private float velocidadeAnimacao = 5f;

    // fillAmount alvo — a barra anima até este valor
    private float _fillAlvo = 0f;

    // ── Ciclo de vida ─────────────────────────────────────────

    private void Start()
    {
        InicializarBarra();

        // Verificar se o MaintenanceManager já existe antes de subscrever
        if (MaintenanceManager.Instance != null)
        {
            MaintenanceManager.Instance.OnProcedimentoIniciado  += AoProcedimentoIniciado;
            MaintenanceManager.Instance.OnPassoAtivado          += AoPassoAtivado;
            MaintenanceManager.Instance.OnProcedimentoConcluido += AoProcedimentoConcluido;
        }
        else
        {
            Debug.LogWarning("[ProgressBar] MaintenanceManager não encontrado na cena.");
        }
    }

    private void OnDestroy()
    {
        if (MaintenanceManager.Instance == null) return;
        MaintenanceManager.Instance.OnProcedimentoIniciado  -= AoProcedimentoIniciado;
        MaintenanceManager.Instance.OnPassoAtivado          -= AoPassoAtivado;
        MaintenanceManager.Instance.OnProcedimentoConcluido -= AoProcedimentoConcluido;
    }

    private void Update()
    {
        if (progressBarFill == null) return;

        // Animar suavemente o fillAmount até ao valor alvo
        // Lerp com deltaTime garante animação independente do framerate
        progressBarFill.fillAmount = Mathf.Lerp(
            progressBarFill.fillAmount,
            _fillAlvo,
            Time.deltaTime * velocidadeAnimacao
        );

        // Atualizar cor gradualmente conforme o progresso
        // Gradient de 3 cores: vermelho → amarelo → verde
        progressBarFill.color = CalcularCorProgresso(progressBarFill.fillAmount);
    }

    // ── Inicialização ─────────────────────────────────────────

    private void InicializarBarra()
    {
        if (progressBarFill == null)
        {
            Debug.LogWarning("[ProgressBar] progressBarFill não atribuído no Inspector.");
            return;
        }

        // CORREÇÃO: usar fillAmount em vez de sizeDelta.
        // sizeDelta depende dos anchors e pode partir com layouts diferentes.
        // fillAmount é a forma correta para imagens do tipo Filled.
        progressBarFill.fillAmount = 0f;
        progressBarFill.color      = corInicio;

        _fillAlvo = 0f;

        AtualizarTextos(0, 0);
    }

    // ── Callbacks dos eventos ─────────────────────────────────

    private void AoProcedimentoIniciado(MaintenanceProcedure proc)
    {
        // Reiniciar a barra quando um procedimento começa
        _fillAlvo = 0f;
        AtualizarTextos(0, proc.passos.Count);
    }

    private void AoPassoAtivado(PastoManutencao passo, int indice)
    {
        int total = MaintenanceManager.Instance.ProcedimentoAtual?.passos.Count ?? 1;

        // Calcular progresso: passo concluídos / total
        // Usamos indice (0-based) para representar quantos passos já foram concluídos
        _fillAlvo = (float)indice / total;

        AtualizarTextos(indice + 1, total);
    }

    private void AoProcedimentoConcluido(MaintenanceProcedure proc)
    {
        // Barra a 100% ao concluir
        _fillAlvo = 1f;
        AtualizarTextos(proc.passos.Count, proc.passos.Count);
    }

    // ── Utilitários ───────────────────────────────────────────

    private void AtualizarTextos(int passoAtual, int total)
    {
        if (textProgresso != null)
            textProgresso.text = total > 0
                ? $"Passo {passoAtual} / {total}"
                : "—";

        if (textPercentagem != null)
        {
            float pct = total > 0 ? (float)passoAtual / total * 100f : 0f;
            textPercentagem.text = $"{pct:F0}%";
        }
    }

    /// <summary>
    /// Calcula a cor da barra num gradiente de 3 pontos:
    ///   0%  → corInicio  (vermelho)
    ///   50% → corMeio    (amarelo)
    ///   100%→ corFim     (verde)
    /// </summary>
    private Color CalcularCorProgresso(float t)
    {
        // t está entre 0 e 1
        if (t < 0.5f)
            // Primeira metade: corInicio → corMeio
            // Remapear t de [0, 0.5] para [0, 1]
            return Color.Lerp(corInicio, corMeio, t * 2f);
        else
            // Segunda metade: corMeio → corFim
            // Remapear t de [0.5, 1] para [0, 1]
            return Color.Lerp(corMeio, corFim, (t - 0.5f) * 2f);
    }
}
