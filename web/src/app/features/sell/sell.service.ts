import { HttpClient, HttpResponse } from "@angular/common/http";
import { inject, Injectable } from "@angular/core";
import { Observable } from "rxjs";
import { environment } from "../../../environments/environment";
import { CreateSale } from "./models/create-sale.model";

@Injectable({ providedIn: 'root' })
export class SellService {
    private _http = inject(HttpClient);

    /** Sucesso: o comprovante de venda em PDF, com o número da OS de venda no cabeçalho X-Service-Order-Id. */
    createSale(command: CreateSale): Observable<HttpResponse<Blob>> {
        return this._http.post(`${environment.apiUrl}/service-orders/sales`, command, {
            observe: 'response',
            responseType: 'blob',
        });
    }
}
