using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Sterilizador/Maintenance Procedure", fileName = "Proc_")]
public class MaintenanceProcedure : ScriptableObject
{
    [Header("Identificação")]
    public string codigoProcedimento;
    public string nomeProcedimento;
    public TipoManutencao tipo;

    [Header("Descrição")]
    [TextArea(2, 4)]
    public string descricao;

    [Header("Informações Gerais")]
    public string tempoEstimado;
    public string nivelTecnico;

    [Header("Ferramentas Necessárias")]
    public List<string> ferramentasNecessarias = new List<string>();

    [Header("Peças de Substituição")]
    public List<PecaSubstituicao> pecas = new List<PecaSubstituicao>();

    [Header("Passos do Procedimento")]
    public List<PastoManutencao> passos = new List<PastoManutencao>();

    [Header("Ligação ao Modelo 3D")]
    public string nomeComponente3D;
}

public enum TipoManutencao
{
    Preventiva,
    Corretiva,
    InspecaoPre
}

[System.Serializable]
public class PastoManutencao
{
    public int numero;

    [TextArea(1, 3)]
    public string instrucao;

    public bool   requerFoto;
    public bool   requerMedicao;
    public string unidadeMedicao;
    public string componenteDestaque3D;
    public CategoriaPasso categoria;
}

public enum CategoriaPasso
{
    Seguranca,
    Desmontagem,
    Inspecao,
    Substituicao,
    Montagem,
    Verificacao
}

[System.Serializable]
public class PecaSubstituicao
{
    public string referencia;
    public string nome;
    public int    quantidade;
}