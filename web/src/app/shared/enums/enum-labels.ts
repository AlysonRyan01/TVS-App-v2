import { EEnterprise, EProduct, ERepairResult, ERepairStatus, EServiceOrderStatus } from './enums';

// Textos iguais aos [Display(Name = ...)] do backend.

export const ENTERPRISE_LABELS: Record<EEnterprise, string> = {
    [EEnterprise.Particular]: 'Particular',
    [EEnterprise.Cocel]: 'Cocel',
    [EEnterprise.Seguradora]: 'Seguradora',
    [EEnterprise.PhilcoBritania]: 'Philco/Britania',
    [EEnterprise.Copel]: 'Copel',
};

export const PRODUCT_LABELS: Record<EProduct, string> = {
    [EProduct.Tv]: 'TV',
    [EProduct.TvBox]: 'TV-BOX',
    [EProduct.ControleRemoto]: 'CONTROLE REMOTO',
    [EProduct.Som]: 'SOM',
    [EProduct.CaixaAcustica]: 'CAIXA ACÚSTICA',
    [EProduct.Microondas]: 'MICROONDAS',
    [EProduct.Fonte]: 'FONTE',
    [EProduct.Bases]: 'BASES',
    [EProduct.PlacaFonte]: 'PLACA FONTE',
    [EProduct.PlacaPrincipal]: 'PLACA PRINCIPAL',
};

export const REPAIR_RESULT_LABELS: Record<ERepairResult, string> = {
    [ERepairResult.Repair]: 'Tem conserto',
    [ERepairResult.Unrepaired]: 'Não tem conserto',
    [ERepairResult.NoDefectFound]: 'Não apresentou defeito',
};

export const REPAIR_STATUS_LABELS: Record<ERepairStatus, string> = {
    [ERepairStatus.Entered]: 'Orçamento pendente',
    [ERepairStatus.Approved]: 'Aprovado',
    [ERepairStatus.Disapproved]: 'Reprovado',
    [ERepairStatus.Waiting]: 'Aguardando resposta',
};

export const SERVICE_ORDER_STATUS_LABELS: Record<EServiceOrderStatus, string> = {
    [EServiceOrderStatus.Entered]: 'Entrada',
    [EServiceOrderStatus.Evaluated]: 'Avaliado',
    [EServiceOrderStatus.Repaired]: 'Consertado',
    [EServiceOrderStatus.Delivered]: 'Entregue',
    [EServiceOrderStatus.OrderPart]: 'Aguardando peça',
    [EServiceOrderStatus.ReadyToPickUp]: 'Pronto para entrega',
};
