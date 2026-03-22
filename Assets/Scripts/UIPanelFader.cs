using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class UIPanelFader : MonoBehaviour
{
    [Header("Animação")]
    [SerializeField] private float duracaoFade = 0.25f;
    [SerializeField] private float deslocamentoEntradaY = 20f;

    private CanvasGroup _canvasGroup;
    private RectTransform _rectTransform;
    private Vector2 _anchoredPosInicial;
    private Coroutine _animacaoAtual;

    private void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
        _rectTransform = GetComponent<RectTransform>();

        if (_rectTransform != null)
            _anchoredPosInicial = _rectTransform.anchoredPosition;
    }

    public void MostrarInstantaneo()
    {
        gameObject.SetActive(true);

        _canvasGroup.alpha = 1f;
        _canvasGroup.interactable = true;
        _canvasGroup.blocksRaycasts = true;

        if (_rectTransform != null)
            _rectTransform.anchoredPosition = _anchoredPosInicial;
    }

    public void EsconderInstantaneo()
    {
        _canvasGroup.alpha = 0f;
        _canvasGroup.interactable = false;
        _canvasGroup.blocksRaycasts = false;

        if (_rectTransform != null)
            _rectTransform.anchoredPosition = _anchoredPosInicial;

        gameObject.SetActive(false);
    }

    public void Mostrar()
    {
        if (_animacaoAtual != null)
            StopCoroutine(_animacaoAtual);

        _animacaoAtual = StartCoroutine(AnimarMostrar());
    }

    public void Esconder()
    {
        if (_animacaoAtual != null)
            StopCoroutine(_animacaoAtual);

        _animacaoAtual = StartCoroutine(AnimarEsconder());
    }

    private IEnumerator AnimarMostrar()
    {
        gameObject.SetActive(true);

        _canvasGroup.alpha = 0f;
        _canvasGroup.interactable = false;
        _canvasGroup.blocksRaycasts = false;

        if (_rectTransform != null)
            _rectTransform.anchoredPosition = _anchoredPosInicial + new Vector2(0f, -deslocamentoEntradaY);

        float t = 0f;

        while (t < duracaoFade)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, t / duracaoFade);

            _canvasGroup.alpha = k;

            if (_rectTransform != null)
            {
                _rectTransform.anchoredPosition = Vector2.Lerp(
                    _anchoredPosInicial + new Vector2(0f, -deslocamentoEntradaY),
                    _anchoredPosInicial,
                    k
                );
            }

            yield return null;
        }

        _canvasGroup.alpha = 1f;
        _canvasGroup.interactable = true;
        _canvasGroup.blocksRaycasts = true;

        if (_rectTransform != null)
            _rectTransform.anchoredPosition = _anchoredPosInicial;

        _animacaoAtual = null;
    }

    private IEnumerator AnimarEsconder()
    {
        _canvasGroup.interactable = false;
        _canvasGroup.blocksRaycasts = false;

        Vector2 posInicial = _rectTransform != null ? _rectTransform.anchoredPosition : Vector2.zero;
        Vector2 posFinal = _anchoredPosInicial + new Vector2(0f, deslocamentoEntradaY);
        float alphaInicial = _canvasGroup.alpha;

        float t = 0f;

        while (t < duracaoFade)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, t / duracaoFade);

            _canvasGroup.alpha = Mathf.Lerp(alphaInicial, 0f, k);

            if (_rectTransform != null)
                _rectTransform.anchoredPosition = Vector2.Lerp(posInicial, posFinal, k);

            yield return null;
        }

        _canvasGroup.alpha = 0f;

        if (_rectTransform != null)
            _rectTransform.anchoredPosition = _anchoredPosInicial;

        gameObject.SetActive(false);
        _animacaoAtual = null;
    }
}