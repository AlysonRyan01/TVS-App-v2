import { HttpClient, HttpParams, HttpResponse } from "@angular/common/http";
import { inject, Injectable } from "@angular/core";
import { Observable } from "rxjs";
import { environment } from "../../../environments/environment";
import { BaseResponse } from "../../shared/models/base-response.interface";
import { PaginatedResult } from "../../shared/models/paginated-result.interface";
import { ServiceOrder } from "../../shared/models/service-order.interface";
import { EditServiceOrder, ServiceOrderSearchItem, ServiceOrderSearchParams } from "./models/service-order-search.model";

@Injectable({ providedIn: 'root' })
export class SearchServiceOrdersService {
    private _http = inject(HttpClient);

    search(params: ServiceOrderSearchParams): Observable<BaseResponse<PaginatedResult<ServiceOrderSearchItem[]>>> {
        let query = new HttpParams();
        for (const [key, value] of Object.entries(params)) {
            if (value !== null && value !== undefined && value !== '') {
                query = query.set(key, String(value));
            }
        }
        return this._http.get<BaseResponse<PaginatedResult<ServiceOrderSearchItem[]>>>(
            `${environment.apiUrl}/service-orders/search`, { params: query });
    }

    getById(id: number): Observable<BaseResponse<ServiceOrder>> {
        return this._http.get<BaseResponse<ServiceOrder>>(`${environment.apiUrl}/service-orders/${id}`);
    }

    update(id: number, body: EditServiceOrder): Observable<BaseResponse<ServiceOrder>> {
        return this._http.put<BaseResponse<ServiceOrder>>(`${environment.apiUrl}/service-orders/${id}`, body);
    }

    regeneratePdf(id: number): Observable<HttpResponse<Blob>> {
        return this._http.get(`${environment.apiUrl}/service-orders/${id}/pdf`, {
            observe: 'response',
            responseType: 'blob',
        });
    }

    updateLocation(id: number, location: string): Observable<BaseResponse<string>> {
        return this._http.put<BaseResponse<string>>(`${environment.apiUrl}/service-orders/${id}/product/location`, { location });
    }
}
