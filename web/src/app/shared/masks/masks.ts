/**
 * Máscaras visuais. O que vai para a API continua sem pontuação (ver `unmaskPhone` e
 * `normalizeDocument`); a máscara só formata o que a pessoa vê e digita.
 */

/** Só os dígitos do telefone, no máximo 11. */
export function unmaskPhone(value: string | null | undefined): string {
    return (value ?? '').replace(/\D/g, '').slice(0, 11);
}

/** (41) 99999-9999 (11 dígitos) ou (41) 3333-4444 (10), formatando também enquanto digita. */
export function maskPhone(value: string | null | undefined): string {
    const d = unmaskPhone(value);
    if (!d) {
        return '';
    }
    if (d.length <= 2) {
        return `(${d}`;
    }

    const ddd = d.slice(0, 2);
    const rest = d.slice(2);
    // Até 10 dígitos o número é tratado como fixo (4 + 4); com 11, como celular (5 + 4).
    const split = d.length === 11 ? 5 : 4;
    return rest.length <= split
        ? `(${ddd}) ${rest}`
        : `(${ddd}) ${rest.slice(0, split)}-${rest.slice(split)}`;
}

/** Letras e números do documento, em maiúsculas, no máximo 14. */
function documentChars(value: string | null | undefined): string {
    return (value ?? '').toUpperCase().replace(/[^A-Z0-9]/g, '').slice(0, 14);
}

/**
 * CPF (000.000.000-00) enquanto tiver até 11 dígitos; com letra ou mais de 11 caracteres,
 * CNPJ (XX.XXX.XXX/XXXX-00, aceitando o CNPJ alfanumérico).
 */
export function maskDocument(value: string | null | undefined): string {
    const c = documentChars(value);
    const isCnpj = c.length > 11 || /[A-Z]/.test(c);

    const parts = isCnpj
        ? [[0, 2, ''], [2, 5, '.'], [5, 8, '.'], [8, 12, '/'], [12, 14, '-']]
        : [[0, 3, ''], [3, 6, '.'], [6, 9, '.'], [9, 11, '-']];

    let out = '';
    for (const [start, end, separator] of parts as [number, number, string][]) {
        const piece = c.slice(start, end);
        if (!piece) {
            break;
        }
        out += (out ? separator : '') + piece;
    }
    return out;
}
