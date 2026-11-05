import { HttpClient } from "@angular/common/http";
import { inject, Injectable } from "@angular/core";
import { Observable } from "rxjs";
import { BaseResponse } from "../../../../shared/models/base-response.interface";
import { ServiceOrder } from "../../../../shared/models/service-order.interface";
import { environment } from "../../../../../environments/environment";

/** Busca de uma OS pelo número e localização do produto, usados por todos os modais da Home. */
@Injectable({ providedIn: 'root' })
export class ServiceOrderLookupService {
    private _http = inject(HttpClient);

    getById(id: number): Observable<BaseResponse<ServiceOrder>> {
        return this._http.get<BaseResponse<ServiceOrder>>(`${environment.apiUrl}/service-orders/${id}`);
    }

    /** Grava onde o aparelho está guardado. A API devolve a localização já normalizada (maiúsculas). */
    addProductLocation(id: number, location: string): Observable<BaseResponse<string>> {
        return this._http.put<BaseResponse<string>>(`${environment.apiUrl}/service-orders/${id}/product/location`, { location });
    }
}
