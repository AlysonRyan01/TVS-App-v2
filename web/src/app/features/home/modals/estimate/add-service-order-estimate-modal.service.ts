import { HttpClient } from "@angular/common/http";
import { inject, Service } from "@angular/core";
import { Observable } from "rxjs";
import { BaseResponse } from "../../../../shared/models/base-response.interface";
import { ServiceOrder } from "../../../../shared/models/service-order.interface";
import { environment } from "../../../../../environments/environment";
import { AddServiceOrderEstimate } from "./models/add-service-order-estimate.model";

@Service()
export class AddServiceOrderEstimateModalService {
    private _http = inject(HttpClient);

    addServiceOrderEstimate(command: AddServiceOrderEstimate): Observable<BaseResponse<ServiceOrder>> {
        return this._http.put<BaseResponse<ServiceOrder>>(`${environment.apiUrl}/add-service-order-estimate`, command);
    }
}