using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Gere o ciclo de vida de um procedimento de manutenção.
/// Versão standalone — não depende do SterilizadorManager.
/// </summary>
public class MaintenanceManager : MonoBehaviour
{
    
    [SerializeField] private GameObject menuUI;
    [SerializeField] private GameObject procedimentoUI;
    
    public static MaintenanceManager Instance { get; private set; }

    // ── Eventos ───────────────────────────────────────────────
    public event System.Action<MaintenanceProcedure>  OnProcedimentoIniciado;
    public event System.Action<PastoManutencao, int>  OnPassoAtivado;
    public event System.Action<int>                   OnPassoConcluido;
    public event System.Action<MaintenanceProcedure>  OnProcedimentoConcluido;

    // ── Estado atual ──────────────────────────────────────────
    public MaintenanceProcedure ProcedimentoAtual { get; private set; }
    public int                  IndicePassoAtual  { get; private set; }
    public bool                 EmAndamento       { get; private set; }

    private bool[]                 _passosConcluidos;
    private Dictionary<int, float> _medicoes = new Dictionary<int, float>();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ── API Pública ───────────────────────────────────────────

    public void IniciarProcedimento(MaintenanceProcedure procedimento)
    {
        if (EmAndamento)
        {
            Debug.LogWarning("[MaintenanceManager] Já existe um procedimento em andamento.");
            return;
        }

        ProcedimentoAtual = procedimento;
        IndicePassoAtual  = 0;
        EmAndamento       = true;
        _passosConcluidos = new bool[procedimento.passos.Count];
        _medicoes.Clear();

        Debug.Log($"[MaintenanceManager] Procedimento iniciado: {procedimento.nomeProcedimento}");

        OnProcedimentoIniciado?.Invoke(procedimento);
        AtivarPasso(0);
    }

    public void ConcluirPassoAtual(float medicao = 0f)
    {
        if (!EmAndamento) return;

        var passo = ProcedimentoAtual.passos[IndicePassoAtual];
        if (passo.requerMedicao)
            _medicoes[IndicePassoAtual] = medicao;

        _passosConcluidos[IndicePassoAtual] = true;
        OnPassoConcluido?.Invoke(IndicePassoAtual);

        Debug.Log($"[MaintenanceManager] Passo {IndicePassoAtual + 1} concluído.");

        int proximo = IndicePassoAtual + 1;
        if (proximo >= ProcedimentoAtual.passos.Count)
            ConcluirProcedimento();
        else
        {
            IndicePassoAtual = proximo;
            AtivarPasso(IndicePassoAtual);
        }
    }

    public void RecuarPasso()
    {
        if (IndicePassoAtual > 0)
        {
            IndicePassoAtual--;
            AtivarPasso(IndicePassoAtual);
        }
    }

    public void CancelarProcedimento()
    {
        if (!EmAndamento) return;
        Debug.Log($"[MaintenanceManager] Procedimento cancelado.");
        ResetarEstado();
    }

    public bool PassoConcluido(int indice)
    {
        if (_passosConcluidos == null || indice >= _passosConcluidos.Length)
            return false;
        return _passosConcluidos[indice];
    }

    // ── Lógica Interna ────────────────────────────────────────

    private void AtivarPasso(int indice)
    {
        var passo = ProcedimentoAtual.passos[indice];
        OnPassoAtivado?.Invoke(passo, indice);

        if (!string.IsNullOrEmpty(passo.componenteDestaque3D))
            DestacarComponente3D(passo.componenteDestaque3D);
    }

    private void ConcluirProcedimento()
    {
        EmAndamento = false;
        Debug.Log($"[MaintenanceManager] Procedimento concluído: {ProcedimentoAtual.nomeProcedimento}");
        OnProcedimentoConcluido?.Invoke(ProcedimentoAtual);
        ResetarEstado();
    }

    private void ResetarEstado()
    {
        EmAndamento       = false;
        ProcedimentoAtual = null;
        IndicePassoAtual  = 0;
        _passosConcluidos = null;
    }

    private void DestacarComponente3D(string nomeComponente)
    {
        foreach (var h in FindObjectsOfType<ComponentHighlighter>())
            h.Desativar();

        var go = GameObject.Find(nomeComponente);
        if (go == null) return;

        var highlighter = go.GetComponent<ComponentHighlighter>();
        if (highlighter != null)
            highlighter.Ativar();
    }
}