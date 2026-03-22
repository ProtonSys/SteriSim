using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ChecklistItemUI : MonoBehaviour
{
    [Header("Referências UI")]
    [SerializeField] private Image imagemFundo;
    [SerializeField] private Image barraLateral;
    [SerializeField] private TMP_Text textNumero;
    [SerializeField] private TMP_Text textInstrucao;
    [SerializeField] private TMP_Text textTick;

    private static readonly Color FundoPendente  = new(0.12f, 0.12f, 0.16f, 0.85f);
    private static readonly Color FundoAtivo     = new(0.10f, 0.35f, 0.80f, 0.95f);
    private static readonly Color FundoConcluido = new(0.10f, 0.45f, 0.20f, 0.80f);

    private static readonly Color BarraPendente  = new(0.30f, 0.30f, 0.35f, 1f);
    private static readonly Color BarraAtivo     = new(0.40f, 0.70f, 1.00f, 1f);
    private static readonly Color BarraConcluido = new(0.20f, 0.90f, 0.40f, 1f);

    private static readonly Color TextoPendente  = new(0.75f, 0.75f, 0.75f, 1f);
    private static readonly Color TextoAtivo     = Color.white;
    private static readonly Color TextoConcluido = Color.white;

    private bool _concluido;
    private bool _ativo;

    public void Inicializar(int numero, string instrucao)
    {
        if (textNumero != null)
            textNumero.text = $"{numero:D2}.";

        if (textInstrucao != null)
            textInstrucao.text = instrucao;

        if (textTick != null)
            textTick.gameObject.SetActive(false);

        _concluido = false;
        _ativo = false;

        AplicarEstado();
    }

    public void SetAtivo(bool ativo)
    {
        if (_concluido) return;

        _ativo = ativo;
        AplicarEstado();
    }

    public void SetConcluido(bool concluido)
    {
        _concluido = concluido;

        if (concluido)
            _ativo = false;

        if (textTick != null)
            textTick.gameObject.SetActive(concluido);

        AplicarEstado();
    }

    private void AplicarEstado()
    {
        Color corFundo;
        Color corBarra;
        Color corTexto;
        FontStyles estilo;

        if (_concluido)
        {
            corFundo = FundoConcluido;
            corBarra = BarraConcluido;
            corTexto = TextoConcluido;
            estilo = FontStyles.Normal;
        }
        else if (_ativo)
        {
            corFundo = FundoAtivo;
            corBarra = BarraAtivo;
            corTexto = TextoAtivo;
            estilo = FontStyles.Bold;
        }
        else
        {
            corFundo = FundoPendente;
            corBarra = BarraPendente;
            corTexto = TextoPendente;
            estilo = FontStyles.Normal;
        }

        if (imagemFundo != null)
            imagemFundo.color = corFundo;

        if (barraLateral != null)
            barraLateral.color = corBarra;

        if (textNumero != null)
        {
            textNumero.color = corTexto;
            textNumero.fontStyle = FontStyles.Normal;
        }

        if (textInstrucao != null)
        {
            textInstrucao.color = corTexto;
            textInstrucao.fontStyle = estilo;
        }

        if (textTick != null)
            textTick.color = Color.white;
    }
}