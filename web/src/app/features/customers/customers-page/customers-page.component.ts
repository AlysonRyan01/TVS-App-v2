import { Component, inject, viewChild } from "@angular/core";
import { MatDialog } from "@angular/material/dialog";
import { BaseLayoutComponent } from "../../../shared/layout/base-layout/base-layout.component";
import { CustomerSearchComponent } from "../customer-search/customer-search.component";
import { CustomerDialogComponent, CustomerDialogData } from "../customer-dialog/customer-dialog.component";
import { Customer } from "../models/customer.model";

/**
 * Tela "Clientes" (substitui o cartão "Editar Cliente" da antiga tela Geral): busca por nome e
 * edita no mesmo modal de cliente das telas Adicionar OS e Vender. O modal já avisa "Cliente atualizado.".
 */
@Component({
    selector: 'app-customers-page',
    templateUrl: './customers-page.component.html',
    styleUrl: './customers-page.component.scss',
    imports: [BaseLayoutComponent, CustomerSearchComponent],
})
export class CustomersPageComponent {
    private _dialog = inject(MatDialog);
    private _search = viewChild(CustomerSearchComponent);

    protected edit(customer: Customer) {
        const data: CustomerDialogData = { customer, mode: 'edit' };
        this._dialog.open(CustomerDialogComponent, { data, width: '44rem', maxWidth: '95vw', autoFocus: 'first-tabbable' })
            .afterClosed()
            .subscribe((updated?: Customer) => {
                if (updated) {
                    this._search()?.clear();
                }
            });
    }
}
