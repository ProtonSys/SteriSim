using UnityEngine;

/// <summary>
/// Faz o painel de UI seguir e encarar a câmara em VR/3D.
/// Coloca este script no GameObject raiz do Canvas (World Space).
/// </summary>
public class UIFollowCamera : MonoBehaviour
{
    [Header("Câmara")]
    [SerializeField] private Transform cameraTransform;

    [Header("Posicionamento")]
    [SerializeField] private float distanceFromCamera = 0.5f;
    [SerializeField] private float heightOffset       = -0.1f;

    [Header("Suavização")]
    [SerializeField] private bool  smoothFollow = true;
    [SerializeField] private float followSpeed  = 5f;

    [Header("Ângulo máximo antes de reposicionar")]
    [Tooltip("Se o painel sair mais do que X graus do centro da visão, reposiciona.")]
    [SerializeField] private float anguloMaximoVisao = 60f;

    // ── Ciclo de vida ─────────────────────────────────────────

    private void Start()
    {
        // Se não foi atribuída câmara no Inspector, procura automaticamente.
        // Em VR usa a câmara XR; em desktop usa a câmara principal.
        if (cameraTransform == null)
        {
            if (Camera.main != null)
                cameraTransform = Camera.main.transform;
            else
                Debug.LogWarning("[UIFollowCamera] Nenhuma câmara encontrada. " +
                                 "Atribui manualmente no Inspector.");
        }
    }

    private void LateUpdate()
    {
        // LateUpdate garante que a câmara já terminou o seu movimento neste frame
        if (cameraTransform == null) return;

        AtualizarPosicao();
        AtualizarRotacao();
    }

    // ── Lógica ────────────────────────────────────────────────

    private void AtualizarPosicao()
    {
        // Posição alvo: à frente da câmara + offset vertical
        Vector3 posAlvo = cameraTransform.position
                        + cameraTransform.forward * distanceFromCamera
                        + Vector3.up * heightOffset;

        if (smoothFollow)
        {
            // Lerp suaviza o movimento — independente do framerate com deltaTime
            transform.position = Vector3.Lerp(
                transform.position,
                posAlvo,
                Time.deltaTime * followSpeed
            );
        }
        else
        {
            transform.position = posAlvo;
        }
    }

    private void AtualizarRotacao()
    {
        // CORREÇÃO: o vetor de direção vai DO painel PARA a câmara,
        // depois negamos para o painel encarar a câmara (e não ficar invertido).
        //
        // Antes (errado):  transform.position - cameraTransform.position
        //   → painel ficava virado para trás
        //
        // Agora (correto): cameraTransform.position - transform.position
        //   → painel enfrenta sempre a câmara
        Vector3 direcaoParaCamara = cameraTransform.position - transform.position;

        // Evitar erro se a distância for zero
        if (direcaoParaCamara == Vector3.zero) return;

        Quaternion rotAlvo = Quaternion.LookRotation(-direcaoParaCamara);

        if (smoothFollow)
        {
            // Slerp é a interpolação esférica — mais suave para rotações
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                rotAlvo,
                Time.deltaTime * followSpeed
            );
        }
        else
        {
            transform.rotation = rotAlvo;
        }
    }

    /// <summary>
    /// Força o painel a reposicionar-se imediatamente à frente da câmara.
    /// Útil para chamar quando o painel é ativado (OnEnable).
    /// </summary>
    public void ReposicionarImediatamente()
    {
        if (cameraTransform == null) return;

        transform.position = cameraTransform.position
                           + cameraTransform.forward * distanceFromCamera
                           + Vector3.up * heightOffset;

        Vector3 dir = cameraTransform.position - transform.position;
        if (dir != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(-dir);
    }

    // Reposicionar sempre que o painel é ativado
    private void OnEnable() => ReposicionarImediatamente();
}
