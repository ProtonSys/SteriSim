using UnityEngine;

public class MenuManager : MonoBehaviour
{
    [Header("Painéis")]
    [SerializeField] private GameObject painelPrincipal;
    [SerializeField] private GameObject painelManutencao;
    [SerializeField] private GameObject painelCorretiva;

    [Header("Procedimento")]
    [SerializeField] private GameObject procedimentoUI;
    [SerializeField] private MaintenanceSequence maintenanceSequence;

    private void Awake()
    {
        if (painelPrincipal != null) painelPrincipal.SetActive(true);
        if (painelManutencao != null) painelManutencao.SetActive(false);
        if (painelCorretiva != null) painelCorretiva.SetActive(false);

        if (procedimentoUI != null)
            procedimentoUI.SetActive(false);

        ForcarCanvasGroup(painelPrincipal, true);
        ForcarCanvasGroup(painelManutencao, false);
        ForcarCanvasGroup(painelCorretiva, false);
    }

    public void AbrirManutencao()
    {
        Debug.Log("[MenuManager] AbrirManutencao");
        MostrarPainel(painelManutencao);
    }

    public void AbrirPreventiva()
    {
        Debug.Log("[MenuManager] Abrir manutenção preventiva.");
    }

    public void AbrirCorretiva()
    {
        Debug.Log("[MenuManager] AbrirCorretiva");
        MostrarPainel(painelCorretiva);
    }

    public void Voltar()
    {
        Debug.Log("[MenuManager] Voltar");

        if (painelCorretiva != null && painelCorretiva.activeSelf)
        {
            MostrarPainel(painelManutencao);
            return;
        }

        if (painelManutencao != null && painelManutencao.activeSelf)
        {
            MostrarPainel(painelPrincipal);
            return;
        }

        MostrarPainel(painelPrincipal);
    }

    public void IniciarSubstituicaoResistencia()
    {
        Debug.Log("[MenuManager] IniciarSubstituicaoResistencia");

        gameObject.SetActive(false);

        if (procedimentoUI != null)
            procedimentoUI.SetActive(true);

        if (maintenanceSequence != null)
            maintenanceSequence.HandleButton();
    }

    public void FecharMenu()
    {
        gameObject.SetActive(false);
    }

    public void FecharAplicacao()
    {
        Debug.Log("A sair do Digital Twin...");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void MostrarPainel(GameObject painelAtivo)
    {
        if (painelPrincipal != null)
        {
            bool ativo = painelPrincipal == painelAtivo;
            painelPrincipal.SetActive(ativo);
            ForcarCanvasGroup(painelPrincipal, ativo);
        }

        if (painelManutencao != null)
        {
            bool ativo = painelManutencao == painelAtivo;
            painelManutencao.SetActive(ativo);
            ForcarCanvasGroup(painelManutencao, ativo);
        }

        if (painelCorretiva != null)
        {
            bool ativo = painelCorretiva == painelAtivo;
            painelCorretiva.SetActive(ativo);
            ForcarCanvasGroup(painelCorretiva, ativo);
        }
    }

    private void ForcarCanvasGroup(GameObject painel, bool visivel)
    {
        if (painel == null) return;

        CanvasGroup cg = painel.GetComponent<CanvasGroup>();
        if (cg == null) return;

        cg.alpha = visivel ? 1f : 0f;
        cg.interactable = visivel;
        cg.blocksRaycasts = visivel;
    }
}