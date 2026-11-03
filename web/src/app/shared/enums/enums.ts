// Espelho dos enums do backend (TVSApp.Domain/Enums). Mantenha os mesmos nomes e valores.

export enum EEnterprise {
    Particular = 1,
    Cocel = 3,
    Seguradora = 8,
    PhilcoBritania = 16,
    Copel = 20,
}

export enum EProduct {
    Tv = 1,
    TvBox = 2,
    ControleRemoto = 5,
    Som = 6,
    CaixaAcustica = 10,
    Microondas = 18,
    Fonte = 19,
    Bases = 20,
    PlacaFonte = 21,
    PlacaPrincipal = 22,
}

export enum ERepairResult {
    Repair = 1,
    Unrepaired = 2,
    NoDefectFound = 3,
}

export enum ERepairStatus {
    Entered = 1,
    Approved = 2,
    Disapproved = 3,
    Waiting = 4,
}

export enum EServiceOrderStatus {
    Entered = 1,
    Evaluated = 2,
    Repaired = 3,
    Delivered = 4,
    OrderPart = 5,
    ReadyToPickUp = 6,
}
