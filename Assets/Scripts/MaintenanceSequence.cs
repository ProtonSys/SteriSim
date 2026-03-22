using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class MaintenanceSequence : MonoBehaviour
{
    [Header("Animators")]
    public Animator screwsAnimator;
    public Animator flangeAnimator;
    public Animator oringAnimator;
    public Animator heaterAnimator;

    [Header("Animation State Names")]
    public string screwsStateName = "HeaterFlangeScrewRemoval";
    public string flangeStateName = "HeaterFlangeRemoval";
    public string oringStateName = "HeaterOringRemoval";
    public string heaterStateName = "HeaterRemoval";

    [Header("UI")]
    public TextMeshProUGUI stepText;
    public TextMeshProUGUI progressText;

    [Header("UI do Procedimento")]
    [Tooltip("Raiz da UI do procedimento (ex: ProcedimentoUI). Pode ser ativada/desativada por código.")]
    public GameObject procedimentoUIRoot;
    [Tooltip("Se ativo, a UI do procedimento começa escondida e só aparece quando o procedimento é aberto.")]
    public bool esconderUIAoIniciar = true;

    [Header("Botão")]
    public Button botaoAcao;
    public TMP_Text textoBotao;

    [Header("Checklist")]
    public Transform checklistContainer;
    public GameObject checklistItemPrefab;

    [Header("Highlight dos Componentes")]
    [Tooltip("Ordem: [0] Parafusos, [1] Flange, [2] Resistência, [3] O-ring")]
    public ComponentHighlighter[] componentesDestaque;

    [Header("Foco da Câmara por Zona")]
    public ProcedureCameraFocus cameraFocus;
    public ProcedureCameraFocus.CameraZoneFocus[] zonasFoco;

    [Header("Parafusos da Flange")]
    public ScrewRotator[] flangeScrews;

    [Header("Procedimento (opcional)")]
    public MaintenanceProcedure procedimento;

    [Header("Definições")]
    public bool allowKeyboardTest = true;

    public int CurrentStep { get; private set; } = 0;
    public bool SequenceStarted { get; private set; } = false;

    [Header("Temporização")]
    [Tooltip("Tempo para a câmara assentar na zona antes da animação começar")]
    public float atrasoAntesAnimacao = 0.75f;

    private Coroutine _passoCoroutine;
    private bool _aExecutarPasso = false;

    private readonly List<ChecklistItemUI> _itens = new List<ChecklistItemUI>();
    private readonly List<string> _passos = new List<string>
    {
        "Remover os parafusos da flange",
        "Remover a flange",
        "Remover a resistência",
        "Remover o O-ring",
    };

    private int Total => _passos.Count;

    private void Start()
    {
        SetAnimatorsSpeed(0f);

        if (procedimento != null && procedimento.passos.Count > 0)
        {
            _passos.Clear();
            foreach (var p in procedimento.passos)
                _passos.Add(p.instrucao);
        }

        if (MaintenanceManager.Instance != null)
        {
            MaintenanceManager.Instance.OnProcedimentoIniciado += OnIniciado;
            MaintenanceManager.Instance.OnPassoAtivado += OnPassoAtivado;
            MaintenanceManager.Instance.OnProcedimentoConcluido += OnConcluido;
        }

        if (procedimentoUIRoot != null && esconderUIAoIniciar)
            procedimentoUIRoot.SetActive(false);

        if (textoBotao == null && botaoAcao != null)
            textoBotao = botaoAcao.GetComponentInChildren<TMPro.TMP_Text>(true);

        ConstruirChecklist();
        ResetSequence();
    }

    private void OnDestroy()
    {
        if (MaintenanceManager.Instance == null) return;

        MaintenanceManager.Instance.OnProcedimentoIniciado -= OnIniciado;
        MaintenanceManager.Instance.OnPassoAtivado -= OnPassoAtivado;
        MaintenanceManager.Instance.OnProcedimentoConcluido -= OnConcluido;
    }

    private void Update()
    {
        if (!allowKeyboardTest || Keyboard.current == null)
            return;

        if (Keyboard.current.spaceKey.wasPressedThisFrame)
            HandleButton();

        if (Keyboard.current.rKey.wasPressedThisFrame)
            ResetSequence();
    }

    public void MostrarProcedimentoUI()
    {
        if (procedimentoUIRoot != null)
            procedimentoUIRoot.SetActive(true);

        if (_itens.Count == 0)
            ConstruirChecklist();

        AtualizarChecklist();
        AtualizarTextos();
    }

    public void EsconderProcedimentoUI()
    {
        if (procedimentoUIRoot != null)
            procedimentoUIRoot.SetActive(false);
    }

    public void MostrarUIEIniciarProcedimento()
    {
        MostrarProcedimentoUI();

        SequenceStarted = false;
        CurrentStep = 0;

        AtualizarChecklist();
        AtualizarTextos();
    }

    private void ConstruirChecklist()
    {
        if (checklistContainer == null || checklistItemPrefab == null)
            return;

        foreach (Transform f in checklistContainer)
            Destroy(f.gameObject);

        _itens.Clear();

        for (int i = 0; i < _passos.Count; i++)
        {
            var go = Instantiate(checklistItemPrefab, checklistContainer);
            var ui = go.GetComponent<ChecklistItemUI>();
            if (ui == null) continue;

            ui.Inicializar(i + 1, _passos[i]);
            _itens.Add(ui);
        }
    }

    private void AtualizarChecklist()
    {
        if (_itens.Count == 0 && checklistContainer != null && checklistItemPrefab != null)
            ConstruirChecklist();
        for (int i = 0; i < _itens.Count; i++)
        {
            _itens[i].SetConcluido(i < CurrentStep);
            _itens[i].SetAtivo(SequenceStarted && i == CurrentStep && CurrentStep < Total);
        }
    }

    private void AtualizarTextos()
    {
        if (stepText != null)
        {
            if (!SequenceStarted)
                stepText.text = "Clique em \"Começar\" para iniciar";
            else if (CurrentStep >= Total)
                stepText.text = "✔ Procedimento concluído";
            else
                stepText.text = $"Passo {CurrentStep + 1}: {_passos[CurrentStep]}";
        }

        if (progressText != null)
        {
            if (!SequenceStarted)
                progressText.text = $"0 / {Total}";
            else if (CurrentStep >= Total)
                progressText.text = $"{Total} / {Total}";
            else
                progressText.text = $"{CurrentStep + 1} / {Total}";
        }

        AtualizarTextoBotao();
        AtualizarHighlight();
        AtualizarFocoCamera();
    }

    private void AtualizarTextoBotao()
    {
        if (textoBotao == null && botaoAcao != null)
            textoBotao = botaoAcao.GetComponentInChildren<TMPro.TMP_Text>(true);

        if (textoBotao == null) return;

        if (!SequenceStarted)
            textoBotao.text = "Começar";
        else if (CurrentStep >= Total)
            textoBotao.text = "Concluído";
        else
            textoBotao.text = "Continuar";
    }

    private void AtualizarHighlight()
    {
        if (componentesDestaque == null || componentesDestaque.Length == 0)
            return;

        for (int i = 0; i < componentesDestaque.Length; i++)
        {
            var comp = componentesDestaque[i];
            if (comp == null) continue;

            if (!SequenceStarted)
            {
                comp.Desativar();
                continue;
            }

            if (CurrentStep >= Total)
            {
                comp.Desativar();
                continue;
            }

            if (i == CurrentStep)
                comp.Ativar();
            else
                comp.Desativar();
        }
    }

    private void AtualizarFocoCamera()
    {
        if (cameraFocus == null)
            return;

        if (!SequenceStarted || CurrentStep >= Total)
        {
            cameraFocus.VoltarAoInicio();
            return;
        }

        if (zonasFoco != null &&
            CurrentStep < zonasFoco.Length &&
            zonasFoco[CurrentStep] != null)
        {
            cameraFocus.Focar(zonasFoco[CurrentStep]);
        }
    }

    private void OnIniciado(MaintenanceProcedure proc)
    {
        if (procedimento == null) return;
        if (proc.codigoProcedimento != procedimento.codigoProcedimento) return;

        ResetSequence();
        MostrarProcedimentoUI();

        SequenceStarted = false;
        CurrentStep = 0;

        AtualizarChecklist();
        AtualizarTextos();
    }

    private void OnPassoAtivado(PastoManutencao passo, int indice)
    {
        if (!SequenceStarted || procedimento == null) return;

        var ativo = MaintenanceManager.Instance?.ProcedimentoAtual;
        if (ativo?.codigoProcedimento != procedimento.codigoProcedimento) return;

        IniciarExecucaoPasso(indice, false);
    }

    private void OnConcluido(MaintenanceProcedure proc)
    {
        if (procedimento == null) return;
        if (proc.codigoProcedimento != procedimento.codigoProcedimento) return;

        if (_passoCoroutine != null)
        {
            StopCoroutine(_passoCoroutine);
            _passoCoroutine = null;
        }

        _aExecutarPasso = false;
        CurrentStep = Total;
        PararRotacaoParafusos();
        AtualizarChecklist();
        AtualizarTextos();
    }

    public void HandleButton()
    {
        Debug.Log($"[HandleButton] Step={CurrentStep} Started={SequenceStarted}");

        if (procedimentoUIRoot != null && !procedimentoUIRoot.activeSelf)
        {
            procedimentoUIRoot.SetActive(true);
            Debug.Log("[MaintenanceSequence] ProcedimentoUI ativada no HandleButton.");
        }

        if (_aExecutarPasso)
            return;

        if (SequenceStarted && CurrentStep >= Total)
            return;

        if (MaintenanceManager.Instance != null && procedimento != null)
        {
            if (!MaintenanceManager.Instance.EmAndamento)
            {
                MaintenanceManager.Instance.IniciarProcedimento(procedimento);
                return;
            }

            MaintenanceManager.Instance.ConcluirPassoAtual();
            return;
        }

        if (!SequenceStarted)
        {
            SequenceStarted = true;
            CurrentStep = 0;
            AtualizarChecklist();
            AtualizarTextos();
            return;
        }

        NextStep();
    }

    public void NextStep()
    {
        if (_aExecutarPasso)
            return;

        if (!SequenceStarted || CurrentStep >= Total)
            return;

        if (MaintenanceManager.Instance != null && MaintenanceManager.Instance.EmAndamento)
        {
            MaintenanceManager.Instance.ConcluirPassoAtual();
            return;
        }

        IniciarExecucaoPasso(CurrentStep, true);
    }

    public void ResetSequence()
    {
        StopAllCoroutines();
        _passoCoroutine = null;
        _aExecutarPasso = false;

        CurrentStep = 0;
        SequenceStarted = false;

        RebindAnimator(screwsAnimator);
        RebindAnimator(flangeAnimator);
        RebindAnimator(oringAnimator);
        RebindAnimator(heaterAnimator);

        SetAnimatorsSpeed(0f);

        PararRotacaoParafusos();
        ResetarParafusos();

        if (componentesDestaque != null)
        {
            foreach (var h in componentesDestaque)
            {
                if (h != null)
                    h.Desativar();
            }
        }

        if (cameraFocus != null)
            cameraFocus.VoltarAoInicio();

        if (procedimentoUIRoot != null && !procedimentoUIRoot.activeSelf && !esconderUIAoIniciar)
            procedimentoUIRoot.SetActive(true);

        AtualizarChecklist();
        AtualizarTextos();
    }

    private void IniciarExecucaoPasso(int passo, bool avancarNoFim)
    {
        if (passo < 0 || passo >= Total)
            return;

        if (_passoCoroutine != null)
            StopCoroutine(_passoCoroutine);

        _passoCoroutine = StartCoroutine(ExecutarPasso(passo, avancarNoFim));
    }

    private IEnumerator ExecutarPasso(int passo, bool avancarNoFim)
    {
        _aExecutarPasso = true;
        CurrentStep = passo;

        AtualizarChecklist();
        AtualizarTextos();

        if (atrasoAntesAnimacao > 0f)
            yield return new WaitForSeconds(atrasoAntesAnimacao);

        if (!TryGetAnimacaoDoPasso(passo, out Animator anim, out string state))
        {
            Debug.LogWarning($"[MaintenanceSequence] Animator ou state inválido no passo {passo}.");
            _aExecutarPasso = false;
            yield break;
        }

        anim.enabled = true;
        anim.speed = 1f;
        anim.Play(state, 0, 0f);

        yield return StartCoroutine(AguardarFimAnimacao(anim, state, passo));

        if (avancarNoFim)
        {
            CurrentStep++;
            AtualizarChecklist();
            AtualizarTextos();
        }

        _aExecutarPasso = false;
        _passoCoroutine = null;
    }

    private bool TryGetAnimacaoDoPasso(int passo, out Animator anim, out string state)
    {
        anim = null;
        state = string.Empty;

        switch (passo)
        {
            case 0:
                anim = screwsAnimator;
                state = screwsStateName;
                IniciarRotacaoParafusos();
                break;

            case 1:
                PararRotacaoParafusos();
                anim = flangeAnimator;
                state = flangeStateName;
                break;

            case 2:
                anim = heaterAnimator;
                state = heaterStateName;
                break;

            case 3:
                anim = oringAnimator;
                state = oringStateName;
                break;
        }

        return anim != null && !string.IsNullOrWhiteSpace(state);
    }

    private IEnumerator AguardarFimAnimacao(Animator anim, string state, int passo)
    {
        if (anim == null) yield break;

        yield return null;

        float timeout = 2f;
        float elapsed = 0f;

        while (elapsed < timeout)
        {
            if (anim.GetCurrentAnimatorStateInfo(0).IsName(state))
                break;

            elapsed += Time.deltaTime;
            yield return null;
        }

        while (true)
        {
            var info = anim.GetCurrentAnimatorStateInfo(0);

            if (info.normalizedTime >= 0.99f && !anim.IsInTransition(0))
                break;

            yield return null;
        }

        anim.speed = 0f;

        if (passo == 0)
            PararRotacaoParafusos();

        Debug.Log($"[MaintenanceSequence] '{state}' pausado na pose final.");
    }

    private void IniciarRotacaoParafusos()
    {
        if (flangeScrews == null) return;

        foreach (var screw in flangeScrews)
        {
            if (screw != null)
                screw.Iniciar();
        }
    }

    private void PararRotacaoParafusos()
    {
        if (flangeScrews == null) return;

        foreach (var screw in flangeScrews)
        {
            if (screw != null)
                screw.Parar();
        }
    }

    private void ResetarParafusos()
    {
        if (flangeScrews == null) return;

        foreach (var screw in flangeScrews)
        {
            if (screw != null)
                screw.Resetar();
        }
    }

    private void RebindAnimator(Animator anim)
    {
        if (anim == null) return;

        anim.enabled = true;
        anim.speed = 1f;
        anim.Rebind();
        anim.Update(0f);
    }

    private void SetAnimatorsSpeed(float speed)
    {
        if (screwsAnimator != null) screwsAnimator.speed = speed;
        if (flangeAnimator != null) flangeAnimator.speed = speed;
        if (oringAnimator != null) oringAnimator.speed = speed;
        if (heaterAnimator != null) heaterAnimator.speed = speed;
    }
}