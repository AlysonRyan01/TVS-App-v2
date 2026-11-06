import { AfterViewInit, Directive, ElementRef, OnDestroy, inject } from '@angular/core';

/**
 * Prepara as tabelas para o layout de celular: copia o texto de cada cabeçalho para o atributo
 * `data-label` das células da mesma coluna. No celular, o CSS (mixin `stacked-table`) mostra cada
 * linha como um cartão "Rótulo: valor". Acompanha mudanças nos dados (novas linhas, troca de aba).
 */
@Directive({
  selector: 'table[mat-table]',
  host: { class: 'stacked-table' },
})
export class StackedTableDirective implements AfterViewInit, OnDestroy {
  private readonly table = inject<ElementRef<HTMLTableElement>>(ElementRef).nativeElement;
  private observer?: MutationObserver;
  private scheduled = false;

  ngAfterViewInit(): void {
    this.label();
    this.observer = new MutationObserver(() => this.schedule());
    this.observer.observe(this.table, { childList: true, subtree: true, characterData: true });
  }

  ngOnDestroy(): void {
    this.observer?.disconnect();
  }

  private schedule(): void {
    if (this.scheduled) return;
    this.scheduled = true;
    queueMicrotask(() => {
      this.scheduled = false;
      this.label();
    });
  }

  private label(): void {
    const headers = Array.from(this.table.querySelectorAll<HTMLTableCellElement>('thead tr:first-child th'))
      .map(th => th.textContent?.trim() ?? '');

    this.table.querySelectorAll<HTMLTableRowElement>('tbody tr').forEach(row => {
      Array.from(row.cells).forEach((cell, index) => {
        const label = cell.colSpan > 1 ? '' : headers[index] ?? '';
        if (label && cell.dataset['label'] !== label) cell.dataset['label'] = label;
        if (!label && cell.hasAttribute('data-label')) cell.removeAttribute('data-label');
      });
    });
  }
}
