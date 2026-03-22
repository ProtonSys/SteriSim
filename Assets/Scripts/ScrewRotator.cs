using UnityEngine;

public class ScrewRotator : MonoBehaviour
{
    [Header("Referências")]
    [SerializeField] private Transform eixoReferencia;
    [SerializeField] private Transform rootMovimento;

    [Header("Rotação")]
    [SerializeField] private float velocidadeRotacao = 720f;
    [SerializeField] private bool inverterRotacao = false;

    [Header("Saída")]
    [SerializeField] private float velocidadeSaida = 0.002f;
    [SerializeField] private bool inverterSaida = false;

    private bool _ativo = false;
    private Quaternion _rotacaoInicialMesh;
    private Vector3 _posicaoInicialRoot;

    private void Awake()
    {
        if (eixoReferencia == null)
            eixoReferencia = transform.parent;

        if (rootMovimento == null && transform.parent != null)
            rootMovimento = transform.parent.parent;

        _rotacaoInicialMesh = transform.localRotation;

        if (rootMovimento != null)
            _posicaoInicialRoot = rootMovimento.localPosition;
    }

    private void Update()
    {
        if (!_ativo || eixoReferencia == null)
            return;

        Vector3 eixo = eixoReferencia.forward;
        if (inverterRotacao)
            eixo = -eixo;

        // roda apenas o mesh do parafuso
        transform.Rotate(eixo, velocidadeRotacao * Time.deltaTime, Space.World);

        if (rootMovimento != null)
        {
            Vector3 direcaoSaida = eixoReferencia.forward;
            if (inverterSaida)
                direcaoSaida = -direcaoSaida;

            // move o root inteiro para fora
            rootMovimento.position += direcaoSaida * velocidadeSaida * Time.deltaTime;
        }
    }

    public void Iniciar()
    {
        _ativo = true;
    }

    public void Parar()
    {
        _ativo = false;
    }

    public void Resetar()
    {
        _ativo = false;
        transform.localRotation = _rotacaoInicialMesh;

        if (rootMovimento != null)
            rootMovimento.localPosition = _posicaoInicialRoot;
    }
}