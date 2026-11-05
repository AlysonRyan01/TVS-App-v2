import { HttpClient } from "@angular/common/http";
import { inject, Service } from "@angular/core";
import { Observable } from "rxjs";
import { BaseResponse } from "../../../../shared/models/base-response.interface";
import { ServiceOrder } from "../../../../shared/models/service-order.interface";
import { environment } from "../../../../../environments/environment";

@Service()
export class RepairModalService {
    private _http = inject(HttpClient);

    registerRepair(id: number): Observable<BaseResponse<ServiceOrder>> {
        return this._http.put<BaseResponse<ServiceOrder>>(`${environment.apiUrl}/service-orders/${id}/repair`, {});
    }
}
