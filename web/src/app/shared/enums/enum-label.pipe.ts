import { Pipe, PipeTransform } from '@angular/core';
import { EnumKind, EnumValue, translateEnum } from './enum-translator';

/** Uso: {{ element.situation | enumLabel: 'repairResult' }} */
@Pipe({ name: 'enumLabel' })
export class EnumLabelPipe implements PipeTransform {
    transform(value: EnumValue, kind: EnumKind): string {
        return translateEnum(kind, value);
    }
}
