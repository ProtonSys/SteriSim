using UnityEngine;

public class ComponentHighlighter : MonoBehaviour
{
    [Header("Material de Highlight")]
    [SerializeField] private Material materialHighlight;

    private Renderer[] _renderers;
    private Material[][] _materiaisOriginais;
    private Material[][] _materiaisHighlight;

    private void Awake()
    {
        _renderers = GetComponentsInChildren<Renderer>(true);

        _materiaisOriginais = new Material[_renderers.Length][];
        _materiaisHighlight = new Material[_renderers.Length][];

        for (int i = 0; i < _renderers.Length; i++)
        {
            _materiaisOriginais[i] = _renderers[i].materials;
            _materiaisHighlight[i] = new Material[_materiaisOriginais[i].Length];

            for (int j = 0; j < _materiaisOriginais[i].Length; j++)
            {
                _materiaisHighlight[i][j] = new Material(materialHighlight);
            }
        }

        Debug.Log($"[ComponentHighlighter] '{name}' encontrou {_renderers.Length} renderer(s).");
    }

    public void Ativar()
    {
        AplicarHighlight();
    }

    public void AtivarInspecao()
    {
        AplicarHighlight();
    }

    public void AtivarConcluido()
    {
        AplicarHighlight();
    }

    public void Desativar()
    {
        for (int i = 0; i < _renderers.Length; i++)
        {
            if (_renderers[i] != null)
                _renderers[i].materials = _materiaisOriginais[i];
        }

        Debug.Log($"[ComponentHighlighter] Highlight removido em '{name}'.");
    }

    private void AplicarHighlight()
    {
        if (materialHighlight == null)
        {
            Debug.LogWarning($"[ComponentHighlighter] materialHighlight não está atribuído em '{name}'.");
            return;
        }

        for (int i = 0; i < _renderers.Length; i++)
        {
            if (_renderers[i] != null)
                _renderers[i].materials = _materiaisHighlight[i];
        }

        Debug.Log($"[ComponentHighlighter] Highlight aplicado em '{name}'.");
    }

    private void OnDestroy()
    {
        if (_materiaisHighlight == null) return;

        for (int i = 0; i < _materiaisHighlight.Length; i++)
        {
            if (_materiaisHighlight[i] == null) continue;

            for (int j = 0; j < _materiaisHighlight[i].Length; j++)
            {
                if (_materiaisHighlight[i][j] != null)
                    Destroy(_materiaisHighlight[i][j]);
            }
        }
    }
}