import { EEnterprise, EProduct, ERepairResult, ERepairStatus, EServiceOrderStatus } from './enums';
import {
    ENTERPRISE_LABELS,
    PRODUCT_LABELS,
    REPAIR_RESULT_LABELS,
    REPAIR_STATUS_LABELS,
    SERVICE_ORDER_STATUS_LABELS,
} from './enum-labels';

const ENUMS = {
    enterprise: { type: EEnterprise, labels: ENTERPRISE_LABELS },
    product: { type: EProduct, labels: PRODUCT_LABELS },
    repairResult: { type: ERepairResult, labels: REPAIR_RESULT_LABELS },
    repairStatus: { type: ERepairStatus, labels: REPAIR_STATUS_LABELS },
    serviceOrderStatus: { type: EServiceOrderStatus, labels: SERVICE_ORDER_STATUS_LABELS },
} as const;

export type EnumKind = keyof typeof ENUMS;

export type EnumValue = number | string | null | undefined;

export function translateEnum(kind: EnumKind, value: EnumValue): string {
    if (value === null || value === undefined || value === '') {
        return '';
    }

    const { type, labels } = ENUMS[kind];
    const enumType = type as unknown as Record<string, number | string>;
    const labelMap = labels as Record<number, string>;

    const numeric = typeof value === 'number'
        ? value
        : /^\d+$/.test(value) ? Number(value) : enumType[value];

    return (typeof numeric === 'number' ? labelMap[numeric] : undefined) ?? String(value);
}

export function enumOptions(kind: EnumKind): { value: number; label: string }[] {
    const labels = ENUMS[kind].labels as Record<number, string>;
    return Object.entries(labels).map(([value, label]) => ({ value: Number(value), label }));
}
