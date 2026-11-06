import { HttpClient } from "@angular/common/http";
import { inject, Injectable } from "@angular/core";
import { Observable } from "rxjs";
import { BaseResponse } from "../../../../shared/models/base-response.interface";
import { ServiceOrder } from "../../../../shared/models/service-order.interface";
import { environment } from "../../../../../environments/environment";

@Injectable({ providedIn: 'root' })
export class ServiceOrderLookupService {
    private _http = inject(HttpClient);

    getById(id: number): Observable<BaseResponse<ServiceOrder>> {
        return this._http.get<BaseResponse<ServiceOrder>>(`${environment.apiUrl}/service-orders/${id}`);
    }

    addProductLocation(id: number, location: string): Observable<BaseResponse<string>> {
        return this._http.put<BaseResponse<string>>(`${environment.apiUrl}/service-orders/${id}/product/location`, { location });
    }
}
