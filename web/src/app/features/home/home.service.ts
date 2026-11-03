import { inject, Injectable, signal } from "@angular/core";
import { HttpClient } from "@angular/common/http";
import { PendingEstimateServiceOrder } from "./models/pending-estimate-service-ordens.interface";
import { WaitingPartServiceOrder } from "./models/waiting-part-service-orders.interface";
import { ApprovedServiceOrder } from "./models/approved-service-orders.interface";
import { RefusedServiceOrder } from "./models/refused-service-orders.interface";
import { ReadyServiceOrder } from "./models/ready-service-orders.interface";
import { PaginationParams } from "../../shared/models/PaginationParams.interface";
import { Observable } from "rxjs";
import { BaseResponse } from "../../shared/models/BaseResponse.interface";
import { environment } from "../../../environments/environment";
import { PaginatedResult } from "../../shared/models/PaginatedResult.interface";
import { WaitingResponseServiceOrder } from "./models/waiting-response-service-orders.interface";

@Injectable({
    providedIn: 'root'
})
export class HomeService {
    private _http = inject(HttpClient);

    getPendingEstimateServiceOrders(pagination: PaginationParams): Observable<BaseResponse<PaginatedResult<PendingEstimateServiceOrder[]>>> {
        return this._http.get<BaseResponse<PaginatedResult<PendingEstimateServiceOrder[]>>>(`${environment.apiUrl}/get-pending-estimates-service-orders/${pagination.pageNumber}/${pagination.pageSize}`);
    }

    getWaitingResponseServiceOrders(pagination: PaginationParams): Observable<BaseResponse<PaginatedResult<WaitingResponseServiceOrder[]>>> {
        return this._http.get<BaseResponse<PaginatedResult<WaitingResponseServiceOrder[]>>>(`${environment.apiUrl}/get-waiting-response-service-orders/${pagination.pageNumber}/${pagination.pageSize}`);
    }

    getApprovedServiceOrders(pagination: PaginationParams): Observable<BaseResponse<PaginatedResult<ApprovedServiceOrder[]>>> {
        return this._http.get<BaseResponse<PaginatedResult<ApprovedServiceOrder[]>>>(`${environment.apiUrl}/get-pending-purchase-service-orders/${pagination.pageNumber}/${pagination.pageSize}`);
    }

    getRefusedServiceOrders(pagination: PaginationParams): Observable<BaseResponse<PaginatedResult<RefusedServiceOrder[]>>> {
        return this._http.get<BaseResponse<PaginatedResult<RefusedServiceOrder[]>>>(`${environment.apiUrl}/get-rejected-estimate-service-orders/${pagination.pageNumber}/${pagination.pageSize}`);
    }

    getWaitingPartServiceOrders(pagination: PaginationParams): Observable<BaseResponse<PaginatedResult<WaitingPartServiceOrder[]>>> {
        return this._http.get<BaseResponse<PaginatedResult<WaitingPartServiceOrder[]>>>(`${environment.apiUrl}/get-waiting-parts-service-orders/${pagination.pageNumber}/${pagination.pageSize}`);
    }

    getReadyServiceOrders(pagination: PaginationParams): Observable<BaseResponse<PaginatedResult<ReadyServiceOrder[]>>> {
        return this._http.get<BaseResponse<PaginatedResult<ReadyServiceOrder[]>>>(`${environment.apiUrl}/get-waiting-pickup-service-orders/${pagination.pageNumber}/${pagination.pageSize}`);
    }
}