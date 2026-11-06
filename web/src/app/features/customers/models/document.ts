
const CPF = /^\d{11}$/;
const CNPJ = /^[A-Z0-9]{12}\d{2}$/;

export const INVALID_DOCUMENT_MESSAGE = 'Informe um CPF (11 dígitos) ou um CNPJ (14 caracteres).';

export function normalizeDocument(value: string | null | undefined): string {
    return (value ?? '').replace(/[.\-\/\s]/g, '').toUpperCase();
}

export function isValidDocument(value: string | null | undefined): boolean {
    const normalized = normalizeDocument(value);
    return normalized === '' || CPF.test(normalized) || CNPJ.test(normalized);
}

export function formatDocument(value: string | null | undefined): string {
    const d = normalizeDocument(value);
    if (CPF.test(d)) {
        return `${d.slice(0, 3)}.${d.slice(3, 6)}.${d.slice(6, 9)}-${d.slice(9)}`;
    }
    if (CNPJ.test(d)) {
        return `${d.slice(0, 2)}.${d.slice(2, 5)}.${d.slice(5, 8)}/${d.slice(8, 12)}-${d.slice(12)}`;
    }
    return value ?? '';
}
