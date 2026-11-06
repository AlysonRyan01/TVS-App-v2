import { HttpClient, HttpResponse } from "@angular/common/http";
import { inject, Injectable } from "@angular/core";
import { Observable } from "rxjs";
import { environment } from "../../../environments/environment";
import { CreateServiceOrder } from "./models/create-service-order.model";

@Injectable({ providedIn: 'root' })
export class AddServiceOrderService {
    private _http = inject(HttpClient);

    /** Sucesso: o termo de entrada em PDF, com o número da OS no cabeçalho X-Service-Order-Id. */
    createServiceOrder(command: CreateServiceOrder): Observable<HttpResponse<Blob>> {
        return this._http.post(`${environment.apiUrl}/service-orders`, command, {
            observe: 'response',
            responseType: 'blob',
        });
    }
}
