import { HttpClient, HttpParams } from "@angular/common/http";
import { inject, Injectable } from "@angular/core";
import { Observable } from "rxjs";
import { environment } from "../../../environments/environment";
import { BaseResponse } from "../../shared/models/base-response.interface";
import { ServiceOrder } from "../../shared/models/service-order.interface";
import { DashboardMetrics, MetricsPeriod } from "./models/dashboard-metrics.model";

@Injectable({ providedIn: 'root' })
export class MetricsService {
    private _http = inject(HttpClient);

    get(period: MetricsPeriod): Observable<BaseResponse<DashboardMetrics>> {
        let params = new HttpParams();
        for (const [key, value] of Object.entries(period)) {
            if (value !== null && value !== undefined && value !== '') {
                params = params.set(key, String(value));
            }
        }
        return this._http.get<BaseResponse<DashboardMetrics>>(`${environment.apiUrl}/metrics`, { params });
    }

    updatePartPurchaseCost(id: number, partPurchaseCost: number): Observable<BaseResponse<ServiceOrder>> {
        return this._http.put<BaseResponse<ServiceOrder>>(
            `${environment.apiUrl}/service-orders/${id}/part/purchase-cost`, { partPurchaseCost });
    }
}
