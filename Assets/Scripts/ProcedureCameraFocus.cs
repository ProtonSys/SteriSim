using System.Collections;
using UnityEngine;

public class ProcedureCameraFocus : MonoBehaviour
{
    [System.Serializable]
    public class CameraZoneFocus
    {
        public Transform cameraPoint;
        public Transform lookAtTarget;
    }

    [Header("Referências")]
    [SerializeField] private Transform cameraRig;

    [Header("Movimento")]
    [SerializeField] private float duracaoMovimento = 1.2f;

    private Vector3 _posInicial;
    private Quaternion _rotInicial;
    private Coroutine _movimentoAtual;

    private void Awake()
    {
        if (cameraRig == null)
            cameraRig = transform;

        _posInicial = cameraRig.position;
        _rotInicial = cameraRig.rotation;
    }

    public void Focar(CameraZoneFocus zona)
    {
        if (cameraRig == null || zona == null || zona.cameraPoint == null || zona.lookAtTarget == null)
            return;

        Vector3 posDestino = zona.cameraPoint.position;
        Quaternion rotDestino = Quaternion.LookRotation(
            (zona.lookAtTarget.position - posDestino).normalized,
            Vector3.up
        );

        if (_movimentoAtual != null)
            StopCoroutine(_movimentoAtual);

        _movimentoAtual = StartCoroutine(MoverSuavemente(
            cameraRig.position,
            cameraRig.rotation,
            posDestino,
            rotDestino
        ));
    }

    public void VoltarAoInicio()
    {
        if (cameraRig == null)
            return;

        if (_movimentoAtual != null)
            StopCoroutine(_movimentoAtual);

        _movimentoAtual = StartCoroutine(MoverSuavemente(
            cameraRig.position,
            cameraRig.rotation,
            _posInicial,
            _rotInicial
        ));
    }

    private IEnumerator MoverSuavemente(Vector3 posInicial, Quaternion rotInicial, Vector3 posFinal, Quaternion rotFinal)
    {
        float t = 0f;

        while (t < duracaoMovimento)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, t / duracaoMovimento);

            cameraRig.position = Vector3.Lerp(posInicial, posFinal, k);
            cameraRig.rotation = Quaternion.Slerp(rotInicial, rotFinal, k);

            yield return null;
        }

        cameraRig.position = posFinal;
        cameraRig.rotation = rotFinal;
        _movimentoAtual = null;
    }
}