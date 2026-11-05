import { inject, Injectable } from "@angular/core";
import { HttpClient } from "@angular/common/http";
import { PendingEstimateServiceOrder } from "./models/pending-estimate-service-order.interface";
import { AwaitingPartServiceOrder } from "./models/awaiting-part-service-order.interface";
import { ApprovedServiceOrder } from "./models/approved-service-order.interface";
import { RejectedServiceOrder } from "./models/rejected-service-order.interface";
import { ReadyForPickupServiceOrder } from "./models/ready-for-pickup-service-order.interface";
import { PaginationParams } from "../../shared/models/pagination-params.interface";
import { Observable } from "rxjs";
import { BaseResponse } from "../../shared/models/base-response.interface";
import { environment } from "../../../environments/environment";
import { PaginatedResult } from "../../shared/models/paginated-result.interface";
import { AwaitingResponseServiceOrder } from "./models/awaiting-response-service-order.interface";

@Injectable({
    providedIn: 'root'
})
export class HomeService {
    private _http = inject(HttpClient);

    getPendingEstimateServiceOrders(pagination: PaginationParams): Observable<BaseResponse<PaginatedResult<PendingEstimateServiceOrder[]>>> {
        return this._http.get<BaseResponse<PaginatedResult<PendingEstimateServiceOrder[]>>>(`${environment.apiUrl}/get-pending-estimate-service-orders/${pagination.pageNumber}/${pagination.pageSize}`);
    }

    getAwaitingResponseServiceOrders(pagination: PaginationParams): Observable<BaseResponse<PaginatedResult<AwaitingResponseServiceOrder[]>>> {
        return this._http.get<BaseResponse<PaginatedResult<AwaitingResponseServiceOrder[]>>>(`${environment.apiUrl}/get-awaiting-response-service-orders/${pagination.pageNumber}/${pagination.pageSize}`);
    }

    getApprovedServiceOrders(pagination: PaginationParams): Observable<BaseResponse<PaginatedResult<ApprovedServiceOrder[]>>> {
        return this._http.get<BaseResponse<PaginatedResult<ApprovedServiceOrder[]>>>(`${environment.apiUrl}/get-approved-service-orders/${pagination.pageNumber}/${pagination.pageSize}`);
    }

    getRejectedServiceOrders(pagination: PaginationParams): Observable<BaseResponse<PaginatedResult<RejectedServiceOrder[]>>> {
        return this._http.get<BaseResponse<PaginatedResult<RejectedServiceOrder[]>>>(`${environment.apiUrl}/get-rejected-service-orders/${pagination.pageNumber}/${pagination.pageSize}`);
    }

    getAwaitingPartServiceOrders(pagination: PaginationParams): Observable<BaseResponse<PaginatedResult<AwaitingPartServiceOrder[]>>> {
        return this._http.get<BaseResponse<PaginatedResult<AwaitingPartServiceOrder[]>>>(`${environment.apiUrl}/get-awaiting-part-service-orders/${pagination.pageNumber}/${pagination.pageSize}`);
    }

    getReadyForPickupServiceOrders(pagination: PaginationParams): Observable<BaseResponse<PaginatedResult<ReadyForPickupServiceOrder[]>>> {
        return this._http.get<BaseResponse<PaginatedResult<ReadyForPickupServiceOrder[]>>>(`${environment.apiUrl}/get-ready-for-pickup-service-orders/${pagination.pageNumber}/${pagination.pageSize}`);
    }
}